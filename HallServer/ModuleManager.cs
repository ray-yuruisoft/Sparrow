using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HallServer
{
    public class ModuleManager
    {
        Dictionary<string, Action<int, HallServerSession, string, JObject>> requestHandlers = 
            new Dictionary<string, Action<int, HallServerSession, string, JObject>>();
        public long totalRequest = 0;

        public HallModule hallModule = new HallModule();
        public void Start()
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                DateTime lastTime = DateTime.Now;

                while (true)
                {
                    Thread.Sleep(2000);

                    if(Configure.Inst.isShowStat)
                    {
                        double elapsed = (DateTime.Now - lastTime).TotalSeconds;
                        Console.WriteLine("R = {0:F2} k/s，S = {1:F2} k/s，QPS = {2:F2} /s，查询 = {3:F2} /s，非查询 = {4:F2} /s",
                            (Program.server.GetRecvBytes() / 1024.0D) / elapsed,
                            (Program.server.GetSendBytes() / 1024.0D) / elapsed,
                            (double)totalRequest / elapsed,
                            (double)Program.dbSvc.totalQuery / elapsed,
                            (double)Program.dbSvc.totalNoQuery / elapsed);
                    }

                    lastTime = DateTime.Now;
                    Program.server.ResetRecvBytes();
                    Program.server.ResetSendBytes();
                    Interlocked.Exchange(ref totalRequest, 0);
                    Interlocked.Exchange(ref Program.dbSvc.totalQuery, 0);
                    Interlocked.Exchange(ref Program.dbSvc.totalNoQuery, 0);
                }
            });

            hallModule.Start();
        }

        public void Stop()
        {
            hallModule.Stop();
        }

        public void RegisterRequestHandler(string cmd, Action<int, HallServerSession, string, JObject> handler)
        {
            requestHandlers[cmd] = handler;
        }
        public void OnAccepted(int workerIndex, HallServerSession session)
        {
            hallModule.OnAccepted(workerIndex, session);
        }

        public void OnReceived(int workerIndex, HallServerSession session, string content)
        {
            JObject jObjRecv = JObject.Parse(content);

            string cmd = jObjRecv["cmd"].ToString();

            Action<int, HallServerSession, string, JObject> handler;

            if (requestHandlers.TryGetValue(cmd, out handler))
            {
                Interlocked.Increment(ref totalRequest);

                handler(workerIndex, session, cmd, jObjRecv);
            }
        }

        public void OnClosed(int workerIndex, HallServerSession session, string closedCause, bool isInternalCause)
        {
            //Console.WriteLine("{0}:{1}", session.sessionID, closedCause);

            hallModule.OnClosed(workerIndex, session, closedCause, isInternalCause);
        }
    }
}
