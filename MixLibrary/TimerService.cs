using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.NetworkInformation;

namespace MixLibrary
{
    public class TimerService
    {
        List<BaseTimer> timers = new List<BaseTimer>();
        public void Start()
        {
            Thread thread = new Thread(ThreadProc);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        public void Stop()
        {

        }

        public void AddTimer(BaseTimer timer)
        {
            timers.Add(timer);
        }

        void ThreadProc()
        {
            DateTime lastTime = DateTime.Now;
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            Ping p = new Ping();

            while (true)
            {
                try
                {
                    foreach(var timer in timers)
                    {
                        timer.Run();
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    //LogUtil.Log(ex.Message);
                    //LogUtil.Log(ex.StackTrace);
                }
            }

        }
    }
}
