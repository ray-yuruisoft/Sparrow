using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace HallServer
{
    public class HallServer : TcpServer<HallServerSession>
    {
        WheelTimer heartCheckTimer;
        public override bool Start(int port, int maxConnectNum, bool isReuseAddress = true, int recvBuffSize = 4096)
        {
            heartCheckTimer = new WheelTimer(Configure.Inst.heartPeriod, (te) =>
            {
                HallServerSession session = te as HallServerSession;

                Program.server.Disconnect(session, "心跳超时");
            });
            Program.timerSvc.AddTimer(heartCheckTimer);

            return base.Start(port, maxConnectNum, isReuseAddress, recvBuffSize);
        }

        public override void Stop()
        {
            base.Stop();
        }
        protected override void OnAccepted(HallServerSession session)
        {
            heartCheckTimer.Add(session, Configure.Inst.heartPeriod);

            var netMessage = new NetMessage(0, session);
            var worker = Program.workerMgr.AllotWorker(session.sessionID);
            worker.PushNetMessage(netMessage);
        }

        protected override void OnReceived(HallServerSession session, int workerHash, byte[] bodyBuffer, int offset, int bodyLen)
        {
            if (bodyLen > 0)
            {
                string content = Encoding.UTF8.GetString(bodyBuffer, offset, bodyLen);
                var netMessage = new NetMessage(1, session, content);
                var worker = Program.workerMgr.AllotWorker(session.sessionID);
                worker.PushNetMessage(netMessage);
            }
            else
            {
                //心跳包
                heartCheckTimer.Active(session, Configure.Inst.heartPeriod);
            }
        }

        protected override void OnClosed(HallServerSession session, string cause, bool isInternalCause)
        {
            heartCheckTimer.Remove(session);

            var netMessage = new NetMessage(2, session, "", cause, isInternalCause);
            var worker = Program.workerMgr.AllotWorker(session.sessionID);
            worker.PushNetMessage(netMessage);
        }

        public void Send(HallServerSession session, JObject jObj)
        {
            if (session == null)
                return;

            string content = jObj.ToString(Formatting.None);
            Send(session, 0, Encoding.UTF8.GetBytes(content));
        }

        public void SendError(HallServerSession session, string cmd, string errorMsg)
        {
            if (session == null)
                return;

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 1;
            jObj["error_msg"] = errorMsg;

            Send(session, jObj);
        }

        public void SendSuccess(HallServerSession session, string cmd)
        {
            if (session == null)
                return;

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;

            Send(session, jObj);
        }

        public void SendSuccessWithReader(HallServerSession session, string cmd, MySqlDataReader reader, params string[] fields)
        {
            if (session == null)
                return;

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;

            foreach (string field in fields)
            {
                if (reader.GetFieldType(field) == typeof(DateTime))
                    jObj[field] = reader.GetDateTimeSafe(field).ToString(DateTimeUtil.format);
                else
                    jObj[field] = reader.GetStringSafe(field);
            }

            Send(session, jObj);
        }
    }
}
