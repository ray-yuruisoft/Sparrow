using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace MixLibrary
{
    public class ArgsPool<TSession> where TSession : TcpSession, new()
    {
        ConcurrentQueue<SocketAsyncEventArgs> pool = new ConcurrentQueue<SocketAsyncEventArgs>();
        EventHandler<SocketAsyncEventArgs> completed;

        public int Count
        {
            get
            {
                return pool.Count;
            }
        }
        public ArgsPool(int initCount, EventHandler<SocketAsyncEventArgs> completed)
        {
            this.completed = completed;

            for (int i = 0; i < initCount; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += completed;

                pool.Enqueue(args);
            }
        }
        public SocketAsyncEventArgs Alloc(TSession session)
        {
            SocketAsyncEventArgs args = null;

            if (!pool.TryDequeue(out args))
            {
                args = new SocketAsyncEventArgs();
                args.Completed += completed;
            }

            args.UserToken = session;
            args.SetBuffer(session.recvBuffer, 0, session.recvBuffer.Length);

            return args;
        }

        public void Free(SocketAsyncEventArgs args)
        {
            if (args == null)
                return;
            if (args.UserToken == null)
                return;

            args.UserToken = null;
            args.SetBuffer(null, 0, 0);
            pool.Enqueue(args);
        }
    }
}
