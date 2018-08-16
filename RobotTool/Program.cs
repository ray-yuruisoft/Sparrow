using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using System.Threading;
using System.Diagnostics;
using MixLibrary;

namespace RobotTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (GCSettings.IsServerGC)
            {
                //Console.WriteLine("GC优化已开启");
            }

            RobotMgr.Inst.Start();

            Console.WriteLine("机器人管理器启动完毕，已创建{0}个机器人", RobotMgr.Inst.robots.Count);

            //Thread thread = new Thread(ClearMemoryThreadProc);
            //thread.IsBackground = true;
            //thread.Start();

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.S)
                {
                    Console.WriteLine("");
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

            RobotMgr.Inst.Stop();
        }

        private static void ClearMemoryThreadProc()
        {
            while (true)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.Sleep(5000);
            }
        }
    }
}
