using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Room
    {
        public List<Table> tables = new List<Table>();
        public GameModule.GameInfo gameInfo;
        public int grade;
        public Room(GameModule.GameInfo gameInfo, int grade, Dictionary<int, Table> idToTableDict, bool isNeedUpdate)
        {
            this.gameInfo = gameInfo;
            this.grade = grade;

            for (int i = 0; i < gameInfo.tableCount; i++)
            {
                Table table = new Table(this);

                tables.Add(table);
                idToTableDict[table.id] = table;

                Worker worker = Program.workerMgr.AllotWorker(table.id, false);

                table.dbLink = Program.dbSvc.GetLink(worker.index);

                if (isNeedUpdate)
                    worker.AddUpdateTable(table);
            }
        }
        public Table AllotTable()
        {
            if (tables.Count == 1)
                return tables[0];

            for(int i = 1; i <= gameInfo.seatCount; i++)
            {
                foreach (var table in tables)
                {
                    if (table.RemainSeatCount == i)
                    {
                        return table;
                    }
                }
            }
            
            return null;
        }
    }
}
