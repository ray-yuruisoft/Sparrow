using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using MySql.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MixLibrary;

namespace RobotTool
{
    public class RobotMgr
    {
        public static RobotMgr Inst = new RobotMgr();

        public string gameServerIp;
        public int gameServerPort;
        public double sendHeartInterval;
        public string gameName;
        public int grade;
        public string dbConnectStr;
        public MySqlConnection conn;

        public List<Robot> robots = new List<Robot>();
        public Random rand = new Random();
        //机器人携带金钱区间
        public long minMoney;
        public long maxMoney;
        public int addMoneyMin;
        public int addMoneyMax;
        public bool Start()
        {
            try
            {
                XmlDocument xmlCfg = new XmlDocument();

                xmlCfg.Load(AppDomain.CurrentDomain.BaseDirectory + "Config.xml");

                XmlNode xmlNode = xmlCfg.SelectSingleNode("Root/Target");

                if (xmlNode == null)
                    return false;

                gameServerIp = xmlNode.Attributes["游戏服IP"].Value;
                gameServerPort = int.Parse(xmlNode.Attributes["游戏服端口"].Value);
                sendHeartInterval = double.Parse(xmlNode.Attributes["心跳发送时间"].Value);
                gameName = xmlNode.Attributes["目标游戏"].Value;
                grade = int.Parse(xmlNode.Attributes["游戏级别"].Value);

                xmlNode = xmlCfg.SelectSingleNode("Root/DB");

                if (xmlNode == null)
                    return false;

                dbConnectStr = xmlNode.Attributes["DB参数"].Value;

                conn = new MySqlConnection(dbConnectStr);
                conn.Open();

                MySqlCommand command = new MySqlCommand("set names utf8", conn);
                command.ExecuteNonQuery();

                xmlNode = xmlCfg.SelectSingleNode("Root/Setting");

                if (xmlNode == null)
                    return false;

                string[] strs = xmlNode.Attributes["金额区间"].Value.Split(',');
                minMoney = long.Parse(strs[0]);
                maxMoney = long.Parse(strs[1]);
                strs = xmlNode.Attributes["补充金额"].Value.Split(',');
                addMoneyMin = int.Parse(strs[0]);
                addMoneyMax = int.Parse(strs[1]);

                xmlNode = xmlCfg.SelectSingleNode("Root/Robots");

                if (xmlNode == null)
                    return false;

                for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
                {
                    var childNode = xmlNode.ChildNodes[i];

                    Robot robot = new Robot(this);

                    robot.account = childNode.Attributes["账号"].Value;

                    #region 火拼牛牛特有配置
                    string temp = childNode.Attributes["叫庄概率"].Value;
                    if (temp != "")
                    {
                        robot.callBankerProb = int.Parse(temp);
                    }
                    temp = childNode.Attributes["押注权重"].Value;
                    if (temp != "")
                    {
                        var tempArr = temp.Split(',');
                        robot.betterBetProbs = new int[tempArr.Length];
                        for (int j = 0; j < tempArr.Length; j++)
                        {
                            robot.betterBetProbs[j] = int.Parse(tempArr[j]);
                        }
                    }
                    #endregion

                    var reader = GetRobotInfoInDB(robot);

                    if (reader != null)
                    {
                        robot.show_id = reader.GetStringSafe("show_id");
                        robot.login_token = reader.GetStringSafe("login_token");
                        robot.login_ip = reader.GetStringSafe("login_ip");
                        robot.nick = reader.GetStringSafe("nick");
                        robot.icon = reader.GetStringSafe("icon");
                        robot.sign = reader.GetStringSafe("sign");
                        robot.money = reader.GetInt64Safe("money");

                        reader.Close();
                    }
                    else
                    {
                        robot.login_token = EncipherUtil.Md5(rand.Next().ToString());
                        robot.login_ip = childNode.Attributes["登录IP"].Value;
                        robot.nick = childNode.Attributes["昵称"].Value;
                        robot.icon = childNode.Attributes["头像"].Value;
                        robot.sign = childNode.Attributes["签名"].Value;
                        robot.money = long.Parse(childNode.Attributes["金额"].Value);
                      
                        do
                        {
                            robot.show_id = RandomUtil.RandChars(rand, 1, "123456789") + RandomUtil.RandChars(rand, 7);

                        } while (!WriteRobotInfoToDB(robot));
                    }

                    robots.Add(robot);
                }

                Thread thread = new Thread(ThreadLogicProc);
                thread.IsBackground = true;
                thread.Start();

                return true;
            }
            catch (Exception ex)
            {
                LogUtil.Log(ex.Message);
                LogUtil.Log(ex.StackTrace);

                return false;
            }
        }

        MySqlDataReader GetRobotInfoInDB(Robot robot)
        {
            try
            {
                string sql = string.Format("select * from cp_user.user where account='{0}' and is_robot=1", robot.account);
                MySqlCommand command = new MySqlCommand(sql, conn);

                MySqlDataReader reader = command.ExecuteReader();

                if (reader == null)
                    return null;

                if (!reader.Read())
                {
                    reader.Close();
                    return null;
                }

                return reader;
            }
            catch (Exception)
            {
                return null;
            }
        }

        bool WriteRobotInfoToDB(Robot robot)
        {
            try
            {
                string sql = string.Format("insert into cp_user.user (show_id,account,login_token,login_ip,nick,icon,sign,money,is_robot) " +
                "values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','1') ",
                robot.show_id, robot.account, robot.login_token, robot.login_ip, robot.nick, robot.icon, robot.sign, robot.money);
                MySqlCommand command = new MySqlCommand(sql, conn);

                return command.ExecuteNonQuery() == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        void ThreadLogicProc()
        {
            while (true)
            {
                foreach (var robot in robots)
                {
                    robot.Update();

                    Thread.Sleep(0);
                }

                Thread.Sleep(10);
            }
        }

        public void Stop()
        {

        }
    }
}
