using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using MixLibrary;

namespace GameInterface
{
    public interface IDBHelper
    {
        bool GetLock(DatabaseService.DatabaseLink dbLink, string key);
        bool ReleaseLock(DatabaseService.DatabaseLink dbLink, string key);
        MySqlDataReader GetUserBaseInfo(DatabaseService.DatabaseLink dbLink, string show_id);
        string GetUserNick(DatabaseService.DatabaseLink dbLink, string show_id);
        long GetUserMoney(DatabaseService.DatabaseLink dbLink, string show_id);
        bool SetUserMoney(DatabaseService.DatabaseLink dbLink, long money, string show_id);
        bool UserIsRobot(DatabaseService.DatabaseLink dbLink, string show_id);
        bool AddNotice(DatabaseService.DatabaseLink dbLink, int type, string title, string content);
    }
}
