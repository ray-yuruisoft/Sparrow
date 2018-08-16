using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MixLibrary
{
    public class WheelTimerEvent
    {
        public int OwnerSlotIndex
        {
            get
            {
                Thread.MemoryBarrier();
                return ownerSlotIndex;
            }
            set
            {
                int compare;

                do
                {
                    compare = ownerSlotIndex;
                } while (Interlocked.CompareExchange(ref ownerSlotIndex, value, compare) != compare);
            }
        }
        int ownerSlotIndex;
    }
}
