using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MixLibrary
{
    public class IniFile
    {
        private string m_FileName;

        public string FileName
        {
            get { return m_FileName; }
            set { m_FileName = value; }
        }

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileInt(
            string lpAppName,
            string lpKeyName,
            int nDefault,
            string lpFileName
            );

        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            int nSize,
            string lpFileName
            );

        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpString,
            string lpFileName
            );

        public IniFile(string aFileName)
        {
            this.m_FileName = aFileName;
        }

        public int ReadInt(string section, string name, int def)
        {
            int ret = def;

            if(!int.TryParse(ReadString(section, name, "0").Trim(), out ret))
            {
                return def;
            }

            return ret;
        }

        public long ReadLong(string section, string name, long def)
        {
            long ret = def;

            if (!long.TryParse(ReadString(section, name, "0").Trim(), out ret))
            {
                return def;
            }

            return ret;
        }

        public double ReadDouble(string section, string name, double def)
        {
            double ret = def;

            if (!double.TryParse(ReadString(section, name, "0").Trim(), out ret))
            {
                return def;
            }

            return ret;
        }

        public string ReadString(string section, string name, string def)
        {
            StringBuilder vRetSb = new StringBuilder(2048);
            GetPrivateProfileString(section, name, def, vRetSb, 2048, this.m_FileName);
            return vRetSb.ToString();
        }
    }
}
