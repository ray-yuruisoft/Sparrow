using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RobotTool
{
    public class Robot : Coroutine
    {
        public RobotMgr ownerMgr;
        public RobotClient client;
        public string account;
        public string show_id;
        public string login_token;
        public string login_ip;
        public string nick;
        public string icon;
        public string sign;
        public long money;
        public int currentState;

        public int callBankerProb;
        public int[] betterBetProbs;

        DateTime lastSendHeartTime;
        object locker = new object();
        List<NetMessage> netMessages = new List<NetMessage>();
        JObject jObjRecv;
        int ret_code;

        public class ChipGroup
        {
            public int betPos;
            public long chipMoney;
            public int chipCount;
        }

        public Robot(RobotMgr robotMgr)
        {
            ownerMgr = robotMgr;
            client = new RobotClient(this);
            client.Init(40960);
            chipLevels =
                ownerMgr.grade == 0 ?
                new long[] { 20000, 10000, 5000, 1000, 500, 100 }
                : new long[] { 5000000, 1000000, 500000, 100000, 50000, 10000 };
        }

        public void PushNetMessage(NetMessage netMessage)
        {
            lock (locker)
            {
                netMessages.Add(netMessage);
            }
        }

        protected override IEnumerator OnCoroutineUpdate()
        {
            //连接
            while (!Connect())
            {
                //5秒后重连
                yield return WaitForSeconds(5);
            }

            while (true)
            {
                yield return WaitForSeconds(0.1f);
                //找寻桌子
                yield return GetTable();

                if (ret_code != 0)
                    continue;

                int table_id = (int)jObjRecv["table_id"];

                if (table_id <= 0)
                {
                    yield return WaitForSeconds(0.5f);
                    continue;
                }

                //进入桌子
                yield return JoinTable(table_id);

                if (ret_code != 0)
                    continue;

                //Console.WriteLine("{0} 进入房间", nick);

                if (ownerMgr.gameName == "二十一点")
                {
                    //yield return PlayOther1Game();
                }
                else if (ownerMgr.gameName == "火拼牛牛")
                {
                    yield return PlayOther2Game();
                }
                else
                {
                    yield return PlayGame();
                }

                yield return LeaveTable();
                yield return WaitForSeconds(20);
            }
        }

        protected override void OnCoroutineComplete()
        {

        }

        public void Update()
        {
            if ((DateTime.Now - lastSendHeartTime).TotalSeconds > ownerMgr.sendHeartInterval)
            {
                client.SendHeart();

                lastSendHeartTime = DateTime.Now;
            }

            UpdateCoroutine();
        }

        bool Connect()
        {
            client.Connect(ownerMgr.gameServerIp, ownerMgr.gameServerPort);

            if (!client.Connected)
                return false;

            lastSendHeartTime = DateTime.Now;

            Thread thread = new Thread(ThreadRecvProc);
            thread.IsBackground = true;
            thread.Start();

            return true;
        }

        void ThreadRecvProc()
        {
            while (true)
            {
                if (!client.Receive())
                    break;
            }

            Disconnect();
        }

        void Send(JObject jObj)
        {
            string content = jObj.ToString(Formatting.None);
            client.Send(0, Encoding.UTF8.GetBytes(content));
        }

        void Disconnect()
        {
            client.Close();
        }

        #region 网络消息处理
        void Reset()
        {
            jObjRecv = null;
            ret_code = -1;
        }

        NetMessage FindNetMessage(string cmd, string subcmd)
        {
            lock (locker)
            {
                for (int i = 0; i < netMessages.Count; i++)
                {
                    NetMessage netMessage = netMessages[i];

                    if (subcmd.Length > 0)
                    {
                        if (netMessage.cmd == cmd && netMessage.jObjRecv["subcmd"].ToString() == subcmd)
                        {
                            netMessages.RemoveAt(i);
                            return netMessage;
                        }
                    }
                    else
                    {
                        if (netMessage.cmd == cmd)
                        {
                            netMessages.RemoveAt(i);
                            return netMessage;
                        }
                    }
                }
            }

            return null;
        }

        void ClearNetMessages()
        {
            lock (locker)
            {
                netMessages.Clear();
            }
        }

        IEnumerator WaitForNetMessage(string cmd, string subcmd = "", double timeOutSeconds = 5)
        {
            NetMessage netMessage = null;
            DateTime startWaitTime = DateTime.Now;

            while (true)
            {
                netMessage = FindNetMessage(cmd, subcmd);
                if (netMessage != null)
                    break;

                if ((DateTime.Now - startWaitTime).TotalSeconds > timeOutSeconds)
                    break;

                yield return null;
            }

            if (netMessage != null)
            {
                jObjRecv = netMessage.jObjRecv;
            }
        }
        IEnumerator GetTable()
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "get_game_table";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["game_name"] = ownerMgr.gameName;
            jObjSend["grade"] = ownerMgr.grade.ToString();

            Send(jObjSend);

            yield return WaitForNetMessage(cmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }

        IEnumerator JoinTable(int table_id)
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "join_table";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["table_id"] = table_id;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }

        IEnumerator LeaveTable()
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "leave_table";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }

        IEnumerator GetTableInfo()
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "get_table_info";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }

        IEnumerator GetRobotBet()
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "get_robot_bet";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }

        void Bet(int pos, long money)
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "bet";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;
            jObjSend["pos"] = pos;
            jObjSend["money"] = money;

            Send(jObjSend);
        }

        IEnumerator GodInstructSetMoney(long newMoney)
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "god_instruct";
            string subcmd = "set_money";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;
            jObjSend["money"] = newMoney;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];

                if (ret_code == 0)
                    money = newMoney;
            }
        }
        IEnumerator WaitNotifySendResult()
        {
            Reset();

            string cmd = "notify_ingame";
            string subcmd = "send_result";

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }

        IEnumerator WaitNotifyChangeState()
        {
            Reset();

            string cmd = "notify_ingame";
            string subcmd = "change_state";

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
                currentState = (int)jObjRecv["state"];
            }
        }
        IEnumerator WaitNotifyPokerState()
        {
            Reset();

            NetMessage netMessage = null;
            DateTime startWaitTime = DateTime.Now;

            while (true)
            {
                netMessage = FindNetMessage("notify_ingame", "change_state");
                if (netMessage != null && (int)netMessage.jObjRecv["state"] == 1)
                {
                    currentState = 1;
                    break;
                }

                //if ((DateTime.Now - startWaitTime).TotalSeconds > 5)
                //    break;

                yield return null;
            }

            if (netMessage != null)
            {
                jObjRecv = netMessage.jObjRecv;
            }

        }
        #endregion

        static long[] chipLevels;
        int[] MoneyToChipCounts(long money)
        {
            int[] chipCounts = new int[chipLevels.Length];
            long remain = money;

            for (int i = 0; i < chipLevels.Length; i++)
            {
                chipCounts[i] = (int)(remain / chipLevels[i]);
                remain = remain % chipLevels[i];
            }

            return chipCounts;
        }
        IEnumerator PlayGame()
        {
            while (true)
            {

                ClearNetMessages();
                yield return WaitForSeconds(0.1f);
                //查看当前状态，是否为投注状态
                yield return GetTableInfo();

                if (ret_code != 0)
                    continue;

                int state = (int)jObjRecv["state"];

                if (state != 1)
                    continue;

                long time = (long)jObjRecv["time"];
                long nextstate_time = (long)jObjRecv["nextstate_time"];
                //确定剩余投注时间-3，得到可以投注的时间
                long canBetTime = nextstate_time - time - 3000;

                if (canBetTime < 1000)
                    continue;

                yield return GetRobotBet();

                if (ret_code != 0)
                    continue;

                JArray jBetMoneys = (JArray)jObjRecv["betMoneys"];
                long[] betMoneys = new long[jBetMoneys.Count];

                for (int i = 0; i < jBetMoneys.Count; i++)
                {
                    betMoneys[i] = (long)jBetMoneys[i];
                }

                //Console.WriteLine("{0} 可投注时间：{1}ms 分配投注：{2}", nick, canBetTime, string.Join(",", betMoneys));

                List<ChipGroup> chipGroups = new List<ChipGroup>();
                long chipTotal = 0;

                //对每个区进行从小到大下注
                for (int i = 0; i < betMoneys.Length; i++)
                {
                    long betMoney = (long)betMoneys[i];

                    var chipCounts = MoneyToChipCounts(betMoney);

                    for (int j = chipLevels.Length - 1; j >= 0; j--)
                    {
                        long chipMoney = chipLevels[j];
                        int chipCount = chipCounts[j];

                        if (chipCount > 0)
                        {
                            ChipGroup chipGroup = new ChipGroup();

                            chipGroup.betPos = i;
                            chipGroup.chipMoney = chipMoney;
                            chipGroup.chipCount = chipCount;

                            chipGroups.Add(chipGroup);

                            chipTotal += chipCount;
                        }
                    }
                }

                var tempInterval = canBetTime / chipTotal;
                //增加1.5秒时延迟
                long interval = tempInterval < 1500 ? 1500 + tempInterval : 1500;

                //Console.WriteLine("投注时间间隔：{0}ms", interval);

                while (chipGroups.Count > 0)
                {
                    int index = ownerMgr.rand.Next(chipGroups.Count);
                    var chipGroup = chipGroups[index];
                    Bet(chipGroup.betPos, chipGroup.chipMoney);
                    chipGroup.chipCount--;
                    if (chipGroup.chipCount == 0)
                        chipGroups.Remove(chipGroup);

                    yield return WaitForTimeStamp(interval);
                }


                while (true)
                {
                    yield return WaitNotifySendResult();

                    if (ret_code == 0)
                    {
                        break;
                    }
                }

                if (jObjRecv != null)
                {
                    money = (long)jObjRecv["money"];

                    ////Console.WriteLine("{0} 结算后金额：{1}", nick, money);

                    if (money <= ownerMgr.minMoney || money > ownerMgr.maxMoney)
                    {
                        if (money <= ownerMgr.minMoney)
                        {
                            long increment = (long)RandomUtil.RandomMinMax(ownerMgr.rand, ownerMgr.addMoneyMin, ownerMgr.addMoneyMax);
                            //Console.WriteLine("{0} 补充金币：{1}", nick, increment);
                            yield return GodInstructSetMoney(money + increment);
                        }
                        //50%概率离开房间
                        if (ownerMgr.rand.Next(10000) < 5000)
                        {
                            //Console.WriteLine("{0} 离开房间", nick);
                            break;
                        }
                    }
                }
            }
        }

        #region 二十一点
        //IEnumerator WaitNotifyOperate()
        //{
        //    Reset();

        //    string cmd = "notify_ingame";
        //    string subcmdA;
        //    string subcmdB;

        //    subcmdA = "stop";
        //    subcmdB = "double";
        //    yield return WaitForAnyTwoNetMessage(cmd, subcmdA, subcmdB);

        //    if (jObjRecv != null)
        //    {
        //        ret_code = (int)jObjRecv["ret_code"];
        //    }
        //}
        //IEnumerator WaitNotifyPoker()
        //{
        //    NetMessage netMessage = null;
        //    DateTime startWaitTime = DateTime.Now;

        //    while (true)
        //    {
        //        netMessage = FindNetMessage("notify_ingame", "send_poker");
        //        if (netMessage != null)
        //            break;

        //        //if ((DateTime.Now - startWaitTime).TotalSeconds > timeOutSeconds)
        //        //    break;

        //        yield return null;
        //    }

        //    if (netMessage != null)
        //    {
        //        jObjRecv = netMessage.jObjRecv;
        //    }

        //}
        //IEnumerator WaitNotifyHellotAndStop()
        //{
        //    Reset();

        //    string cmd = "notify_ingame";
        //    string subcmdA;
        //    string subcmdB;

        //    subcmdA = "hellot";
        //    subcmdB = "stop";

        //    yield return WaitForAnyTwoNetMessageNoCondition(cmd, subcmdA, subcmdB);

        //    if (jObjRecv != null)
        //    {
        //        ret_code = (int)jObjRecv["ret_code"];
        //    }
        //}
        //IEnumerator PlayOther1Game()
        //{
        //    while (true)
        //    {

        //        ClearNetMessages();
        //        yield return WaitForSeconds(0.1f);
        //        //查看当前状态，是否为投注状态
        //        yield return GetTableInfo();
        //        JArray allSeats = (JArray)jObjRecv["all_seats"];

        //        foreach (var item in allSeats)
        //        {
        //            if ((string)item["player_show_id"] == show_id)
        //            {
        //                seatId = (int)item["id"];
        //                break;
        //            }
        //        }

        //        if (ret_code != 0)
        //            continue;

        //        int state = (int)jObjRecv["state"];

        //        if (state != 0)
        //            continue;

        //        long time = (long)jObjRecv["time"];
        //        long nextstate_time = (long)jObjRecv["nextstate_time"];
        //        //确定剩余投注时间-3，得到可以投注的时间
        //        long canBetTime = nextstate_time - time - 3000;

        //        if (canBetTime < 1000)
        //            continue;

        //        yield return GetRobotBet();

        //        if (ret_code != 0)
        //            continue;

        //        long betMoney = (long)jObjRecv["betMoney"];
        //        Bet(betMoney);
        //        FinishBet();

        //        yield return WaitNotifyPoker();
        //        var nextSeatId = (int)jObjRecv["next_option_seats"];
        //        int bankerPoints = 0;
        //        int robotPoints = 0;
        //        int[] robotPokers = new int[2];
        //        var seats = (JArray)jObjRecv["pokers"];
        //        GetBankerAndRobotPokersPointSendPokerState(seats, ref bankerPoints, ref robotPoints, robotPokers);
        //        yield return WaitForTimeStamp(3000);
        //        if (nextSeatId >= seatId)
        //        {//会进行操作
        //            if (nextSeatId == seatId)
        //            {
        //                if (robotPoints < 21)
        //                {
        //                    while (true)
        //                    {//操作

        //                        if (robotPoints >= 21) break;
        //                        if (robotPoints > 16)
        //                        {
        //                            yield return WaitForTimeStamp(1000);
        //                            Stop();
        //                            break;
        //                        }
        //                        var pokerA = robotPokers[0];
        //                        var pokerB = robotPokers[1];
        //                        string a = pokerA == 1 ? "A" : pokerA.ToString();
        //                        string b = pokerB == 1 ? "A" : pokerB.ToString();
        //                        string robotPoker = $"{a},{b}";
        //                        string bankerPoker = bankerPoints == 1 ? "A" : bankerPoints > 10 ? "10" : bankerPoints.ToString();
        //                        var ai = operationStrategyInfos.FirstOrDefault(c => c.robotPoker.Contains(robotPoker) && c.bankerPoker == bankerPoker);
        //                        if (ai == null)
        //                        {
        //                            if (5 <= robotPoints && robotPoints <= 8)
        //                            {
        //                                robotPoker = "5-8";
        //                            }
        //                            else if (13 <= robotPoints && robotPoints <= 16)
        //                            {
        //                                robotPoker = "13-16";
        //                            }
        //                            else if (17 <= robotPoints && robotPoints <= 21)
        //                            {
        //                                robotPoker = "17-21";
        //                            }
        //                            else
        //                            {
        //                                robotPoker = bankerPoints > 10 ? "10" : bankerPoints.ToString();
        //                            }
        //                            ai = operationStrategyInfos.First(c => c.robotPoker.Contains(robotPoker) && c.bankerPoker == bankerPoker);
        //                        }
        //                        if (ai.cmd == "H")
        //                        {
        //                            yield return WaitForTimeStamp(1000);
        //                            Hellot();
        //                            yield return WaitNotifyHellotAndStop();
        //                            if ((string)jObjRecv["subcmd"] == "stop")
        //                            {
        //                                break;
        //                            }
        //                            else
        //                            {
        //                                GetRobotPokersPointNotify((JArray)jObjRecv["pokers"], ref robotPoints, robotPokers);
        //                                continue;
        //                            }
        //                        }
        //                        else if (ai.cmd == "P")
        //                        {

        //                            ;
        //                        }
        //                        else if (ai.cmd == "D")
        //                        {

        //                            ;
        //                        }
        //                        else if (ai.cmd == "S")
        //                        {
        //                            Stop();
        //                            break;
        //                        }
        //                        else if (ai.cmd == "DS")
        //                        {
        //                            ;

        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    Stop();
        //                }

        //            }
        //            else
        //            {
        //                yield return WaitNotifyOperate();
        //                //GetBankerAndRobotPokersPoint(seats, ref bankerPoints, ref robotPoints);
        //                while (true)
        //                {//操作
        //                    ;
        //                }

        //            }

        //        }

        //        while (true)
        //        {
        //            yield return WaitNotifySendResult();

        //            if (ret_code == 0)
        //            {
        //                break;
        //            }
        //        }

        //        if (jObjRecv != null)
        //        {
        //            var jResults = (JArray)jObjRecv["results"];
        //            foreach (var item in jResults)
        //            {
        //                if ((string)item["show_id"] == show_id)
        //                {
        //                    money = (long)item["money"];
        //                }
        //            }

        //            ////Console.WriteLine("{0} 结算后金额：{1}", nick, money);

        //            if (money <= ownerMgr.minMoney || money > ownerMgr.maxMoney)
        //            {
        //                if (money <= ownerMgr.minMoney)
        //                {
        //                    long increment = (long)RandomUtil.RandomMinMax(ownerMgr.rand, ownerMgr.addMoneyMin, ownerMgr.addMoneyMax);
        //                    //Console.WriteLine("{0} 补充金币：{1}", nick, increment);
        //                    yield return GodInstructSetMoney(money + increment);
        //                }
        //                //50%概率离开房间
        //                if (ownerMgr.rand.Next(10000) < 5000)
        //                {
        //                    //Console.WriteLine("{0} 离开房间", nick);
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //}

        //IEnumerator WaitForAnyTwoNetMessageNoCondition(string cmd, string subcmdA = "", string subcmdB = "", double timeOutSeconds = 5)
        //{
        //    NetMessage netMessage = null;
        //    DateTime startWaitTime = DateTime.Now;

        //    while (true)
        //    {
        //        var tempA = FindNetMessage(cmd, subcmdA);
        //        var tempB = FindNetMessage(cmd, subcmdB);
        //        if (tempA != null) netMessage = tempA;
        //        if (tempB != null) netMessage = tempB;

        //        if (netMessage != null)
        //            break;

        //        if ((DateTime.Now - startWaitTime).TotalSeconds > timeOutSeconds)
        //            break;

        //        yield return null;
        //    }

        //    if (netMessage != null)
        //    {
        //        jObjRecv = netMessage.jObjRecv;
        //    }
        //}
        //IEnumerator WaitForAnyTwoNetMessage(string cmd, string subcmdA = "", string subcmdB = "", double timeOutSeconds = 5)
        //{
        //    NetMessage netMessage = null;
        //    DateTime startWaitTime = DateTime.Now;

        //    while (true)
        //    {
        //        netMessage = FindNetMessage(cmd, subcmdA);
        //        netMessage = FindNetMessage(cmd, subcmdB);

        //        if (netMessage != null && (int)netMessage.jObjRecv["next_option_seatid"] == seatId)
        //            break;

        //        if ((DateTime.Now - startWaitTime).TotalSeconds > timeOutSeconds)
        //            break;

        //        yield return null;
        //    }

        //    if (netMessage != null)
        //    {
        //        jObjRecv = netMessage.jObjRecv;
        //    }
        //}

        //void Hellot()
        //{
        //    Reset();

        //    JObject jObjSend = new JObject();
        //    string cmd = "operate_ingame";
        //    string subcmd = "hellot";

        //    jObjSend["cmd"] = cmd;
        //    jObjSend["show_id"] = show_id;
        //    jObjSend["token"] = login_token;
        //    jObjSend["subcmd"] = subcmd;
        //    jObjSend["is_separate_pokers"] = 0;

        //    Send(jObjSend);
        //}
        //void Stop()
        //{
        //    Reset();

        //    JObject jObjSend = new JObject();
        //    string cmd = "operate_ingame";
        //    string subcmd = "stop";

        //    jObjSend["cmd"] = cmd;
        //    jObjSend["show_id"] = show_id;
        //    jObjSend["token"] = login_token;
        //    jObjSend["subcmd"] = subcmd;
        //    jObjSend["is_separate_pokers"] = 0;

        //    Send(jObjSend);
        //}
        //void FinishBet()
        //{
        //    Reset();

        //    JObject jObjSend = new JObject();
        //    string cmd = "operate_ingame";
        //    string subcmd = "finish_bet";

        //    jObjSend["cmd"] = cmd;
        //    jObjSend["show_id"] = show_id;
        //    jObjSend["token"] = login_token;
        //    jObjSend["subcmd"] = subcmd;

        //    Send(jObjSend);
        //}
        //void Bet(long money)
        //{
        //    Reset();

        //    JObject jObjSend = new JObject();
        //    string cmd = "operate_ingame";
        //    string subcmd = "bet";

        //    jObjSend["cmd"] = cmd;
        //    jObjSend["show_id"] = show_id;
        //    jObjSend["token"] = login_token;
        //    jObjSend["subcmd"] = subcmd;
        //    jObjSend["money"] = money;

        //    Send(jObjSend);
        //}
        //public class OperationStrategyInfo
        //{
        //    public string bankerPoker { get; set; }
        //    public string robotPoker { get; set; }
        //    public string cmd { get; set; }
        //}
        //public List<OperationStrategyInfo> operationStrategyInfos => new List<OperationStrategyInfo>
        //{
        //    #region 5-8
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "5-8",
        //        cmd = "H"
        //    },
        // #endregion            

        //    #region 9
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "9",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "9",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "9",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "9",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "9",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "9",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "9",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "9",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "9",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "9",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 10
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "10",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "10",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 11
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "11",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "11",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "11",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 12
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "12",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "12",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "12",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "12",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 13-16
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "13-16",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "13-16",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "13-16",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "13-16",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 17-21
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "17-21",
        //        cmd = "S"
        //    },
        // #endregion

        //    #region A,2 A,3
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "A,2 A,3",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "A,2 A,3",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "A,2 A,3",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region A,4 A,5
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "A,4 A,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "A,4 A,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "A,4 A,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "A,4 A,5",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region A,6
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "A,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "A,6",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "A,6",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "A,6",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "A,6",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "A,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "A,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "A,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "A,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "A,6",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region A,7
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "A,7",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "A,7",
        //        cmd = "DS"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "A,7",
        //        cmd = "DS"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "A,7",
        //        cmd = "DS"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "A,7",
        //        cmd = "DS"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "A,7",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "A,7",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "A,7",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "A,7",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "A,7",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region A,8 10,10
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "A,8 10,10",
        //        cmd = "S"
        //    },
        // #endregion

        //    #region 2,2 3,3
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "2,2 3,3",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "2,2 3,3",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "2,2 3,3",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "2,2 3,3",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "2,2 3,3",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "2,2 3,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "2,2 3,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "2,2 3,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "2,2 3,3",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "2,2 3,3",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 4,4
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "4,4",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "4,4",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "4,4",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 5,5
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "5,5",
        //        cmd = "D"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "5,5",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "5,5",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 6,6
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "6,6",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "6,6",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "6,6",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "6,6",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "6,6",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "6,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "6,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "6,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "6,6",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "6,6",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 7,7
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "7,7",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "7,7",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "7,7",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "7,7",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "7,7",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "7,7",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "7,7",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "7,7",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "7,7",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "7,7",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 8,8
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "8,8",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "8,8",
        //        cmd = "H"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "8,8",
        //        cmd = "H"
        //    },
        // #endregion

        //    #region 9,9
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "9,9",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "9,9",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "9,9",
        //        cmd = "S"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "9,9",
        //        cmd = "S"
        //    },
        // #endregion

        //    #region A,A
        //    new OperationStrategyInfo{
        //        bankerPoker = "2",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "3",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "4",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "5",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "6",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "7",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "8",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "9",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "10",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    },
        //    new OperationStrategyInfo{
        //        bankerPoker = "A",
        //        robotPoker = "A,A",
        //        cmd = "P"
        //    }
        // #endregion
        //};
        //void GetBankerAndRobotPokersPointSendPokerState(JArray seats, ref int bankerPoints, ref int robotPoints, int[] robotPokersRet)
        //{
        //    bankerPoints = 0;
        //    var bankerPoker = (JObject)seats[0];
        //    var robotPokers = (JArray)seats[seatId];
        //    int temp = (int)bankerPoker["v"];
        //    bankerPoints += temp > 10 ? 10 : temp;

        //    for (int i = 0; i < robotPokers.Count; i++)
        //    {
        //        temp = (int)robotPokers[i]["v"];
        //        robotPoints += temp > 10 ? 10 : temp;
        //        temp = (int)robotPokers[i]["v"];
        //        robotPokersRet[i] = temp > 10 ? 10 : temp;
        //    }
        //}
        //void GetRobotPokersPointNotify(JArray pokers, ref int robotPoints, int[] robotPokersRet)
        //{
        //    robotPoints = 0;
        //    robotPokersRet = null;
        //    for (int i = 0; i < pokers.Count; i++)
        //    {
        //        var temp = (int)pokers[i]["v"];
        //        robotPoints += temp > 10 ? 10 : temp;
        //    }
        //}
        #endregion

        #region 火拼牛牛
        IEnumerator WaitNotifyGrabBanker()
        {
            Reset();

            string cmd = "notify_ingame";
            string subcmd = "call_banker";
            string subcmd2 = "who_is_banker";
            NetMessage netMessage = null;
            DateTime startWaitTime = DateTime.Now;

            while (true)
            {
                netMessage = FindNetMessage(cmd, subcmd);
                if (netMessage == null) netMessage = FindNetMessage(cmd, subcmd2);
                if (netMessage != null)
                {
                    if ((string)netMessage.jObjRecv["subcmd"] == "call_banker")
                    {
                        if ((string)netMessage.jObjRecv["show_id"] != show_id) continue;
                    }
                    break;
                }

                if ((DateTime.Now - startWaitTime).TotalSeconds > 15)
                {
                    break;
                }

                yield return null;
            }

            if (netMessage != null)
            {
                jObjRecv = netMessage.jObjRecv;
            }

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }
        IEnumerator WaitNotifyHpnnBetterBet()
        {

            Reset();

            string cmd = "notify_ingame";
            string subcmd = "better_bet";

            yield return WaitForNetMessage(cmd, subcmd, 15);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }

        }
        IEnumerator WaitNotifyHpnnShowPoker()
        {
            Reset();

            string cmd = "notify_ingame";
            string subcmd = "change_state";
            NetMessage netMessage = null;
            DateTime startWaitTime = DateTime.Now;

            while (true)
            {
                netMessage = FindNetMessage(cmd, subcmd);
                if (netMessage != null)
                {
                    if ((int)netMessage.jObjRecv["state"] != 5) continue;
                    break;
                }

                if ((DateTime.Now - startWaitTime).TotalSeconds > 10)
                {
                    break;
                }

                yield return null;
            }

            if (netMessage != null)
            {
                jObjRecv = netMessage.jObjRecv;
            }

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }
        IEnumerator PlayOther2Game()
        {
            while (true)
            {
                ClearNetMessages();
                yield return WaitForSeconds(0.1f);

                yield return GetTableInfo();
                if (ret_code != 0)
                    continue;

                int state = (int)jObjRecv["state"];
                if (state != 1)
                    continue;

                long time = (long)jObjRecv["time"];
                long nextstate_time = (long)jObjRecv["nextstate_time"];
                //确定剩余准备时间-3，得到可以准备的时间
                long canReadyTime = nextstate_time - time - 3000;

                if (canReadyTime < 1000)
                    continue;

                //准备
                yield return GetReady();
                if (ret_code != 0)
                    continue;

                yield return WaitNotifyGrabBanker();
                if (ret_code != 0)
                    continue;

                if ((string)jObjRecv["subcmd"] == "call_banker")
                {
                    bool isCallBanker = ownerMgr.rand.Next(callBankerProb * 100) < 10000;

                    yield return WaitForTimeStamp(ownerMgr.rand.Next(500, 3000));

                    yield return CallBanker(isCallBanker);
                    if (ret_code != 0)
                        continue;

                    if (!isCallBanker)
                    {
                        yield return WaitNotifyHpnnBetterBet();
                        if (ret_code != 0)
                            continue;

                        yield return WaitForTimeStamp(ownerMgr.rand.Next(500, 3000));

                        JArray jArray = (JArray)jObjRecv["bet_moneys"];
                        int[] probs = new int[jArray.Count];
                        for (int i = 0; i < probs.Length; i++)
                        {
                            probs[i] = betterBetProbs[i];
                        }

                        var index = RandomUtil.ProbChoose(ownerMgr.rand, probs);
                        yield return HpnnBet(index + 1);
                        if (ret_code != 0)
                            continue;
                    }
                }
                else
                {
                    yield return WaitNotifyHpnnBetterBet();

                    if (ret_code != 0)
                        continue;

                    yield return WaitForTimeStamp(ownerMgr.rand.Next(500, 3000));

                    JArray jArray = (JArray)jObjRecv["bet_moneys"];
                    int[] probs = new int[jArray.Count];
                    for (int i = 0; i < probs.Length; i++)
                    {
                        probs[i] = betterBetProbs[i];
                    }

                    var index = RandomUtil.ProbChoose(ownerMgr.rand, probs);
                    yield return HpnnBet(index + 1);
                    if (ret_code != 0)
                        continue;
                }

                yield return WaitNotifyHpnnShowPoker();
                if (ret_code != 0)
                    continue;

                yield return WaitForTimeStamp(ownerMgr.rand.Next(3000, 7000));

                yield return HpnnShowPoker();
                if (ret_code != 0)
                    continue;

                while (true)
                {
                    yield return WaitNotifySendResult();

                    if (ret_code == 0)
                    {
                        break;
                    }
                }

                if (jObjRecv != null)
                {
                    var jResults = (JArray)jObjRecv["result"];
                    foreach (var item in jResults)
                    {
                        if ((string)item["show_id"] == show_id)
                        {
                            money = (long)item["money"];
                        }
                    }

                    ////Console.WriteLine("{0} 结算后金额：{1}", nick, money);

                    if (money <= ownerMgr.minMoney || money > ownerMgr.maxMoney)
                    {
                        if (money <= ownerMgr.minMoney)
                        {
                            long increment = (long)RandomUtil.RandomMinMax(ownerMgr.rand, ownerMgr.addMoneyMin, ownerMgr.addMoneyMax);
                            //Console.WriteLine("{0} 补充金币：{1}", nick, increment);
                            yield return GodInstructSetMoney(money + increment);
                        }
                        //50%概率离开房间
                        if (ownerMgr.rand.Next(10000) < 5000)
                        {
                            //Console.WriteLine("{0} 离开房间", nick);
                            yield return WaitForTimeStamp(ownerMgr.rand.Next(1000, 2000));
                            break;
                        }
                    }
                }
            }
        }

        //准备
        IEnumerator GetReady()
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "get_ready";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }
        //叫庄
        IEnumerator CallBanker(bool isCall)
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "call_banker";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;
            jObjSend["is_call"] = isCall;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }
        //押注
        IEnumerator HpnnBet(int muti)
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "bet";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;
            jObjSend["muti"] = muti;

            Send(jObjSend);

            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }
        //现牌
        IEnumerator HpnnShowPoker()
        {
            Reset();

            JObject jObjSend = new JObject();
            string cmd = "operate_ingame";
            string subcmd = "show_poker";

            jObjSend["cmd"] = cmd;
            jObjSend["show_id"] = show_id;
            jObjSend["token"] = login_token;
            jObjSend["subcmd"] = subcmd;

            Send(jObjSend);
            yield return WaitForNetMessage(cmd, subcmd);

            if (jObjRecv != null)
            {
                ret_code = (int)jObjRecv["ret_code"];
            }
        }
        #endregion
    }
}
