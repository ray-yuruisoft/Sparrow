using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixLibrary
{
    public class NormalTimer : BaseTimer
    {
        bool isTriggerAtTime;
        DateTime triggerTime;   //触发时间
        TimeSpan timeSpan;
        Action onTimer;
        DayOfWeek dayOfWeek;
        int hour, minute, second;

        public NormalTimer(int period, int periodUnit, Action onTimer)
        {
            isTriggerAtTime = false;
            this.onTimer = onTimer;

            timeSpan = TimeSpan.Zero;

            switch (periodUnit)
            {
                case 0:
                    timeSpan = TimeSpan.FromSeconds(period);
                    break;
                case 1:
                    timeSpan = TimeSpan.FromMinutes(period);
                    break;
                case 2:
                    timeSpan = TimeSpan.FromHours(period);
                    break;
                case 3:
                    timeSpan = TimeSpan.FromDays(period);
                    break;
            }

            UpdateTriggerTime();
        }

        public NormalTimer(DayOfWeek dayOfWeek, int hour, int minute, int second, Action onTimer)
        {
            isTriggerAtTime = true;
            this.onTimer = onTimer;

            this.dayOfWeek = dayOfWeek;
            this.hour = hour;
            this.minute = minute;
            this.second = second;
        }

        void UpdateTriggerTime()
        {
            if(isTriggerAtTime)
            {
                var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, second);
                //计算离目标星期几相差多少天
                int days = (int)dayOfWeek - (int)today.DayOfWeek;

                if (days < 0)
                    days += 7;

                triggerTime = today + TimeSpan.FromDays(days);
            }
            else
            {
                triggerTime = DateTime.Now + timeSpan;
            }
            
        }

        public override void Run()
        {
            if(DateTime.Now > triggerTime)
            {
                UpdateTriggerTime();

                onTimer();
            }
        }
    }
}
