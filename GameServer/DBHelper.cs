using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SQLite;
using GameInterface;

namespace GameServer
{
    public class DBHelper : IDBHelper
    {
        public class GameLog
        {
            public bool isPlayGame; 
            public string baseDir;
            public string action;
            public string show_id;
            public string game_name;
            public int grade;
            public int table_id;
            public JObject jData;
        }

        ConcurrentQueue<GameLog> gameLogQueue = new ConcurrentQueue<GameLog>();
        AutoResetEvent gameLogQueueEvent = new AutoResetEvent(false);
        public void Start()
        {
            Program.dbSvc.SetPrepareCommand("get lock", "select get_lock(@para1, @para2)", MySqlDbType.String, MySqlDbType.Int32);
            Program.dbSvc.SetPrepareCommand("release lock", "select release_lock(@para1)", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get user base info", "select * from cp_user.user where show_id=@para1 limit 1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user money", "update cp_user.user set money=@para1 where show_id=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("reg game server", "insert into cp_user.game_server (game_name, ip_port) values (@para1, @para2)", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get game info", "select * from cp_user.game where name=@para1 limit 1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game store 0", "update cp_user.game set grade0_store=@para1 where name=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game store 1", "update cp_user.game set grade1_store=@para1 where name=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game store 2", "update cp_user.game set grade2_store=@para1 where name=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game jackpot 0", "update cp_user.game set grade0_jackpot=@para1 where name=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game jackpot 1", "update cp_user.game set grade1_jackpot=@para1 where name=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game jackpot 2", "update cp_user.game set grade2_jackpot=@para1 where name=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game bigaward counter 0", "update cp_user.game set grade0_bigaward_counter=@para1 where name=@para2", MySqlDbType.Int32, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game bigaward counter 1", "update cp_user.game set grade1_bigaward_counter=@para1 where name=@para2", MySqlDbType.Int32, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set game bigaward counter 2", "update cp_user.game set grade2_bigaward_counter=@para1 where name=@para2", MySqlDbType.Int32, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get back control", "select * from cp_user.back_control where cmd=@para1 and game_name=@para2 and grade=@para3 and is_effective=0 limit 1", 
                MySqlDbType.String, MySqlDbType.String, MySqlDbType.Byte);
            Program.dbSvc.SetPrepareCommand("effective back control", "update cp_user.back_control set is_effective=1,occur_time=@para1 where ID=@para2",
                MySqlDbType.DateTime, MySqlDbType.Int32);
            Program.dbSvc.SetPrepareCommand("get user game info", "select * from cp_user.user_game where game_name=@para1 and game_grade=@para2 and show_id=@para3 limit 1", 
                MySqlDbType.String, MySqlDbType.Byte, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user game profit", "insert into cp_user.user_game (game_name, game_grade, show_id, profit) values(@para1, @para2, @para3, @para4) on duplicate key update profit=@para5",
                MySqlDbType.String, MySqlDbType.Byte, MySqlDbType.String, MySqlDbType.Int64, MySqlDbType.Int64);
            Program.dbSvc.SetPrepareCommand("set user game force", "insert into cp_user.user_game (game_name, game_grade, show_id, force_value) values(@para1, @para2, @para3, @para4) on duplicate key update force_value=@para5",
                MySqlDbType.String, MySqlDbType.Byte, MySqlDbType.String, MySqlDbType.Int64, MySqlDbType.Int64);
            Program.dbSvc.SetPrepareCommand("set user game data", "insert into cp_user.user_game (game_name, game_grade, show_id, data) values(@para1, @para2, @para3, @para4) on duplicate key update data=@para5",
                MySqlDbType.String, MySqlDbType.Byte, MySqlDbType.String, MySqlDbType.String, MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("add notice", "insert into cp_user.notice (create_time, type, title, content) values (@para1, @para2, @para3, @para4)",
                MySqlDbType.DateTime,
                MySqlDbType.Byte,
                MySqlDbType.String,
                MySqlDbType.String
                );

            Program.dbSvc.SetPrepareCommand("set user last table", "update cp_user.user set last_server=@para1, last_game=@para2, last_grade=@para3, last_table_id=@para4 where show_id=@para5",
                MySqlDbType.String, MySqlDbType.String, MySqlDbType.Byte, MySqlDbType.Int32, MySqlDbType.String);

            Thread thread = new Thread(GameLogThreadProc);
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Normal;
            thread.Start();
        }

        public void Stop()
        {

        }

        public bool GetLock(DatabaseService.DatabaseLink dbLink, string key)
        {
            using (var reader = dbLink.ExecuteReader("get lock", key, 500))
            {
                if (reader != null && reader.Read())
                    return reader.GetBoolSafe(0);
            }

            return false;
        }

        public bool ReleaseLock(DatabaseService.DatabaseLink dbLink, string key)
        {
            using (var reader = dbLink.ExecuteReader("release lock", key))
            {
                if (reader != null && reader.Read())
                    return reader.GetBoolSafe(0);
            }

            return false;
        }

        public MySqlDataReader GetUserBaseInfo(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            return dbLink.ExecuteReader("get user base info", show_id);
        }

        public string GetUserLoginToken(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("login_token");
            }
        }

        public string GetUserNick(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("nick");
            }
        }

