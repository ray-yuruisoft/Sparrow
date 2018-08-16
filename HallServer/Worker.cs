using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MixLibrary;

namespace HallServer
{
    public class Worker
    {
        int index;
        ConcurrentQueue<NetMessage> netMsgQueue = new ConcurrentQueue<NetMessage>();
        AutoResetEvent queueEvent = new AutoResetEvent(false);

        public Worker(int index)
        {
            this.index = index;
        }
        public void Start()
        {
            Thread thread = new Thread(ThreadProc);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        public void PushNetMessage(NetMessage msg)
        {
            netMsgQueue.Enqueue(msg);
            queueEvent.Set();
        }
        void ThreadProc()
        {
            while (true)
            {
                NetMessage netMessage = null;

                try
                {
                    if (netMsgQueue.Count > 0)
                    {
                        if (netMsgQueue.TryDequeue(out netMessage))
                        {
                            switch (netMessage.action)
                            {
                                case 0:
                                    Program.moduleManager.OnAccepted(index, netMessage.session);
                                    break;
                                case 1:
                                    Program.moduleManager.OnReceived(index, netMessage.session, netMessage.content);
                                    break;
                                case 2:
                                    Program.moduleManager.OnClosed(index, netMessage.session, netMessage.closedCause, netMessage.isClosedInternalCause);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        queueEvent.WaitOne(100);
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Log(ex.Message);
                    LogUtil.Log(ex.StackTrace);
                    if(netMessage != null)
                        Program.server.Disconnect(netMessage.session, "网络消息处理异常");
                }
            }
        }
    }
}
