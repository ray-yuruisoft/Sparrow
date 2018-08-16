using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MixLibrary;

namespace GameInterface
{
    public interface ITable
    {
        int GetTableId();
        string GetGameName();
        string GetDllFile();
        int GetGrade();
        JObject GetGradeData();
        int GetSeatCount();
        List<string> GetPlayers();
        List<string> GetRobots();
        bool Kick(string show_id);
        void NotifyIngame(string subcmd, JObject jObjSend);
        void NotifyIngame(string show_id, string subcmd, JObject jObjSend);
        DatabaseService.DatabaseLink GetDbLink();
        long GetStore();
        bool SetStore(long store);
        long GetJackpot();
        bool SetJackpot(long jackpot);
        int GetBigAwardCounter();
        bool SetBigAwardCounter(int counter);
        long GetProfit(string show_id);
        bool SetProfit(string show_id, long profit);
        long GetForce(string show_id);
        bool SetForce(string show_id, long force);
        JObject GetUserGameData(string show_id);
        bool SetUserGameData(string show_id, JObject jData);
        bool LogPlayGame(string action, string show_id, JObject jData);
        bool LogGame(string action, JObject jData);
        JArray QuerySQLite(string sql);
        void Print(string format, params object[] args);
        void SetUserLastTable(string show_id);
        void UnsetUserLastTable(string show_id);
    }
}
