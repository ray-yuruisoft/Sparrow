using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.IO;

namespace GameServer
{
    public class GameModule
    {
        public class RoomGrade
        {
            public int grade;
            public long moneyLimit;
            public List<Room> rooms = new List<Room>();
            public JObject data = new JObject();
        }
        public class GameInfo
        {
            public string name;
            public string dllFile;
            public string baseDir;
            public Type gameLogicType;
            public MethodInfo methodLoadConfigs;
            public JObject jConfigs;
            public RoomGrade[] roomGrades;
            public int roomCount;
            public int tableCount;
            public int seatCount;
        }

        public Dictionary<string, GameInfo> gameInfos = new Dictionary<string, GameInfo>();
        public Dictionary<int, Table> idToTableDict = new Dictionary<int, Table>();
        public void Start()
        {
            var dbLink = Program.dbSvc.GetLink(0);

            string[] gameNames = Configure.Inst.supportGames.Split(new string[] { ",", "，" }, 
                StringSplitOptions.RemoveEmptyEntries);

            foreach(var gameName in gameNames)
            {
                //创建预设房间
                CreateRooms(dbLink, gameName);

                //注册服务器
                string ip_port = Configure.Inst.outerIp + ":" + Configure.Inst.serverPort;
                Program.dbHelper.RegGameServer(dbLink, gameName, ip_port);
            }

            Program.moduleManager.RegisterRequestHandler("get_game_table", OnReqGetGameTable);
            Program.moduleManager.RegisterRequestHandler("join_table", OnReqJoinTable);
            Program.moduleManager.RegisterRequestHandler("quick_join_table", OnReqQuickJoinTable);
            Program.moduleManager.RegisterRequestHandler("leave_table", OnReqLeaveTable);
            Program.moduleManager.RegisterRequestHandler("operate_ingame", OnReqOperateIngame);
            Program.moduleManager.RegisterRequestHandler("god_instruct", OnReqGodInstruct);
            Program.moduleManager.RegisterRequestHandler("test_db", OnReqTestDB);
        }

        void CreateRooms(DatabaseService.DatabaseLink dbLink, string gameName)
        {
            using (var reader = Program.dbHelper.GetGameInfo(dbLink, gameName))
            {
                if(reader == null)
                {
                    return;
                }

                if(!reader.Read())
                {
                    return;
                }

                GameInfo gameInfo = new GameInfo();

                gameInfo.name = gameName;
                gameInfo.dllFile = reader.GetStringSafe("dll_file");

                gameInfo.baseDir = string.Format(@"{0}Games\{1}", AppDomain.CurrentDomain.BaseDirectory, gameInfo.dllFile);
                var assembly = Assembly.LoadFile(string.Format(@"{0}\{1}.dll", gameInfo.baseDir, gameInfo.dllFile));
                gameInfo.gameLogicType = assembly.GetType("Game.GameLogic");
                gameInfo.methodLoadConfigs = gameInfo.gameLogicType.GetMethod("LoadConfigs", BindingFlags.Static | BindingFlags.Public);
                gameInfo.jConfigs = (JObject)gameInfo.methodLoadConfigs.Invoke(null, new object[] { gameInfo.baseDir });

                gameInfo.roomCount = reader.GetInt32Safe("room_count");
                gameInfo.tableCount = reader.GetInt32Safe("table_count");
                gameInfo.seatCount = reader.GetInt32Safe("seat_count");

                int gradeCount = reader.GetInt32Safe("grade_count");
                bool isNeedUpdate = reader.GetBoolSafe("need_update");

                gameInfo.roomGrades = new RoomGrade[gradeCount];

                for (int i = 0; i < gradeCount; i++)
                {
                    RoomGrade roomGrade = new RoomGrade();

                    roomGrade.grade = i;
                    roomGrade.moneyLimit = reader.GetInt64Safe("grade" + i + "_money");

                    for (int j = 0; j < gameInfo.roomCount; j++)
                    {
                        Room room = new Room(gameInfo, roomGrade.grade, idToTableDict, isNeedUpdate);

                        roomGrade.rooms.Add(room);
                    }

                    gameInfo.roomGrades[i] = roomGrade;
                }

                gameInfos[gameName] = gameInfo;
            }
        }

        public void Stop()
        {
            
        }

        public void LoadAllConfigs()
        {
            foreach(var gameInfo in gameInfos.Values)
            {
                gameInfo.jConfigs = (JObject)gameInfo.methodLoadConfigs.Invoke(null, new object[] { gameInfo.baseDir });
            }

            foreach (var table in idToTableDict.Values)
            {
                table.EnableApplyNewConfigs = 1;
            }
        }

        public void SetAllGameIsShowInfo(bool isShow)
        {
            foreach(var table in idToTableDict.Values)
            {
                table.isShowInfo = isShow;
            }
        }

