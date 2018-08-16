using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;
using GameInterface;
using MixLibrary;

namespace GameServer
{
    public class Table : ITable
    {
        static int tableIdCounter = 1;
        public int id;
        public DatabaseService.DatabaseLink dbLink;
        public Room room;
        public IGameLogic gameLogic;
        public Dictionary<string, Player> players = new Dictionary<string, Player>();
        public bool isShowInfo = false;
        int playerCount = 0;
        int enableApplyNewConfigs = 0;
        public int PlayerCount
        {
            get
            {
                Thread.MemoryBarrier();
                return playerCount;
            }
            set
            {
                int compare;

                do
                {
                    compare = playerCount;
                } while (Interlocked.CompareExchange(ref playerCount, value, compare) != compare);
            }
        }

        public int RemainSeatCount
        {
            get
            {
                int remain = room.gameInfo.seatCount - PlayerCount;

                if (remain < 0)
                    remain = 0;

                return remain;
            }
        }

        public int EnableApplyNewConfigs
        {
            get
            {
                Thread.MemoryBarrier();
                return enableApplyNewConfigs;
            }
            set
            {
                int compare;

                do
                {
                    compare = enableApplyNewConfigs;
                } while (Interlocked.CompareExchange(ref enableApplyNewConfigs, value, compare) != compare);
            }
        }
        
        public Table(Room room)
        {
            id = tableIdCounter++;
            this.room = room;

            try
            {
                gameLogic = Activator.CreateInstance(room.gameInfo.gameLogicType) as IGameLogic;
                gameLogic.ApplyNewConfigs(room.gameInfo.jConfigs, room.grade);
                gameLogic.Init(this, Program.dbHelper);
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex.Message);
                LogUtil.Log(ex.StackTrace);
            }
        }
        public int GetTableId()
        {
            return id;
        }
        public string GetGameName()
        {
            return room.gameInfo.name;
        }
        public string GetDllFile()
        {
            return room.gameInfo.dllFile;
        }
        public int GetGrade()
        {
            return room.grade;
        }
        public JObject GetGradeData()
        {
            var roomGrade = room.gameInfo.roomGrades[room.grade];

            return roomGrade.data;
        }
        public int GetSeatCount()
        {
            return room.gameInfo.seatCount;
        }
        public List<string> GetPlayers()
        {
            List<string> ret = new List<string>();

            foreach (var show_id in players.Keys)
            {
                ret.Add(show_id);
            }

            return ret;
        }

        public List<string> GetRobots()
        {
            List<string> ret = new List<string>();

            foreach (var kv in players)
            {
                if(kv.Value.is_robot)
                    ret.Add(kv.Key);
            }

            return ret;
        }

        public long GetStore()
        {
            return Program.dbHelper.GetStore(dbLink, room.gameInfo.name, room.grade);
        }

        public bool SetStore(long store)
        {
            int id = 0;

            using (var reader = Program.dbHelper.GetBackControl(dbLink, "set store", room.gameInfo.name, room.grade))
            {
                if(reader != null && reader.Read())
                {
                    store = long.Parse(reader.GetStringSafe("param"));
                    id = reader.GetInt32Safe("ID");
                }
            }

            if(id > 0)
                Program.dbHelper.EffectiveBackControl(dbLink, id);

            return Program.dbHelper.SetStore(dbLink, room.gameInfo.name, room.grade, store);
        }

        public long GetJackpot()
        {
            return Program.dbHelper.GetJackpot(dbLink, room.gameInfo.name, room.grade);
        }

        public bool SetJackpot(long jackpot)
        {
            int id = 0;

            using (var reader = Program.dbHelper.GetBackControl(dbLink, "set jackpot", room.gameInfo.name, room.grade))
            {
                if (reader != null && reader.Read())
                {
                    jackpot = long.Parse(reader.GetStringSafe("param"));
                    id = reader.GetInt32Safe("ID");
                }
            }

            if (id > 0)
                Program.dbHelper.EffectiveBackControl(dbLink, id);

            return Program.dbHelper.SetJackpot(dbLink, room.gameInfo.name, room.grade, jackpot);
        }

        public int GetBigAwardCounter()
        {
            return Program.dbHelper.GetBigAwardCounter(dbLink, room.gameInfo.name, room.grade);
        }

        public bool SetBigAwardCounter(int counter)
        {
            int id = 0;

            using (var reader = Program.dbHelper.GetBackControl(dbLink, "set bigaward counter", room.gameInfo.name, room.grade))
            {
                if (reader != null && reader.Read())
                {
                    counter = int.Parse(reader.GetStringSafe("param"));
                    id = reader.GetInt32Safe("ID");
                }
            }

            if (id > 0)
                Program.dbHelper.EffectiveBackControl(dbLink, id);

            return Program.dbHelper.SetBigAwardCounter(dbLink, room.gameInfo.name, room.grade, counter);
        }

        public long GetProfit(string show_id)
        {
            return Program.dbHelper.GetProfit(dbLink, room.gameInfo.name, room.grade, show_id);
        }
        public bool SetProfit(string show_id, long profit)
        {
            return Program.dbHelper.SetProfit(dbLink, room.gameInfo.name, room.grade, show_id, profit);
        }

        public long GetForce(string show_id)
        {
            return Program.dbHelper.GetForce(dbLink, room.gameInfo.name, room.grade, show_id);
        }
        public bool SetForce(string show_id, long force)
        {
            return Program.dbHelper.SetForce(dbLink, room.gameInfo.name, room.grade, show_id, force);
        }

