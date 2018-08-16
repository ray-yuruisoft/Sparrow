using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime;
using System.Xml;
using MixLibrary;
using MySql.Data.MySqlClient;

namespace HallServer
{
    public class Program
    {
        public static TimerService timerSvc = new TimerService();
        public static WorkerManager workerMgr = new WorkerManager();
        public static DatabaseService dbSvc = new DatabaseService();
        public static DBHelper dbHelper = new DBHelper();
        public static HallServer server = new HallServer();
        public static ModuleManager moduleManager = new ModuleManager();

        static Random rand = new Random();
        static void TestProb()
        {
            int[] counts = new int[2];
            int times = 10000;

            for (int i = 0; i < times; i++)
            {
                if (rand.Next(10000) < 1000)
                    counts[0]++;
                else
                    counts[1]++;
            }

            Console.WriteLine("{0:F2} - {1:F2}", 
                counts[0] / (double)times, 
                counts[1] / (double)times);
        }

        public static void Main(string[] args)
        {
            //挂载全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (GCSettings.IsServerGC)
            {
                Console.WriteLine("GC优化已开启");
            }

            var config = Configure.Inst;

            if (!config.Load())
                return;

            dbSvc.Start(config.dbConnectStr, config.workerCount + 1);
            dbHelper.Start();
            workerMgr.Start(config.workerCount);
            if (!server.Start(config.serverPort, 10000))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("端口：{0}被占用，请按任意键退出", config.serverPort);
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
            moduleManager.Start();
            timerSvc.Start();

            Console.WriteLine("大厅服务器启动完毕，端口：{0}", config.serverPort);

            while (true)
            {
                var key = Console.ReadKey();

                if(key.Key == ConsoleKey.S)
                {
                    Console.WriteLine("");
                    Console.WriteLine("当前连接数：{0} 连接池存量：{1}",
                        server.connectNum, server.GetSessionPoolCount());
                }
                if (key.Key == ConsoleKey.T)
                {
                    Configure.Inst.isShowStat = !Configure.Inst.isShowStat;

                    Console.WriteLine("");
                    if (Configure.Inst.isShowStat)
                        Console.WriteLine("已打开统计信息显示");
                    else
                        Console.WriteLine("已关闭统计信息显示");
                }
                if (key.Key == ConsoleKey.C)
                {
                    Console.Clear();
                }
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            timerSvc.Stop();
            moduleManager.Stop();
            server.Stop();
            workerMgr.Stop();
            dbHelper.Stop();
            dbSvc.Stop();
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUtil.Log("捕获到全局异常：{0}", e.ExceptionObject);
        }
    }
}
