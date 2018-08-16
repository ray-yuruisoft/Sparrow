using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixLibrary
{
    public class WheelTimerEventSlot
    {
        ConcurrentDictionary<WheelTimerEvent, WheelTimerEvent> events = new ConcurrentDictionary<WheelTimerEvent, WheelTimerEvent>();
        public bool Set(WheelTimerEvent timerEvent)
        {
            return events.TryAdd(timerEvent, timerEvent);
        }

        public bool Remove(WheelTimerEvent timerEvent)
        {
            WheelTimerEvent dummy = null;

            return events.TryRemove(timerEvent, out dummy);
        }

        public void Clear()
        {
            events.Clear();
        }

        public void Perform(Action<WheelTimerEvent> onTimer)
        {
            foreach (var te in events.Values)
            {
                onTimer(te);
            }
        }
    }
}