        public JObject GetUserGameData(string show_id)
        {
            return Program.dbHelper.GetUserGameData(dbLink, room.gameInfo.name, room.grade, show_id);
        }

        public bool SetUserGameData(string show_id, JObject jData)
        {
            return Program.dbHelper.SetUserGameData(dbLink, room.gameInfo.name, room.grade, show_id, jData);
        }

        public bool LogPlayGame(string action, string show_id, JObject jData)
        {
            return Program.dbHelper.LogPlayGame(room.gameInfo.baseDir, action, show_id, room.gameInfo.name, room.grade, id, jData);
        }
        public bool LogGame(string action, JObject jData)
        {
            return Program.dbHelper.LogGame(room.gameInfo.baseDir, action, room.gameInfo.name, room.grade, id, jData);
        }

        public JArray QuerySQLite(string sql)
        {
            return Program.dbHelper.QuerySQLite(room.gameInfo.baseDir, sql);
        }

        public void Print(string format, params object[] args)
        {
            if (!isShowInfo)
                return;

            string content = string.Format(format, args);

            Console.WriteLine("[{0}/{1}场/{2}桌] {3}", room.gameInfo.name, room.grade, id, content);
        }

        public bool Kick(string show_id)
        {
            Player player = GetPlayer(show_id);

            if (player == null)
                return false;

            RemovePlayer(player, false);

            player.session.TableId = 0;

            NotifyJoinLeave("2", player.show_id);

            return true;
        }
        public void NotifyIngame(string subcmd, JObject jObjSend)
        {
            jObjSend["cmd"] = "notify_ingame";
            jObjSend["ret_code"] = 0;
            jObjSend["subcmd"] = subcmd;

            Broadcast(jObjSend);
        }

        public void NotifyIngame(string show_id, string subcmd, JObject jObjSend)
        {
            Player player = GetPlayer(show_id);

            if (player == null)
                return;

            jObjSend["cmd"] = "notify_ingame";
            jObjSend["ret_code"] = 0;
            jObjSend["subcmd"] = subcmd;

            Program.server.Send(player.session, jObjSend);
        }

        public void SetUserLastTable(string show_id)
        {
            string last_server = string.Format("{0}:{1}", Configure.Inst.outerIp, Configure.Inst.serverPort);
            string last_game = GetGameName();
            int last_grade = GetGrade();

            Program.dbHelper.SetUserLastTable(dbLink, last_server, last_game, last_grade, id, show_id);
        }

        public void UnsetUserLastTable(string show_id)
        {
            Program.dbHelper.SetUserLastTable(dbLink, "", "", -1, 0, show_id);
        }

        public DatabaseService.DatabaseLink GetDbLink()
        {
            return dbLink;
        }

        public void Update()
        {
            ReloadConfigs();
            gameLogic.Update();
        }

        Player GetPlayer(string show_id)
        {
            if (show_id.Length == 0)
                return null;

            Player value = null;

            if(!players.TryGetValue(show_id, out value))
                return null;

            return value;
        }

        void AddPlayer(string show_id, Player player)
        {
            player.show_id = show_id;
            players[show_id] = player;
            PlayerCount = players.Count;

            gameLogic.OnAddPlayer(show_id);
        }

        void RemovePlayer(string show_id, bool isCauseDisconnect)
        {
            players.Remove(show_id);
            PlayerCount = players.Count;

            gameLogic.OnRemovePlayer(show_id, isCauseDisconnect);
        }

        void RemovePlayer(Player player, bool isCauseDisconnect)
        {
            RemovePlayer(player.show_id, isCauseDisconnect);
        }

        Player GetPlayerFromSession(GameServerSession session)
        {
            foreach (var player in players.Values)
            {
                if (player.session == session)
                    return player;
            }

            return null;
        }

        public bool Join(GameServerSession session, string show_id)
        {
            //人数已满
            if (RemainSeatCount == 0)
                return false;

            Player player = GetPlayer(show_id);

            if(player == null)
            {
                player = new Player();
                player.is_robot = Program.dbHelper.UserIsRobot(dbLink, show_id);
                AddPlayer(show_id, player);
            }

            player.session = session;

            NotifyJoinLeave("1", player.show_id);

            return true;
        }

        public bool Leave(GameServerSession session, bool isCauseDisconnect)
        {
            Player player = GetPlayerFromSession(session);

            //不存在该玩家
            if (player == null)
                return false;

            RemovePlayer(player, isCauseDisconnect);

            session.TableId = 0;

            NotifyJoinLeave("2", player.show_id);

            return true;
        }

        void NotifyJoinLeave(string type, string show_id)
        {
            JObject jObj = new JObject();

            jObj["cmd"] = "notify_join_leave";
            jObj["ret_code"] = 0;
            jObj["type"] = type;
            jObj["show_id"] = show_id;

            Broadcast(jObj);
        }

        void Broadcast(JObject jObj)
        {
            foreach(var player in players.Values)
            {
                Program.server.Send(player.session, jObj);
            }
        }

        public void OperateIngame(string player, string subcmd, JObject jObjRecv, JObject jObjSend)
        {
            ReloadConfigs();
            gameLogic.OperateIngame(player, subcmd, jObjRecv, jObjSend);
        }

        void ReloadConfigs()
        {
            if(EnableApplyNewConfigs == 1)
            {
                gameLogic.ApplyNewConfigs(room.gameInfo.jConfigs, room.grade);
                EnableApplyNewConfigs = 0;

                Console.WriteLine("游戏【{0}】桌号：{1}已重新加载配置", 
                    room.gameInfo.name, id);
            }
        }
    }
}
