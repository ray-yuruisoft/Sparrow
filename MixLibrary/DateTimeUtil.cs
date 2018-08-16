using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixLibrary
{
    public class DateTimeUtil
    {
        public static string format = "yyyy-MM-dd HH:mm:ss";

        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (long)ts.TotalMilliseconds;
        }
    }
}
