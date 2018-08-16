using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Threading;
using MySql.Data;
using MySql.Data.MySqlClient;


namespace MixLibrary
{
    public class DatabaseService
    {
        public class DatabaseLink
        {
            public class PrepareCommandInfo
            {
                public string Name;
                public string Sql;
                public MySqlDbType[] ParaTypes;

                public PrepareCommandInfo(string name, string sql, MySqlDbType[] paraTypes)
                {
                    Name = name;
                    Sql = sql;
                    ParaTypes = paraTypes;
                }
            }

            public DatabaseService owner;
            public int index;
            public string connectStr;
            public MySqlConnection conn;
            //public object locker;
            public Dictionary<string, MySqlCommand> prepareCommands = new Dictionary<string, MySqlCommand>();
            public Dictionary<string, PrepareCommandInfo> prepareCommandInfos = new Dictionary<string, PrepareCommandInfo>();

            public DatabaseLink(DatabaseService owner, int index)
            {
                this.owner = owner;
                this.index = index;
            }
            public void Start(string connectStr)
            {
                try
                {
                    this.connectStr = connectStr;
                    //locker = new object();
                    conn = new MySqlConnection(connectStr);
                    conn.Open();

                    MySqlCommand command = new MySqlCommand("set names utf8", conn);
                    command.ExecuteNonQuery();
                }
                catch (Exception)
                {
                }
            }

            public void Stop()
            {
                try
                {
                    conn.Close();
                }
                catch (Exception)
                {

                }
            }

            public void Restart()
            {
                Thread.Sleep(100);
                conn.Close();
                conn = new MySqlConnection(connectStr);
                conn.Open();

                MySqlCommand command = new MySqlCommand("set names utf8", conn);
                command.ExecuteNonQuery();

                prepareCommands.Clear();

                foreach (var info in prepareCommandInfos.Values)
                {
                    SetPrepareCommand(info);
                }
            }

            public void SetPrepareCommand(PrepareCommandInfo info)
            {
                MySqlCommand command = new MySqlCommand(info.Sql, conn);
                int i = 1;

                foreach (var type in info.ParaTypes)
                {
                    command.Parameters.Add("@para" + i, type);
                    i++;
                }

                command.Prepare();

                prepareCommands[info.Name] = command;
            }

            public void SetPrepareCommand(string name, string sql, params MySqlDbType[] paraTypes)
            {
                MySqlCommand command = new MySqlCommand(sql, conn);
                int i = 1;

                foreach (var type in paraTypes)
                {
                    command.Parameters.Add("@para" + i, type);
                    i++;
                }

                command.Prepare();

                prepareCommands[name] = command;
                prepareCommandInfos[name] = new PrepareCommandInfo(name, sql, paraTypes);
            }

            int RealExecuteNonQuery(string name, params object[] paras)
            {
                var command = prepareCommands[name];

                for (int i = 0; i < paras.Length; i++)
                {
                    command.Parameters[i].Value = paras[i];
                }

                Interlocked.Increment(ref owner.totalNoQuery);

                return command.ExecuteNonQuery();
            }

            public int ExecuteNonQueryDirect(string sql)
            {
                bool ping = conn.Ping();
                if (!ping)
                    Restart();

                MySqlCommand command = new MySqlCommand(sql, conn);
                return command.ExecuteNonQuery();
            }

            public int ExecuteNonQuery(string name, params object[] paras)
            {
                try
                {
                    bool ping = conn.Ping();
                    if (!ping)
                        Restart();
                    return RealExecuteNonQuery(name, paras);
                }
                catch (Exception ex)
                {
                    if (ex.Message.StartsWith("Duplicate entry"))
                        return 0;
                    LogUtil.Log(ex.Message);
                    LogUtil.Log(ex.StackTrace);
                    return -1;
                }
            }


            MySqlDataReader RealExecuteReader(string name, params object[] paras)
            {
                var command = prepareCommands[name];

                for (int i = 0; i < paras.Length; i++)
                {
                    command.Parameters[i].Value = paras[i];
                }

                Interlocked.Increment(ref owner.totalQuery);

                return command.ExecuteReader();
            }

