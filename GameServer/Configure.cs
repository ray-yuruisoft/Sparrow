using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MixLibrary;

namespace GameServer
{
    public class Configure
    {
        public static Configure Inst = new Configure();

        public string outerIp;
        public int serverPort;
        public int heartPeriod;
        public string dbConnectStr;
        public int workerCount;
        public bool isShowStat;
        public string supportGames;
        public bool Load()
        {
            try
            {
                XmlDocument xmlCfg = new XmlDocument();

                xmlCfg.Load(AppDomain.CurrentDomain.BaseDirectory + "Config.xml");

                XmlNode xmlNode = xmlCfg.SelectSingleNode("Root/Server");

                if (xmlNode == null)
                    return false;

                outerIp = xmlNode.Attributes["对外IP"].Value;
                serverPort = int.Parse(xmlNode.Attributes["端口"].Value);
                heartPeriod = int.Parse(xmlNode.Attributes["心跳检测时间"].Value);

                xmlNode = xmlCfg.SelectSingleNode("Root/DB");

                if (xmlNode == null)
                    return false;

                dbConnectStr = xmlNode.Attributes["DB参数"].Value;
                
                xmlNode = xmlCfg.SelectSingleNode("Root/Worker");

                if (xmlNode == null)
                    return false;

                workerCount = int.Parse(xmlNode.Attributes["数量"].Value);

                xmlNode = xmlCfg.SelectSingleNode("Root/Other");

                if (xmlNode == null)
                    return false;

                supportGames = xmlNode.Attributes["SupportGames"].Value;

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex.Message);
                LogUtil.Log(ex.StackTrace);

                return false;
            }
        }
    }

}
