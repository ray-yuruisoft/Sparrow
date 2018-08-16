using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace HallServer
{
    public class DBHelper
    {
        public void Start()
        {
            Program.dbSvc.SetPrepareCommand("add user", "insert into cp_user.user set account=@para1, login_pwd=@para2, reg_time=now()", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("update reg user", "update cp_user.user set show_id=@para1, nick=@para2, icon=@para3, reg_ip=@para4, reg_mac=@para5 where account=@para6", 
                MySqlDbType.String,
                MySqlDbType.String, 
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("check user", "select show_id,nick,locked from cp_user.user where account=@para1 and login_pwd=@para2 limit 1", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("update login user", "update cp_user.user set login_token=@para1, login_time=now(), login_ip=@para2, login_mac=@para3, login_ismobile=@para4 where account=@para5",
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.Byte,
                MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("record account", "insert into cp_record.account (create_time, action, show_id, ip, mac) " + 
                "values (@para1, @para2, @para3, @para4, @para5)",
                MySqlDbType.DateTime,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("get user base info", "select * from cp_user.user where show_id=@para1 limit 1", MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("set user nick", "update cp_user.user set nick=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user icon", "update cp_user.user set icon=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user sign", "update cp_user.user set sign=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user login pwd", "update cp_user.user set login_pwd=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user bank pwd", "update cp_user.user set bank_pwd=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user money", "update cp_user.user set money=@para1 where show_id=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user bank money", "update cp_user.user set bank_money=@para1 where show_id=@para2", MySqlDbType.Int64, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user phone", "update cp_user.user set phone=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user vc", "update cp_user.user set vc=@para1 where show_id=@para2", MySqlDbType.String, MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("set user getvc time", "update cp_user.user set getvc_time=@para1 where show_id=@para2", MySqlDbType.DateTime, MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("record gift", "insert into cp_record.gift (sender_id, sender_nick, " +
                "receiver_id, receiver_nick, send_money, create_time) values (@para1, @para2, @para3, @para4, @para5, @para6)",
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.Int64,
                MySqlDbType.DateTime);

            Program.dbSvc.SetPrepareCommand("get send gift record", "select * from cp_record.gift where sender_id=@para1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get recv gift record", "select * from cp_record.gift where receiver_id=@para1", MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("record bank", "insert into cp_record.bank (create_time, show_id, nick, operate_type, money, money_before, money_last)" +
                " values (@para1, @para2, @para3, @para4, @para5, @para6, @para7)",
                MySqlDbType.DateTime,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.Int64,
                MySqlDbType.Int64,
                MySqlDbType.Int64);

            Program.dbSvc.SetPrepareCommand("get bank record", "select * from cp_record.bank where show_id=@para1", MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("stat rank1", "call cp_user.StatRank1");
            Program.dbSvc.SetPrepareCommand("stat rank2", "call cp_user.StatRank2");
            Program.dbSvc.SetPrepareCommand("get all rank1", "select * from cp_record.rank1 order by rank_num limit 20");
            Program.dbSvc.SetPrepareCommand("get all rank2", "select * from cp_record.rank2 order by rank_num limit 20");
            Program.dbSvc.SetPrepareCommand("get rank1", "select * from cp_record.rank1 where show_id=@para1 limit 1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get rank2", "select * from cp_record.rank2 where show_id=@para1 limit 1", MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("add mail", "insert into cp_user.mail (create_time, show_id, content)" +
                " values (@para1, @para2, @para3)",
                MySqlDbType.DateTime,
                MySqlDbType.String,
                MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("get user mail", "select * from cp_user.mail where show_id=@para1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("del all mail", "delete from cp_user.mail where show_id=@para1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("del one mail", "delete from cp_user.mail where ID=@para1", MySqlDbType.Int32);

            Program.dbSvc.SetPrepareCommand("get game list", "select * from cp_user.game where kind=@para1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get game server", "select ip_port from cp_user.game_server where game_name=@para1", MySqlDbType.String);
            Program.dbSvc.SetPrepareCommand("get notice", "select * from cp_user.notice where type=@para1 order by create_time desc limit 5", MySqlDbType.String);

            Program.dbSvc.SetPrepareCommand("record trade", "insert into cp_record.trade (create_time, number, show_id, item_id, type, money, status)" +
                " values (@para1, @para2, @para3, @para4, @para5, @para6, @para7)",
                MySqlDbType.DateTime,
                MySqlDbType.String,
                MySqlDbType.String,
                MySqlDbType.Int32,
                MySqlDbType.String,
                MySqlDbType.Int32,
                MySqlDbType.Int32);
            Program.dbSvc.SetPrepareCommand("get userbaseinfo by account", "select *from cp_user.user where account = @para1",
                MySqlDbType.String
                );
            Program.dbSvc.SetPrepareCommand("get phoneNum", "select count(phone) as num from cp_user.user where phone = @para1",
                MySqlDbType.String
                );
        }
        public void Stop()
        {

        }

        public int GetPhoneCount(DatabaseService.DatabaseLink dbLink, string phone)
        {
            using (var reader = dbLink.ExecuteReader("get phoneNum", phone))
            {
                if (reader == null || !reader.Read())
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(reader["num"]);
                }
            }
        }

        public MySqlDataReader GetUserInfoByAccount(DatabaseService.DatabaseLink dbLink, string account)
        {
            return dbLink.ExecuteReader("get userbaseinfo by account", account);
        }

        public bool AddUser(DatabaseService.DatabaseLink dbLink, string account, string login_pwd)
        {
            return dbLink.ExecuteNonQuery("add user", account, login_pwd) > 0;
        }

        public bool UpdateRegUser(DatabaseService.DatabaseLink dbLink, string show_id, string nick, string icon, string reg_ip, string reg_mac, string account)
        {
            return dbLink.ExecuteNonQuery("update reg user", show_id, nick, icon, reg_ip, reg_mac, account) > 0;
        }

        public MySqlDataReader CheckUser(DatabaseService.DatabaseLink dbLink, string account, string login_pwd)
        {
            return dbLink.ExecuteReader("check user", account, login_pwd);
        }

        public bool UpdateLoginUser(DatabaseService.DatabaseLink dbLink, string login_token, string login_ip, string login_mac, int login_ismobile, string account)
        {
            return dbLink.ExecuteNonQuery("update login user", login_token, login_ip, login_mac, login_ismobile, account) > 0;
        }
        public MySqlDataReader GetUserBaseInfo(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            return dbLink.ExecuteReader("get user base info", show_id);
        }

        public bool AddMail(DatabaseService.DatabaseLink dbLink, string show_id, string content)
        {
            return dbLink.ExecuteNonQuery("add mail", DateTime.Now, show_id, content) > 0;
        }

        public MySqlDataReader GetUserMail(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            return dbLink.ExecuteReader("get user mail", show_id);
        }

        public bool DelAllMail(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            return dbLink.ExecuteNonQuery("del all mail", show_id) >= 0;
        }

        public bool DelOneMail(DatabaseService.DatabaseLink dbLink, int mailID)
        {
            return dbLink.ExecuteNonQuery("del one mail", mailID) >= 0;
        }

        public string GetUserAccount(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("account");
            }
        }

        public string GetUserLoginPwd(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("login_pwd");
            }
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

        public string GetUserLoginIP(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("login_ip");
            }
        }

        public string GetUserLoginMac(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("login_mac");
            }
        }

        public DateTime GetUserLoginTime(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return DateTime.MinValue;
                }

                return reader.GetDateTimeSafe("login_time");
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

        public string GetUserIcon(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("icon");
            }
        }

        public string GetUserSign(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("sign");
            }
        }

        public string GetUserBankPwd(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("bank_pwd");
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

        public long GetUserBankMoney(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return 0;
                }

                return reader.GetInt64Safe("bank_money");
            }
        }

        public string GetUserPhone(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("phone");
            }
        }

        public string GetUserVc(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return "";
                }

                return reader.GetStringSafe("vc");
            }
        }

        public DateTime GetUserGetVcTime(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            using (var reader = GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    return DateTime.MinValue;
                }

                return reader.GetDateTimeSafe("getvc_time");
            }
        }

        public bool SetUserNick(DatabaseService.DatabaseLink dbLink, string nick, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user nick", nick, show_id) > 0;
        }

        public bool SetUserIcon(DatabaseService.DatabaseLink dbLink, string icon, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user icon", icon, show_id) > 0;
        }

        public bool SetUserSign(DatabaseService.DatabaseLink dbLink, string sign, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user sign", sign, show_id) > 0;
        }

        public bool SetUserLoginPwd(DatabaseService.DatabaseLink dbLink, string pwd, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user login pwd", pwd, show_id) > 0;
        }

        public bool SetUserBankPwd(DatabaseService.DatabaseLink dbLink, string pwd, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user bank pwd", pwd, show_id) > 0;
        }

        public bool SetUserMoney(DatabaseService.DatabaseLink dbLink, long money, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user money", money, show_id) > 0;
        }

        public bool SetUserBankMoney(DatabaseService.DatabaseLink dbLink, long bank_money, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user bank money", bank_money, show_id) > 0;
        }

        public bool SetUserPhone(DatabaseService.DatabaseLink dbLink, string phone, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user phone", phone, show_id) > 0;
        }

        public bool SetUserVc(DatabaseService.DatabaseLink dbLink, string vc, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user vc", vc, show_id) > 0;
        }

        public bool SetUserGetVcTime(DatabaseService.DatabaseLink dbLink, DateTime getVcTime, string show_id)
        {
            return dbLink.ExecuteNonQuery("set user getvc time", getVcTime, show_id) > 0;
        }

        public MySqlDataReader GetRecordGift(DatabaseService.DatabaseLink dbLink, string show_id, string type)
        {
            if(type == "1")
                return dbLink.ExecuteReader("get send gift record", show_id);
            if(type == "2")
                return dbLink.ExecuteReader("get recv gift record", show_id);

            return null;
        }

        public MySqlDataReader GetRecordBank(DatabaseService.DatabaseLink dbLink, string show_id)
        {
            return dbLink.ExecuteReader("get bank record", show_id);
        }

        public MySqlDataReader GetRecordRank(DatabaseService.DatabaseLink dbLink, string type, string show_id = "")
        {
            if(show_id.Length == 0)
                return dbLink.ExecuteReader("get all rank" + type);

            return dbLink.ExecuteReader("get rank" + type, show_id);
        }

        public void StatRecordRank(DatabaseService.DatabaseLink dbLink, string type)
        {
            dbLink.ExecuteNonQuery("stat rank" + type);
        }

        public MySqlDataReader GetGameList(DatabaseService.DatabaseLink dbLink, string kind)
        {
            return dbLink.ExecuteReader("get game list", kind);
        }

        public MySqlDataReader GetGameServer(DatabaseService.DatabaseLink dbLink, string game_name)
        {
            return dbLink.ExecuteReader("get game server", game_name);
        }

        public MySqlDataReader GetGetNotice(DatabaseService.DatabaseLink dbLink, string type)
        {
            return dbLink.ExecuteReader("get notice", type);
        }

        public bool RecordAccount(DatabaseService.DatabaseLink dbLink, string action, string show_id, string ip, string mac)
        {
            return dbLink.ExecuteNonQuery("record account", DateTime.Now, action, show_id, ip, mac) > 0;
        }
        public bool RecordTrade(DatabaseService.DatabaseLink dbLink, string number, string show_id, int item_id, string type, int money)
        {
            return dbLink.ExecuteNonQuery("record trade", DateTime.Now, number, show_id, item_id, type, money, 0) > 0;
        }
    }
}
