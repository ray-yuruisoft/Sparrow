using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameInterface
{
    public interface IGameLogic
    {
        //初始化
        void Init(ITable table, IDBHelper dbHelper);
        //应用配置
        void ApplyNewConfigs(JObject jConfigs, int grade);
        //更新
        void Update();
        //操作
        void OperateIngame(string player, string subcmd, JObject jObjRecv, JObject jObjSend);
        //玩家进入
        void OnAddPlayer(string player);
        //玩家离开
        void OnRemovePlayer(string player, bool isCauseDisconnect);
    }
}
