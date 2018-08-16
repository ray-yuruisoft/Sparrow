using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixLibrary
{
    public class WheelTimer : BaseTimer
    {
        int slotsLen;
        WheelTimerEventSlot[] slots;
        Action<WheelTimerEvent> onTimer;
        public int currSlotIndex = 0;
        bool isLoop;
        public WheelTimer(int maxPeriod, Action<WheelTimerEvent> onTimer, bool isLoop = false)
        {
            slots = new WheelTimerEventSlot[maxPeriod * 2];
            slotsLen = slots.Length;
            for(int i = 0; i < slotsLen; i++)
            {
                slots[i] = new WheelTimerEventSlot();
            }

            this.onTimer = onTimer;
            this.isLoop = isLoop;
        }

        public override void Run()
        {
            currSlotIndex++;
            if (currSlotIndex >= slotsLen)
                currSlotIndex = 0;

            var slot = slots[currSlotIndex];

            slot.Perform(onTimer);
            if(!isLoop)
                slot.Clear();
        }

        public void Add(WheelTimerEvent te, int period)
        {
            int addToSlotIndex = currSlotIndex + period;
            if (addToSlotIndex >= slotsLen)
                addToSlotIndex -= slotsLen;

            var addToSlot = slots[addToSlotIndex];
            if (addToSlot.Set(te))
            {
                te.OwnerSlotIndex = addToSlotIndex;
            }
                
        }

        public bool Remove(WheelTimerEvent te)
        {
            var ownerSlot = slots[te.OwnerSlotIndex];

            return ownerSlot.Remove(te);
        }

        public void Active(WheelTimerEvent te, int period)
        {
            int addToSlotIndex = currSlotIndex + period;
            if (addToSlotIndex >= slotsLen)
                addToSlotIndex -= slotsLen;

            if (te.OwnerSlotIndex == addToSlotIndex)
                return;

            if (Remove(te))
                Add(te, period);
        }
    }
}
