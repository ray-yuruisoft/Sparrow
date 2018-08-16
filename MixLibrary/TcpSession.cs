using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MixLibrary
{
    public class TcpSession : WheelTimerEvent
    {
        public Socket socket;
        public byte[] recvBuffer;
        public int readDataPos;
        public int writeDataPos;
        public int isUsing;
        public int sessionID
        {
            get
            {
                if (isUsing != 1)
                    return 0;

                return (int)socket.Handle;
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
        public void Init(int recvBuffSize)
        {
            recvBuffer = new byte[recvBuffSize];
            isUsing = 0;
        }
        public bool Close()
        {
            if (socket == null)
                return false;

            if (socket.Connected)
            {
                //socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            socket = null;
            readDataPos = 0;
            writeDataPos = 0;

            return true;
        }
    }
}
