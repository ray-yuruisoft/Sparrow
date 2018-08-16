using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MixLibrary
{
    public class TcpServer<TSession> where TSession : TcpSession, new()
    {
        public enum ParsePacketRet
        {
            Complete,
            Pending,
            Fatal,
        }

        const int HeaderLen = 8;
        const int MaxBodyLen = 10240;
        public int connectNum;

        TcpSessionPool<TSession> sessionPool;
        ArgsPool<TSession> receiveArgsPool;
        //ArgsPool<TSession> sendArgsPool;
        Socket listener;
        long recvBytes = 0;
        long sendBytes = 0;
        protected virtual void OnAccepted(TSession session)
        {

        }

        protected virtual void OnReceived(TSession session, int workerHash, byte[] bodyBuffer, int offset, int bodyLen)
        {
            
        }

        protected virtual void OnClosed(TSession session, string cause, bool isInternalCause)
        {

        }

        public virtual bool Start(int port, int maxConnectNum, bool isReuseAddress = true, int recvBuffSize = 4096)
        {
            try
            {
                if (PortInUse(port))
                    return false;

                sessionPool = new TcpSessionPool<TSession>(maxConnectNum, recvBuffSize);
                receiveArgsPool = new ArgsPool<TSession>(maxConnectNum, IOCompleted);
                //sendArgsPool = new ArgsPool<TSession>(60000, IOCompleted);
                connectNum = 0;

                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, isReuseAddress);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(1000);
                StartAccept(null);

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex.Message);
                LogUtil.Log(ex.StackTrace);

                return true;
            }
        }

        public virtual void Stop()
        {
            listener.Close();
        }

        public long GetSendBytes()
        {
            return sendBytes;
        }

        public long GetRecvBytes()
        {
            return recvBytes;
        }

        public void ResetSendBytes()
        {
            Interlocked.Exchange(ref sendBytes, 0);
        }

        public void ResetRecvBytes()
        {
            Interlocked.Exchange(ref recvBytes, 0);
        }

        bool PortInUse(int port)
        {
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }

            return inUse;
        }

        public int GetSessionPoolCount()
        {
            return sessionPool.Count;
        }

        public void Send(TSession session, int workerHash, byte[] bodyBuffer)
        {
            if (session == null || session.socket == null || !session.socket.Connected)
                return;

            int bodyLen = bodyBuffer.Length;
            byte[] data = new byte[HeaderLen + bodyLen];

            WriteInt32(bodyLen, data, 0);
            WriteInt32(workerHash, data, 4);
            Buffer.BlockCopy(bodyBuffer, 0, data, HeaderLen, bodyLen);

            try
            {
                int offset = 0;

                while(offset < data.Length)
                {
                    int sendLen = session.socket.Send(data, offset, data.Length - offset, SocketFlags.None);

                    offset += sendLen;
                }

                Interlocked.Add(ref sendBytes, (long)data.Length);
            }
            catch (Exception)
            {
                
            }
        }

        //void SendAsync(TSession session, byte[] data, int offset, int len)
        //{
        //    SocketAsyncEventArgs writeArgs = null;

        //    try
        //    {
        //        writeArgs = sendArgsPool.Alloc(session);
        //        writeArgs.SetBuffer(data, offset, len);

        //        if (!session.socket.SendAsync(writeArgs))
        //        {
        //            ProcessSended(writeArgs);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        sendArgsPool.Free(writeArgs);
        //    }
        //}
        public void Disconnect(TSession session, string cause, bool isInternalCause = false)
        {
            if (session == null)
                return;

            if (Interlocked.CompareExchange(ref session.isUsing, 0, 1) == 1)
            {
                Interlocked.Decrement(ref connectNum);
                OnClosed(session, cause, isInternalCause);

                session.Close();
                sessionPool.Free(session);
            }
        }

        void IOCompleted(object sender, SocketAsyncEventArgs args)
        {
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceived(args);
                    break;
                //case SocketAsyncOperation.Send:
                //    ProcessSended(args);
                //    break;
                default:
                    break;
            }
        }

        void StartAccept(SocketAsyncEventArgs args)
        {
            if(args == null)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += ProcessAccepted;
            }
            else
            {
                args.AcceptSocket = null;
            }

            if(!listener.AcceptAsync(args))
            {
                ProcessAccepted(listener, args);
            }
        }

        void ProcessAccepted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Console.WriteLine("Accepted error: {0}", args.SocketError);
                return;
            }

            TSession session = sessionPool.Alloc(args.AcceptSocket);

            if (session == null)
            {
                LogUtil.Log("超出最大连接数");
                if (args.AcceptSocket.Connected)
                    args.AcceptSocket.Close();
            }
            else
            {
                SocketAsyncEventArgs recvArgs = null;

                try
                {
                    if (Interlocked.CompareExchange(ref session.isUsing, 1, 0) == 0)
                    {
                        //session.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                        Interlocked.Increment(ref connectNum);

                        OnAccepted(session);

                        recvArgs = receiveArgsPool.Alloc(session);

                        if (!session.socket.ReceiveAsync(recvArgs))
                        {
                            ProcessReceived(recvArgs);
                        }
                    }
                    else
                    {
                        LogUtil.Log("isUsing同步异常");
                        if (args.AcceptSocket.Connected)
                            args.AcceptSocket.Close();
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Log(ex.Message);
                    LogUtil.Log(ex.StackTrace);
                    Disconnect(session, "接受连接发生异常", true);
                    receiveArgsPool.Free(recvArgs);
                }
            }

            StartAccept(args);
        }

        void ProcessReceived(SocketAsyncEventArgs args)
        {
            TSession session = args.UserToken as TSession;

            try
            {
                if (args.SocketError == SocketError.Success && args.BytesTransferred > 0)
                {
                    Interlocked.Add(ref recvBytes, (long)args.BytesTransferred);

                    if (ParsePacket(session, args) == ParsePacketRet.Fatal)
                        throw new Exception("解包异常2");

                    if (!session.socket.ReceiveAsync(args))
                    {
                        ProcessReceived(args);
                    }
                }
                else
                {
                    Disconnect(session, "远端主动关闭连接", true);
                    receiveArgsPool.Free(args);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex.Message);
                LogUtil.Log(ex.StackTrace);
                Disconnect(session, "接收数据发生异常", true);
                receiveArgsPool.Free(args);
            }
        }

        //void ProcessSended(SocketAsyncEventArgs args)
        //{
        //    TSession session = args.UserToken as TSession;
        //    sendArgsPool.Free(args);
        //}

        ParsePacketRet ParsePacket(TSession session, SocketAsyncEventArgs args)
        {
            ParsePacketRet ret = ParsePacketRet.Pending;

            session.writeDataPos += args.BytesTransferred;

            while(true)
            {
                //不断解出包
                int bodyLen = 0;
                int workerHash = 0;

                if (!ParseHeader(session, args, ref bodyLen, ref workerHash))
                    break;

                if (bodyLen > MaxBodyLen)
                    return ParsePacketRet.Fatal;

                byte[] bodyBuffer = null;
                int offset = 0;

                if (!ParseBody(session, args, bodyLen, ref bodyBuffer, ref offset))
                    break;

                ret = ParsePacketRet.Complete;

                OnReceived(session, workerHash, bodyBuffer, offset, bodyLen);
            }

            //如果有残留数据
            if (session.DataLen > 0 && session.readDataPos > 0)
            {
                //复制到起始位置
                Buffer.BlockCopy(args.Buffer, session.readDataPos, args.Buffer, 0, session.DataLen);
            }

            //重置读写指针
            session.writeDataPos = session.DataLen;
            session.readDataPos = 0;

            args.SetBuffer(session.writeDataPos, session.RemainLen);

            return ret;
        }

        bool ParseHeader(TSession session, SocketAsyncEventArgs args, ref int bodyLen, ref int workerHash)
        {
            if (session.DataLen < HeaderLen)
                return false;

            bodyLen = ReadInt32(args.Buffer, session.readDataPos);
            workerHash = ReadInt32(args.Buffer, session.readDataPos + 4);

            return true;
        }

        bool ParseBody(TSession session, SocketAsyncEventArgs args, int bodyLen, ref byte[] bodyBuffer, ref int offset)
        {
            if (session.DataLen < (HeaderLen + bodyLen))
                return false;
            //跳过包头
            session.readDataPos += HeaderLen;

            //解析包体
            bodyBuffer = args.Buffer;
            offset = session.readDataPos;

            session.readDataPos += bodyLen;

            return true;
        }

        void WriteInt32(Int32 value, byte[] buffer, int offset)
        {
            byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));

            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
        }

        Int32 ReadInt32(byte[] buffer, int offset)
        {
            int ret = BitConverter.ToInt32(buffer, offset);

            return IPAddress.NetworkToHostOrder(ret);
        }
    }
}
