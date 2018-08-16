using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixLibrary
{
    public static class RandomUtil
    {
        
        public static int RandomMinMax(Random rd, int min, int max)
        {
            if (min > max)
            {
                int copy = min;
                min = max;
                max = copy;
            }
            else if (min == max)
            {
                return min;
            }

            return min + rd.Next((max - min) + 1);
        }

        public static int ProbChoose(Random rd, int[] probs)
        {
            int total = 0;

            for (int i = 0; i < probs.Length; i++)
            {
                total += probs[i];
            }

            int r = rd.Next(0, total);

            for (int i = 0; i < probs.Length; i++)
            {
                if (r < probs[i])
                {
                    return i;
                }
                else
                {
                    r -= probs[i];
                }
            }

            return probs.Length - 1;
        }

        public static string RandChars(Random rd, int count, string chars = "0123456789")
        {
            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < count; i++)
            {
                int idx = rd.Next(chars.Length);
                sb.Append(chars[idx]);
            }
            
            return sb.ToString();
        }
    }
}
