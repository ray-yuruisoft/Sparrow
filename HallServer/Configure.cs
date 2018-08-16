using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HallServer
{
    public class Configure
    {
        public static Configure Inst = new Configure();

        public int serverPort;
        public int heartPeriod;
        public string dbConnectStr;
        public int workerCount;
        public bool isShowStat = false;
        public Dictionary<string, string> payCallbackUrls = new Dictionary<string, string>();
        public JObject jClientConfig;
        public bool Load()
        {
            try
            {
                XmlDocument xmlCfg = new XmlDocument();

                xmlCfg.Load(AppDomain.CurrentDomain.BaseDirectory + "Config.xml");

                XmlNode xmlNode = xmlCfg.SelectSingleNode("Root/Server");

                if (xmlNode == null)
                    return false;

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

                xmlNode = xmlCfg.SelectSingleNode("Root/Pay");

                if (xmlNode == null)
                    return false;

                foreach(XmlNode childNode in xmlNode.ChildNodes)
                {
                    string name = childNode.Attributes["名称"].Value;
                    string url = childNode.Attributes["回调链接"].Value;

                    payCallbackUrls.Add(name, url);
                }

                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "client_config.json"))
                {
                    jClientConfig = JObject.Parse(sr.ReadToEnd());
                }

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex.Message);
                LogUtil.Log(ex.StackTrace);

                return false;
            }
        }

        public string GetPayCallbackUrl(string name)
        {
            return payCallbackUrls[name];
        }
    }

}
