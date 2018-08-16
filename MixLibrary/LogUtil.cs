using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MixLibrary
{
    public static class LogUtil
    {
        static object logLocker = new object();
        public static void Log(string format, params object[] args)
        {
            string content = string.Format(format, args);
            string timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
//#if DEBUG
//            lock (logLocker)
//            {
//                Console.ForegroundColor = ConsoleColor.Red;
//                Console.WriteLine(timeStr + "  " + content);
//                Console.ResetColor();
//            }
//#endif
            string path = AppDomain.CurrentDomain.BaseDirectory;

            if (path.Length > 0)
            {
                path += "log";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path += @"\" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                if (!File.Exists(path))
                {
                    FileStream fs = File.Create(path);
                    fs.Close();
                }
                if (File.Exists(path))
                {
                    lock (logLocker)
                    {
                        var encoding = new UTF8Encoding(false);
                        StreamWriter sw = new StreamWriter(path, true, encoding);
                        sw.WriteLine(timeStr + "  " + content);
                        sw.Close();
                    }
                }
            }
        }
    }
}
