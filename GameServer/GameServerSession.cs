using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MixLibrary;

namespace GameServer
{
    public class GameServerSession : TcpSession
    {
        public int TableId
        {
            get
            {
                Thread.MemoryBarrier();
                return table_id;
            }
            set
            {
                int compare;

                do
                {
                    compare = table_id;
                } while (Interlocked.CompareExchange(ref table_id, value, compare) != compare);
            }
        }

        int table_id = 0;

    }
}
