using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RobotTool
{
    public class RobotClient : TcpClientSync
    {
        public Robot ownerRobot;

        public RobotClient(Robot robot)
        {
            ownerRobot = robot;
        }
        protected override void OnClosed(bool isInternalCause)
        {
            //Console.WriteLine("OnClosed({0})", isInternalCause);
        }

        protected override void OnReceived(int workerHash, byte[] bodyBuffer, int offset, int bodyLen)
        {
            string content = Encoding.UTF8.GetString(bodyBuffer, offset, bodyLen);
            JObject jObjRecv = JObject.Parse(content);
            string cmd = jObjRecv["cmd"].ToString();

            NetMessage netMessage = new NetMessage();

            netMessage.jObjRecv = jObjRecv;
            netMessage.cmd = cmd;

            ownerRobot.PushNetMessage(netMessage);
        }
    }
}