        public long GetUserMoney(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return 0;
                }

                return reader.GetInt64Safe("money");
            }
        }
        public bool SetUserMoney(DatabaseService.DatabaseLink dbLink, long money, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user money", money, show_id) > 0;
        }

        public bool SetUserLastTable(DatabaseService.DatabaseLink dbLink, string last_server, string last_game, int last_grade, int last_table_id, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user last table", last_server, last_game, last_grade, last_table_id, show_id) > 0;
        }

        public bool UserIsRobot(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return false;
                }

                return reader.GetBoolSafe("is_robot");
            }
        }
        public long GetStore(DatabaseService.DatabaseLink dbLink, string game_name, int grade)
        {
            using (var reader = dbLink.ExecuteReader("get game info", game_name))
            {
                if (reader == null)
                {
                    return 0;
                }

                if (!reader.Read())
                {
                    return 0;
                }

                return reader.GetInt64Safe(string.Format("grade{0}_store", grade));
            }
        }

        public bool SetStore(DatabaseService.DatabaseLink dbLink, string game_name, int grade, long store)
        {
            return dbLink.ExecuteNonQuery("set game store " + grade, store, game_name) > 0;
        }

        public long GetJackpot(DatabaseService.DatabaseLink dbLink, string game_name, int grade)
        {
            using (var reader = dbLink.ExecuteReader("get game info", game_name))
            {
                if (reader == null)
                {
                    return 0;
                }

                if (!reader.Read())
                {
                    return 0;
                }

                return reader.GetInt64Safe(string.Format("grade{0}_jackpot", grade));
            }
        }

        public bool SetJackpot(DatabaseService.DatabaseLink dbLink, string game_name, int grade, long jackpot)
        {
            return dbLink.ExecuteNonQuery("set game jackpot " + grade, jackpot, game_name) > 0;
        }

        public int GetBigAwardCounter(DatabaseService.DatabaseLink dbLink, string game_name, int grade)
        {
            using (var reader = dbLink.ExecuteReader("get game info", game_name))
            {
                if (reader == null)
                {
                    return 0;
                }

                if (!reader.Read())
                {
                    return 0;
                }

                return reader.GetInt32Safe(string.Format("grade{0}_bigaward_counter", grade));
            }
        }

        public bool SetBigAwardCounter(DatabaseService.DatabaseLink dbLink, string game_name, int grade, long counter)
        {
            return dbLink.ExecuteNonQuery("set game bigaward counter " + grade, counter, game_name) > 0;
        }

        public long GetProfit(DatabaseService.DatabaseLink dbLink, string game_name, int grade, string show_id)
        {
            using (var reader = dbLink.ExecuteReader("get user game info", game_name, grade, show_id))
            {
                if (reader == null)
                {
                    return 0;
                }

                if (!reader.Read())
                {
                    return 0;
                }

                return reader.GetInt64Safe("profit");
            }
        }

        public bool SetProfit(DatabaseService.DatabaseLink dbLink, string game_name, int grade, string show_id, long profit)
        {
            return dbLink.ExecuteNonQuery("set user game profit", game_name, grade, show_id, profit, profit) > 0;
        }

        public long GetForce(DatabaseService.DatabaseLink dbLink, string game_name, int grade, string show_id)
        {
            using (var reader = dbLink.ExecuteReader("get user game info", game_name, grade, show_id))
            {
                if (reader == null)
                {
                    return 0;
                }

                if (!reader.Read())
                {
                    return 0;
                }

                return reader.GetInt64Safe("force_value");
            }
        }

        public bool SetForce(DatabaseService.DatabaseLink dbLink, string game_name, int grade, string show_id, long force)
        {
            return dbLink.ExecuteNonQuery("set user game force", game_name, grade, show_id, force, force) > 0;
        }

        public JObject GetUserGameData(DatabaseService.DatabaseLink dbLink, string game_name, int grade, string show_id)
        {
            using (var reader = dbLink.ExecuteReader("get user game info", game_name, grade, show_id))
            {
                if (reader == null)
                {
                    return null;
                }

                if (!reader.Read())
                {
                    return null;
                }

                string jsonData =  reader.GetStringSafe("data");

                if (jsonData.Length == 0)
                    return null;

                return JObject.Parse(jsonData);
            }
        }

        public bool SetUserGameData(DatabaseService.DatabaseLink dbLink, string game_name, int grade, string show_id, JObject jData)
        {
            string jsonData = jData.ToString(Formatting.None);

            return dbLink.ExecuteNonQuery("set user game data", game_name, grade, show_id, jsonData, jsonData) > 0;
        }

        public bool RegGameServer(DatabaseService.DatabaseLink dbLink, string game_name, string ip_port)
        {
            return dbLink.ExecuteNonQuery("reg game server", game_name, ip_port) > 0;
        }

        public MySqlDataReader GetGameInfo(DatabaseService.DatabaseLink dbLink, string game_name)
        {
            return dbLink.ExecuteReader("get game info", game_name);
        }

        public MySqlDataReader GetBackControl(DatabaseService.DatabaseLink dbLink, string cmd, string game_name, int grade)
        {
            return dbLink.ExecuteReader("get back control", cmd, game_name, grade);
        }

        public bool EffectiveBackControl(DatabaseService.DatabaseLink dbLink, int id)
        {
            return dbLink.ExecuteNonQuery("effective back control", DateTime.Now, id) > 0;
        }

        public bool AddNotice(DatabaseService.DatabaseLink dbLink, int type, string title, string content)
        {
            return dbLink.ExecuteNonQuery("add notice", DateTime.Now, type, title, content) > 0;
        }

        public bool LogPlayGame(string baseDir, string action, string show_id, string game_name, int grade, int table_id, JObject jData)
        {
            GameLog gameLog = new GameLog();

            gameLog.isPlayGame = true;
            gameLog.baseDir = baseDir;
            gameLog.action = action;
            gameLog.show_id = show_id;
            gameLog.game_name = game_name;
            gameLog.grade = grade;
            gameLog.table_id = table_id;
            gameLog.jData = jData;

            PushGameLog(gameLog);

            return true;
        }
        public bool LogGame(string baseDir, string action, string game_name, int grade, int table_id, JObject jData)
        {
            GameLog gameLog = new GameLog();

            gameLog.isPlayGame = false;
            gameLog.baseDir = baseDir;
            gameLog.action = action;
            gameLog.game_name = game_name;
            gameLog.grade = grade;
            gameLog.table_id = table_id;
            gameLog.jData = jData;

            PushGameLog(gameLog);

            return true;
        }

        void PushGameLog(GameLog gameLog)
        {
            gameLogQueue.Enqueue(gameLog);
            gameLogQueueEvent.Set();
        }

        public JArray QuerySQLite(string baseDir, string sql)
        {
            string path = baseDir + @"\game_log.db";

            SQLiteConnection connection = new SQLiteConnection("data source=" + path);

            connection.Open();

            SQLiteCommand cmd = new SQLiteCommand(sql, connection);

            SQLiteDataReader reader = cmd.ExecuteReader();
            JArray jRet = new JArray();

            while (reader.Read())
            {
                JArray jRow = new JArray();

                for(int i = 0; i < reader.FieldCount; i++)
                {
                    jRow.Add(reader.GetString(i));
                }

                jRet.Add(jRow);
            }

            reader.Close();

            connection.Close();

            return jRet;
        }

        void GameLogThreadProc()
        {
            while (true)
            {
                GameLog gameLog = null;

                try
                {
                    if (gameLogQueue.Count > 0)
                    {
                        if (gameLogQueue.TryDequeue(out gameLog))
                        {
                            string path = gameLog.baseDir + @"\game_log.db";

                            SQLiteConnection connection = new SQLiteConnection("data source=" + path);

                            connection.Open();

                            SQLiteCommand cmd = new SQLiteCommand();
                            cmd.Connection = connection;
                            cmd.CommandText = "PRAGMA synchronous = OFF;";
                            cmd.ExecuteNonQuery();

                            if(gameLog.isPlayGame)
                            {
                                cmd = new SQLiteCommand("insert into play_game values(@val1, @val2, @val3, @val4, @val5, @val6, @val7)", connection);
                                cmd.Parameters.Add("@val1", DbType.DateTime);
                                cmd.Parameters.Add("@val2", DbType.String);
                                cmd.Parameters.Add("@val3", DbType.String);
                                cmd.Parameters.Add("@val4", DbType.String);
                                cmd.Parameters.Add("@val5", DbType.Int32);
                                cmd.Parameters.Add("@val6", DbType.Int32);
                                cmd.Parameters.Add("@val7", DbType.String);
                            }
                            else
                            {
                                cmd = new SQLiteCommand("insert into game values(@val1, @val2, @val3, @val4, @val5, @val6)", connection);
                                cmd.Parameters.Add("@val1", DbType.DateTime);
                                cmd.Parameters.Add("@val2", DbType.String);
                                cmd.Parameters.Add("@val3", DbType.String);
                                cmd.Parameters.Add("@val4", DbType.Int32);
                                cmd.Parameters.Add("@val5", DbType.Int32);
                                cmd.Parameters.Add("@val6", DbType.String);
                            }

                            cmd.Prepare();

                            if (gameLog.isPlayGame)
                            {
                                cmd.Parameters[0].Value = DateTime.Now;
                                cmd.Parameters[1].Value = gameLog.action;
                                cmd.Parameters[2].Value = gameLog.show_id;
                                cmd.Parameters[3].Value = gameLog.game_name;
                                cmd.Parameters[4].Value = gameLog.grade;
                                cmd.Parameters[5].Value = gameLog.table_id;
                                cmd.Parameters[6].Value = gameLog.jData.ToString(Formatting.None);
                            }
                            else
                            {
                                cmd.Parameters[0].Value = DateTime.Now;
                                cmd.Parameters[1].Value = gameLog.action;
                                cmd.Parameters[2].Value = gameLog.game_name;
                                cmd.Parameters[3].Value = gameLog.grade;
                                cmd.Parameters[4].Value = gameLog.table_id;
                                cmd.Parameters[5].Value = gameLog.jData.ToString(Formatting.None);
                            }

                            int rowAffected = cmd.ExecuteNonQuery();

                            connection.Close();
                        }
                    }
                    else
                    {
                        gameLogQueueEvent.WaitOne(100);
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Log(ex.Message);
                    LogUtil.Log(ex.StackTrace);
                }
            }
        }
    }
}