        public void OnAccepted(int workerIndex, GameServerSession session)
        {

        }
        public void OnClosed(int workerIndex, GameServerSession session, string closedCause, bool isInternalCause)
        {
            //桌子分多线处理
            if (session.TableId > 0)
            {
                var table = GetTableFromId(session.TableId);
                if(table != null)
                    table.Leave(session, true);
            }
        }

        GameInfo GetGameInfo(string game_name)
        {
            GameInfo value = null;

            if (!gameInfos.TryGetValue(game_name, out value))
                return null;

            return value;
        }

        Table GetTableFromId(int table_id)
        {
            Table value = null;

            if (!idToTableDict.TryGetValue(table_id, out value))
                return null;

            return value;
        }

        bool CheckToken(DatabaseService.DatabaseLink dbLink, string show_id, string token)
        {
            if (Program.dbHelper.GetUserLoginToken(dbLink, show_id) != token)
            {
                return false;
            }

            return true;
        }

        void OnReqGetGameTable(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string game_name = jObjRecv["game_name"].ToString();
            int grade = int.Parse(jObjRecv["grade"].ToString());

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (game_name.Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            GameInfo gameInfo = GetGameInfo(game_name);

            if (gameInfo == null)
            {
                Program.server.SendError(session, cmd, "未找到该游戏");
                return;
            }

            if (grade < 0 || grade >= gameInfo.roomGrades.Length)
            {
                Program.server.SendError(session, cmd, "未找到该场次");
                return;
            }

            RoomGrade roomGrade = gameInfo.roomGrades[grade];

            long money = Program.dbHelper.GetUserMoney(dbLink, show_id);

            if(money < roomGrade.moneyLimit)
            {
                Program.server.SendError(session, cmd, "入场金币限制");
                return;
            }

            foreach (var room in roomGrade.rooms)
            {
                Table table = room.AllotTable();

                //该房间全满了
                if (table == null)
                    continue;

                session.TableId = table.id;

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;
                jObj["table_id"] = table.id;

                Program.server.Send(session, jObj);
                return;
            }

            Program.server.SendError(session, cmd, "未找到合适的桌子");
        }

        void OnReqJoinTable(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            int table_id = int.Parse(jObjRecv["table_id"].ToString());

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (session.TableId == 0 || session.TableId != table_id)
            {
                Program.server.SendError(session, cmd, "未找到该桌子");
                return;
            }

            Table table = GetTableFromId(table_id);

            if(table == null)
            {
                Program.server.SendError(session, cmd, "未找到该房间");
                return;
            }

            if (!table.Join(session, show_id))
            {
                Program.server.SendError(session, cmd, "房间已满");
                return;
            }

            Program.server.SendSuccess(session, cmd);
        }

        void OnReqQuickJoinTable(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            int table_id = int.Parse(jObjRecv["table_id"].ToString());

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            Table table = GetTableFromId(table_id);

            if (table == null)
            {
                Program.server.SendError(session, cmd, "找不到该房间");
                return;
            }

            if (!table.Join(session, show_id))
            {
                Program.server.SendError(session, cmd, "房间已满");
                return;
            }

            session.TableId = table.id;

            Program.server.SendSuccess(session, cmd);
        }
        //桌子分多线处理
        void OnReqLeaveTable(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (session.TableId == 0)
                return;

            var table = GetTableFromId(session.TableId);
            if (table == null)
                return;

            if(!table.Leave(session, false))
            {
                Program.server.SendError(session, cmd, "不存在该玩家");
                return;
            }

            Program.server.SendSuccess(session, cmd);
        }

        void OnReqOperateIngame(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string subcmd = jObjRecv["subcmd"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (subcmd.Length == 0)
                return;

            if (session.TableId == 0)
                return;

            var table = GetTableFromId(session.TableId);
            if (table == null)
                return;

            JObject jObjSend = new JObject();

            jObjSend["cmd"] = cmd;
            jObjSend["subcmd"] = subcmd;

            table.OperateIngame(show_id, subcmd, jObjRecv, jObjSend);

            Program.server.Send(session, jObjSend);
        }

        void OnReqGodInstruct(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string subcmd = jObjRecv["subcmd"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (subcmd.Length == 0)
                return;

            if (!Program.dbHelper.UserIsRobot(dbLink, show_id))
                return;

            if(subcmd == "set_money")
            {
                long money = (long)jObjRecv["money"];

                Program.dbHelper.SetUserMoney(dbLink, money, show_id);
            }

            JObject jObjSend = new JObject();

            jObjSend["cmd"] = cmd;
            jObjSend["subcmd"] = subcmd;
            jObjSend["ret_code"] = 0;

            Program.server.Send(session, jObjSend);
        }
        
        void OnReqTestDB(int workerIndex, GameServerSession session, string cmd, JObject jObjRecv)
        {
            Random rand = new Random();
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            long money = (long)RandomUtil.RandomMinMax(rand, 1000000, 2000000);

            Program.dbHelper.SetUserMoney(dbLink, money, show_id);

            Program.server.SendSuccess(session, cmd);
        }
    }
}
