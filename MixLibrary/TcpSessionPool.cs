using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MixLibrary
{
    public class TcpSessionPool<TSession> where TSession : TcpSession, new()
    {
        ConcurrentQueue<TSession> sessions = new ConcurrentQueue<TSession>();
        public int Count
        {
            get
            {
                return sessions.Count;
            }
        }
        public TcpSessionPool(int max, int recvBuffSize)
        {
            for (int i = 0; i < max; i++)
            {
                TSession session = new TSession();

                session.Init(recvBuffSize);
                sessions.Enqueue(session);
            }
        }
        public TSession Alloc(Socket acceptedSocket)
        {
            TSession session = null;

            if (!sessions.TryDequeue(out session))
                return null;

            session.socket = acceptedSocket;

            return session;
        }

        public void Free(TSession tcpSession)
        {
            sessions.Enqueue(tcpSession);
        }
    }
}