            public MySqlDataReader ExecuteReaderDirect(string sql)
            {
                bool ping = conn.Ping();
                if (!ping)
                    Restart();

                MySqlCommand command = new MySqlCommand(sql, conn);
                return command.ExecuteReader();
            }

            public MySqlDataReader ExecuteReader(string name, params object[] paras)
            {
                try
                {
                    bool ping = conn.Ping();
                    if (!ping)
                        Restart();

                    return RealExecuteReader(name, paras);
                }
                catch (Exception ex)
                {
                    LogUtil.Log(ex.Message);
                    LogUtil.Log(ex.StackTrace);
                    return null;
                }
            }
        }

        public static string connectStr;
        DatabaseLink[] dbLinks;
        public long totalQuery = 0;
        public long totalNoQuery = 0;
        public void Start(string connectStr, int dbLinkCount = 10)
        {
            dbLinks = new DatabaseLink[dbLinkCount];
            DatabaseService.connectStr = connectStr;

            for (int i = 0; i < dbLinks.Length; i++)
            {
                var dbLink = new DatabaseLink(this, i);

                dbLinks[i] = dbLink;

                dbLink.Start(connectStr);
            }


            Console.WriteLine("启动数据库连接池（{0}个连接）", dbLinkCount);
        }

        public void Stop()
        {
            foreach (var dbLink in dbLinks)
            {
                dbLink.Stop();
            }
        }
        public void SetPrepareCommand(string name, string sql, params MySqlDbType[] paraTypes)
        {
            foreach (var dbLink in dbLinks)
            {
                dbLink.SetPrepareCommand(name, sql, paraTypes);
            }
        }

        public DatabaseLink GetLink(int idx)
        {
            return dbLinks[idx];
        }

        public int GetLinkCount()
        {
            return dbLinks.Length;
        }
    }

    public static class DatabaseExt
    {
        public static bool GetBoolSafe(this MySqlDataReader reader, int fieldIndex)
        {
            if (reader.IsDBNull(fieldIndex))
                return false;

            return reader.GetBoolean(fieldIndex);
        }
        public static bool GetBoolSafe(this MySqlDataReader reader, string field)
        {
            return GetBoolSafe(reader, reader.GetOrdinal(field));
        }
        public static short GetInt16Safe(this MySqlDataReader reader, int fieldIndex)
        {
            if (reader.IsDBNull(fieldIndex))
                return 0;

            return reader.GetInt16(fieldIndex);
        }
        public static short GetInt16Safe(this MySqlDataReader reader, string field)
        {
            return GetInt16Safe(reader, reader.GetOrdinal(field));
        }

        public static int GetInt32Safe(this MySqlDataReader reader, int fieldIndex)
        {
            if (reader.IsDBNull(fieldIndex))
                return 0;

            return reader.GetInt32(fieldIndex);
        }
        public static int GetInt32Safe(this MySqlDataReader reader, string field)
        {
            return GetInt32Safe(reader, reader.GetOrdinal(field));
        }
        public static long GetInt64Safe(this MySqlDataReader reader, int fieldIndex)
        {
            if (reader.IsDBNull(fieldIndex))
                return 0;

            return reader.GetInt64(fieldIndex);
        }
        public static long GetInt64Safe(this MySqlDataReader reader, string field)
        {
            return GetInt64Safe(reader, reader.GetOrdinal(field));
        }
        public static string GetStringSafe(this MySqlDataReader reader, int fieldIndex)
        {
            if (reader.IsDBNull(fieldIndex))
                return "";

            return reader.GetString(fieldIndex);
        }
        public static string GetStringSafe(this MySqlDataReader reader, string field)
        {
            return GetStringSafe(reader, reader.GetOrdinal(field));
        }

        public static DateTime GetDateTimeSafe(this MySqlDataReader reader, int fieldIndex)
        {
            if (reader.IsDBNull(fieldIndex))
                return DateTime.MinValue;

            return reader.GetDateTime(fieldIndex);
        }

        public static DateTime GetDateTimeSafe(this MySqlDataReader reader, string field)
        {
            return GetDateTimeSafe(reader, reader.GetOrdinal(field));
        }
    }
}
