using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace MixLibrary
{
    public class TcpClientSync
    {
        const int HeaderLen = 8;
        const int MaxBodyLen = 10240;

        public Socket socket;
        public byte[] recvBuffer;
        public int readDataPos;
        public int writeDataPos;

        public bool Connected
        {
            get
            {
                if (socket == null)
                    return false;

                return socket.Connected;
            }
        }

        public int DataLen
        {
            get
            {
                if (writeDataPos < readDataPos)
                    throw new Exception("解包异常1");

                return writeDataPos - readDataPos;
            }
        }

        public int RemainLen
        {
            get
            {
                return recvBuffer.Length - writeDataPos;
            }
        }

        protected virtual void OnReceived(int workerHash, byte[] bodyBuffer, int offset, int bodyLen)
        {

        }

        protected virtual void OnClosed(bool isInternalCause)
        {

        }
        public void Init(int recvBuffSize)
        {
            recvBuffer = new byte[recvBuffSize];
        }

        public bool Connect(string serverIp, int serverPort)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(serverIp, serverPort);

                return socket.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public bool Close(bool isInternalCause = false)
        {
            if (socket == null)
                return false;

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            OnClosed(isInternalCause);

            socket = null;
            readDataPos = 0;
            writeDataPos = 0;

            return true;
        }

        public void Send(int workerHash, byte[] bodyBuffer)
        {
            if (socket == null || !socket.Connected)
                return;

            int bodyLen = bodyBuffer.Length;
            byte[] data = new byte[HeaderLen + bodyLen];

            WriteInt32(bodyLen, data, 0);
            WriteInt32(workerHash, data, 4);
            Buffer.BlockCopy(bodyBuffer, 0, data, HeaderLen, bodyLen);

            try
            {
                int offset = 0;

                while (offset < data.Length)
                {
                    int sendLen = socket.Send(data, offset, data.Length - offset, SocketFlags.None);

                    offset += sendLen;
                }
            }
            catch (Exception)
            {
                
            }
        }

        public void SendHeart()
        {
            if (socket == null || !socket.Connected)
                return;

            byte[] data = new byte[HeaderLen];

            WriteInt32(0, data, 0);
            WriteInt32(0, data, 4);

            try
            {
                int offset = 0;

                while (offset < data.Length)
                {
                    int sendLen = socket.Send(data, offset, data.Length - offset, SocketFlags.None);

                    offset += sendLen;
                }
            }
            catch (Exception)
            {
                
            }
        }

        public bool Receive()
        {
            try
            {
                int bytesTransferred = socket.Receive(recvBuffer, writeDataPos, RemainLen, SocketFlags.None);

                if (bytesTransferred <= 0)
                {
                    return false;
                }

                if (!ParsePacket(recvBuffer, bytesTransferred))
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        bool ParsePacket(byte[] buffer, int bytesTransferred)
        {
            writeDataPos += bytesTransferred;

            while (true)
            {
                //不断解出包
                int bodyLen = 0;
                int workerHash = 0;

                if (!ParseHeader(buffer, ref bodyLen, ref workerHash))
                    break;

                if (bodyLen > MaxBodyLen)
                    return false;

                byte[] bodyBuffer = null;
                int offset = 0;

                if (!ParseBody(buffer, bodyLen, ref bodyBuffer, ref offset))
                    break;

                OnReceived(workerHash, bodyBuffer, offset, bodyLen);
            }

            //如果有残留数据
            if (DataLen > 0 && readDataPos > 0)
            {
                //复制到起始位置
                Buffer.BlockCopy(buffer, readDataPos, buffer, 0, DataLen);                
            }

            //重置读写指针
            writeDataPos = DataLen;
            readDataPos = 0;

            return true;
        }

        bool ParseHeader(byte[] buffer, ref int bodyLen, ref int workerHash)
        {
            if (DataLen < HeaderLen)
                return false;

            bodyLen = ReadInt32(buffer, readDataPos);
            workerHash = ReadInt32(buffer, readDataPos + 4);

            return true;
        }

        bool ParseBody(byte[] buffer, int bodyLen, ref byte[] bodyBuffer, ref int offset)
        {
            if (DataLen < (HeaderLen + bodyLen))
                return false;
            //跳过包头
            readDataPos += HeaderLen;

            //解析包体
            bodyBuffer = buffer;
            offset = readDataPos;

            readDataPos += bodyLen;

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
