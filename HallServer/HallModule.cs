using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.IO;
using System.Xml;
using System.Threading;

namespace HallServer
{
    public class HallModule
    {
        public class ImageVC
        {
            public string text;
            public byte[] imageData;
        }

        public class PayParam
        {
            public string name;
            public string value;

            public PayParam(string name, string value)
            {
                this.name = name;
                this.value = value;
            }
        }

        NormalTimer rank1StatTimer;
        NormalTimer rank2StatTimer;
        Random rand = new Random();
        ImageVC[] imageVCs;
        const double VertifyCodeExpiresSeconds = 120; //验证码有效期设为60秒
        static string[] nick1 = new string[] { "快乐的", "冷静的", "醉熏的", "潇洒的", "糊涂的", "积极的", "冷酷的", "深情的", "粗暴的", "温柔的", "可爱的", "愉快的", "义气的", "认真的", "威武的", "帅气的", "传统的", "潇洒的", "漂亮的", "自然的", "专一的", "听话的", "昏睡的", "狂野的", "等待的", "搞怪的", "幽默的", "魁梧的", "活泼的", "开心的", "高兴的", "超帅的", "留胡子的", "坦率的", "直率的", "轻松的", "痴情的", "完美的", "精明的", "无聊的", "有魅力的", "丰富的", "繁荣的", "饱满的", "炙热的", "暴躁的", "碧蓝的", "俊逸的", "英勇的", "健忘的", "故意的", "无心的", "土豪的", "朴实的", "兴奋的", "幸福的", "淡定的", "不安的", "阔达的", "孤独的", "独特的", "疯狂的", "时尚的", "落后的", "风趣的", "忧伤的", "大胆的", "爱笑的", "矮小的", "健康的", "合适的", "玩命的", "沉默的", "斯文的", "香蕉", "苹果", "鲤鱼", "鳗鱼", "任性的", "细心的", "粗心的", "大意的", "甜甜的", "酷酷的", "健壮的", "英俊的", "霸气的", "阳光的", "默默的", "大力的", "孝顺的", "忧虑的", "着急的", "紧张的", "善良的", "凶狠的", "害怕的", "重要的", "危机的", "欢喜的", "欣慰的", "满意的", "跳跃的", "诚心的", "称心的", "如意的", "怡然的", "娇气的", "无奈的", "无语的", "激动的", "愤怒的", "美好的", "感动的", "激情的", "激昂的", "震动的", "虚拟的", "超级的", "寒冷的", "精明的", "明理的", "犹豫的", "忧郁的", "寂寞的", "奋斗的", "勤奋的", "现代的", "过时的", "稳重的", "热情的", "含蓄的", "开放的", "无辜的", "多情的", "纯真的", "拉长的", "热心的", "从容的", "体贴的", "风中的", "曾经的", "追寻的", "儒雅的", "优雅的", "开朗的", "外向的", "内向的", "清爽的", "文艺的", "长情的", "平常的", "单身的", "伶俐的", "高大的", "懦弱的", "柔弱的", "爱笑的", "乐观的", "耍酷的", "酷炫的", "神勇的", "年轻的", "唠叨的", "瘦瘦的", "无情的", "包容的", "顺心的", "畅快的", "舒适的", "靓丽的", "负责的", "背后的", "简单的", "谦让的", "彩色的", "缥缈的", "欢呼的", "生动的", "复杂的", "慈祥的", "仁爱的", "魔幻的", "虚幻的", "淡然的", "受伤的", "雪白的", "高高的", "糟糕的", "顺利的", "闪闪的", "羞涩的", "缓慢的", "迅速的", "优秀的", "聪明的", "含糊的", "俏皮的", "淡淡的", "坚强的", "平淡的", "欣喜的", "能干的", "灵巧的", "友好的", "机智的", "机灵的", "正直的", "谨慎的", "俭朴的", "殷勤的", "虚心的", "辛勤的", "自觉的", "无私的", "无限的", "踏实的", "老实的", "现实的", "可靠的", "务实的", "拼搏的", "个性的", "粗犷的", "活力的", "成就的", "勤劳的", "单纯的", "落寞的", "朴素的", "悲凉的", "忧心的", "洁净的", "清秀的", "自由的", "小巧的", "单薄的", "贪玩的", "刻苦的", "干净的", "壮观的", "和谐的", "文静的", "调皮的", "害羞的", "安详的", "自信的", "端庄的", "坚定的", "美满的", "舒心的", "温暖的", "专注的", "勤恳的", "美丽的", "腼腆的", "优美的", "甜美的", "甜蜜的", "整齐的", "动人的", "典雅的", "尊敬的", "舒服的", "妩媚的", "秀丽的", "喜悦的", "甜美的", "彪壮的", "强健的", "大方的", "俊秀的", "聪慧的", "迷人的", "陶醉的", "悦耳的", "动听的", "明亮的", "结实的", "魁梧的", "标致的", "清脆的", "敏感的", "光亮的", "大气的", "老迟到的", "知性的", "冷傲的", "呆萌的", "野性的", "隐形的", "笑点低的", "微笑的", "笨笨的", "难过的", "沉静的", "火星上的", "失眠的", "安静的", "纯情的", "要减肥的", "迷路的", "烂漫的", "哭泣的", "贤惠的", "苗条的", "温婉的", "发嗲的", "会撒娇的", "贪玩的", "执着的", "眯眯眼的", "花痴的", "想人陪的", "眼睛大的", "高贵的", "傲娇的", "心灵美的", "爱撒娇的", "细腻的", "天真的", "怕黑的", "感性的", "飘逸的", "怕孤独的", "忐忑的", "高挑的", "傻傻的", "冷艳的", "爱听歌的", "还单身的", "怕孤单的", "懵懂的" };
        static string[] nick2 = new string[] { "嚓茶", "凉面", "便当", "毛豆", "花生", "可乐", "灯泡", "哈密瓜", "野狼", "背包", "眼神", "缘分", "雪碧", "人生", "牛排", "蚂蚁", "飞鸟", "灰狼", "斑马", "汉堡", "悟空", "巨人", "绿茶", "自行车", "保温杯", "大碗", "墨镜", "魔镜", "煎饼", "月饼", "月亮", "星星", "芝麻", "啤酒", "玫瑰", "大叔", "小伙", "哈密瓜，数据线", "太阳", "树叶", "芹菜", "黄蜂", "蜜粉", "蜜蜂", "信封", "西装", "外套", "裙子", "大象", "猫咪", "母鸡", "路灯", "蓝天", "白云", "星月", "彩虹", "微笑", "摩托", "板栗", "高山", "大地", "大树", "电灯胆", "砖头", "楼房", "水池", "鸡翅", "蜻蜓", "红牛", "咖啡", "机器猫", "枕头", "大船", "诺言", "钢笔", "刺猬", "天空", "飞机", "大炮", "冬天", "洋葱", "春天", "夏天", "秋天", "冬日", "航空", "毛衣", "豌豆", "黑米", "玉米", "眼睛", "老鼠", "白羊", "帅哥", "美女", "季节", "鲜花", "服饰", "裙子", "白开水", "秀发", "大山", "火车", "汽车", "歌曲", "舞蹈", "老师", "导师", "方盒", "大米", "麦片", "水杯", "水壶", "手套", "鞋子", "自行车", "鼠标", "手机", "电脑", "书本", "奇迹", "身影", "香烟", "夕阳", "台灯", "宝贝", "未来", "皮带", "钥匙", "心锁", "故事", "花瓣", "滑板", "画笔", "画板", "学姐", "店员", "电源", "饼干", "宝马", "过客", "大白", "时光", "石头", "钻石", "河马", "犀牛", "西牛", "绿草", "抽屉", "柜子", "往事", "寒风", "路人", "橘子", "耳机", "鸵鸟", "朋友", "苗条", "铅笔", "钢笔", "硬币", "热狗", "大侠", "御姐", "萝莉", "毛巾", "期待", "盼望", "白昼", "黑夜", "大门", "黑裤", "钢铁侠", "哑铃", "板凳", "枫叶", "荷花", "乌龟", "仙人掌", "衬衫", "大神", "草丛", "早晨", "心情", "茉莉", "流沙", "蜗牛", "战斗机", "冥王星", "猎豹", "棒球", "篮球", "乐曲", "电话", "网络", "世界", "中心", "鱼", "鸡", "狗", "老虎", "鸭子", "雨", "羽毛", "翅膀", "外套", "火", "丝袜", "书包", "钢笔", "冷风", "八宝粥", "烤鸡", "大雁", "音响", "招牌", "胡萝卜", "冰棍", "帽子", "菠萝", "蛋挞", "香水", "泥猴桃", "吐司", "溪流", "黄豆", "樱桃", "小鸽子", "小蝴蝶", "爆米花", "花卷", "小鸭子", "小海豚", "日记本", "小熊猫", "小懒猪", "小懒虫", "荔枝", "镜子", "曲奇", "金针菇", "小松鼠", "小虾米", "酒窝", "紫菜", "金鱼", "柚子", "果汁", "百褶裙", "项链", "帆布鞋", "火龙果", "奇异果", "煎蛋", "唇彩", "小土豆", "高跟鞋", "戒指", "雪糕", "睫毛", "铃铛", "手链", "香氛", "红酒", "月光", "酸奶", "银耳汤", "咖啡豆", "小蜜蜂", "小蚂蚁", "蜡烛", "棉花糖", "向日葵", "水蜜桃", "小蝴蝶", "小刺猬", "小丸子", "指甲油", "康乃馨", "糖豆", "薯片", "口红", "超短裙", "乌冬面", "冰淇淋", "棒棒糖", "长颈鹿", "豆芽", "发箍", "发卡", "发夹", "发带", "铃铛", "小馒头", "小笼包", "小甜瓜", "冬瓜", "香菇", "小兔子", "含羞草", "短靴", "睫毛膏", "小蘑菇", "跳跳糖", "小白菜", "草莓", "柠檬", "月饼", "百合", "纸鹤", "小天鹅", "云朵", "芒果", "面包", "海燕", "小猫咪", "龙猫", "唇膏", "鞋垫", "羊", "黑猫", "白猫", "万宝路", "金毛", "山水", "音响" };
        public void Start()
        {
            LoadImageVC();
            SetupTimer();
            //登录
            Program.moduleManager.RegisterRequestHandler("login", OnReqLogin);
            //获取图片验证码
            Program.moduleManager.RegisterRequestHandler("get_image_vc", OnReqGetImageVC);
            //注册
            Program.moduleManager.RegisterRequestHandler("reg", OnReqReg);
            //进入大厅
            Program.moduleManager.RegisterRequestHandler("enter_hall", OnReqEnterHall);
            //修改个人信息
            Program.moduleManager.RegisterRequestHandler("modify_info", OnReqModifyInfo);
            //修改密码
            Program.moduleManager.RegisterRequestHandler("modify_pwd", OnReqModifyPwd);
            //请求短信验证码
            Program.moduleManager.RegisterRequestHandler("sms", OnReqSms);
            //重设密码 游戏内
            Program.moduleManager.RegisterRequestHandler("reset_pwd", OnReqResetPwd);
            //请求短信验证码 游戏外
            Program.moduleManager.RegisterRequestHandler("sms_Hall", OnReqSmsHall);
            //重设密码 游戏外
            Program.moduleManager.RegisterRequestHandler("reset_pwd_Hall", OnReqResetPwdHall);
            //注册时发送短信
            Program.moduleManager.RegisterRequestHandler("sms_Reg", OnReqSmsHallRegister);
            //绑定手机
            Program.moduleManager.RegisterRequestHandler("bind_phone", OnReqBindPhone);
            //--------礼品系统-----------
            //检查昵称
            Program.moduleManager.RegisterRequestHandler("gift_check_id", OnReqGiftCheckId);
            //确定送礼
            Program.moduleManager.RegisterRequestHandler("send_gift", OnReqSendGift);
            //请求礼物记录
            Program.moduleManager.RegisterRequestHandler("get_gift_record", OnReqGetGiftRecord);
            //--------邮件系统-----------
            //请求邮件列表
            Program.moduleManager.RegisterRequestHandler("get_mail_list", OnReqGetMailList);
            //删除邮件
            Program.moduleManager.RegisterRequestHandler("del_mail", OnReqDelMail);
            //--------银行系统-----------
            //银行存取操作
            Program.moduleManager.RegisterRequestHandler("bank_operate", OnReqBankOperate);
            //请求存取记录
            Program.moduleManager.RegisterRequestHandler("get_bank_record", OnReqGetBankRecord);
            //--------排行榜系统-----------
            //获取排行榜
            Program.moduleManager.RegisterRequestHandler("get_rank", OnReqGetRank);
            //--------游戏列表-------------
            //请求大类的游戏列表
            Program.moduleManager.RegisterRequestHandler("get_game_list", OnReqGetGameList);
            //请求游戏IP地址端口
            Program.moduleManager.RegisterRequestHandler("get_game_server", OnReqGetGameServer);
            //--------公告系统-------------
            Program.moduleManager.RegisterRequestHandler("get_notice", OnReqGetNotice);
            //--------支付系统-------------
            Program.moduleManager.RegisterRequestHandler("pay", OnPay);

            Program.moduleManager.RegisterRequestHandler("get_config", OnGetConfig);
        }

        public void Stop()
        {
            
        }

        public void OnAccepted(int workerIndex, HallServerSession session)
        {

        }
        public void OnClosed(int workerIndex, HallServerSession session, string closedCause, bool isInternalCause)
        {
            var dbLink = Program.dbSvc.GetLink(workerIndex);

            Program.dbHelper.RecordAccount(dbLink, "logout", session.show_id, session.ip, session.mac);

            session.imageVCText = "";
            session.show_id = "";
            session.ip = "";
            session.mac = "";
        }

        void LoadImageVC()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"vcs\";
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] fis = di.GetFiles("*.jpg");
            imageVCs = new ImageVC[fis.Length];
            
            for (int i = 0; i < fis.Length; i++)
            {
                FileInfo fi = fis[i];
                var imageVC = new ImageVC();

                FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);

                imageVC.imageData = new byte[fs.Length];
                fs.Read(imageVC.imageData, 0, (int)fs.Length);
                imageVC.text = fi.Name.Replace(".jpg", "");

                fs.Close();

                imageVCs[i] = imageVC;
            }
        }

        string GenSmsVC()
        {
            string[] numbers = new string[] {"1","2","3","4","5","6","7","8","9","0"};

            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < 6; i++)
            {
                string number = numbers[rand.Next(numbers.Length)];

                sb.Append(number);
            }

            return sb.ToString();
        }

        bool SendSms(string phone, string content)
        {
            string url = "http://dc.28inter.com/sms.aspx?action=send";
            string paramData = string.Format("userid={0}&account={1}&password={2}&mobile={3}&content={4}&sendtime=",
                1313, "xdwldhyyl", "xdwl2s9ky6q", phone, content);

            string result = HttpUtil.Post(url, "application/x-www-form-urlencoded", paramData);

            return (result.IndexOf("Success") >= 0);
        }

        void SetupTimer()
        {
            rank1StatTimer = new NormalTimer(DayOfWeek.Sunday, 23, 0, 0, () =>
            {

            });
            Program.timerSvc.AddTimer(rank1StatTimer);

            rank2StatTimer = new NormalTimer(5, 1, () =>
            {
                var dbLink = Program.dbSvc.GetLink(Program.dbSvc.GetLinkCount() - 1);
                Program.dbHelper.StatRecordRank(dbLink, "2");
            });
            Program.timerSvc.AddTimer(rank2StatTimer);
        }

        bool CheckToken(DatabaseService.DatabaseLink dbLink, string show_id, string token)
        {
            string tokenInDB = Program.dbHelper.GetUserLoginToken(dbLink, show_id);

            if (tokenInDB.Length == 0)
                System.Diagnostics.Debug.WriteLine("tokenInDB is null");
            if (tokenInDB != token)
            {
                return false;
            }

            return true;
        }

        void OnReqLogin(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string account = jObjRecv["account"].ToString();
            string login_pwd = jObjRecv["login_pwd"].ToString();
            string login_ip = jObjRecv["login_ip"].ToString();
            string login_mac = jObjRecv["login_mac"].ToString();
            int login_ismobile = (int)jObjRecv["login_ismobile"];

            //参数校验
            if (account.Trim().Length == 0)
                return;
            if (login_pwd.Trim().Length == 0)
                return;

            login_pwd = EncipherUtil.Md5(login_pwd);

            var dbLink = Program.dbSvc.GetLink(workerIndex);
            string show_id = "";
            string nick = "";

            using (var reader = Program.dbHelper.CheckUser(dbLink, account, login_pwd))
            {
                if (reader == null || !reader.Read())
                {
                    Program.server.SendError(session, cmd, "账号不存在或密码错误");
                    return;
                }

                if(reader.GetBoolSafe("locked"))
                {
                    Program.server.SendError(session, cmd, "该账户已经被冻结");
                    return;
                }

                show_id = reader.GetStringSafe("show_id");
                nick = reader.GetStringSafe("nick");
            }

            string login_token = CreateLoginToken();

            if(!Program.dbHelper.UpdateLoginUser(dbLink, login_token, login_ip, login_mac, login_ismobile, account))
            {
                Program.server.SendError(session, cmd, "更新用户登录信息失败");
                return;
            }

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["account"] = account;
            jObj["show_id"] = show_id;
            jObj["nick"] = nick;
            jObj["login_token"] = login_token;

            Program.server.Send(session, jObj);

            session.show_id = show_id;
            session.ip = login_ip;
            session.mac = login_mac;

            Program.dbHelper.RecordAccount(dbLink, "login", show_id, login_ip, login_mac);
        }
        void OnReqGetImageVC(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            int index = rand.Next(imageVCs.Length);
            ImageVC imageVC = imageVCs[index];

            session.imageVCText = imageVC.text;

            Program.server.Send(session, 1, imageVC.imageData);
        }

        string CreateShowId()
        {
	        string str = "0123456789";
	
	        string ret = "" + str[RandomUtil.RandomMinMax(rand, 1, str.Length - 1)];

            for (int i = 1; i < 8; i++)
		        ret += str[RandomUtil.RandomMinMax(rand, 0, str.Length - 1)];

            return ret;
        }

        string CreateLoginToken()
        {
            return EncipherUtil.NewGuid();
        }

        string CreateNick()
        {
            int num1 = rand.Next(331);
            int num2 = rand.Next(325);

            return nick1[num1] + nick2[num2];
        }
        void OnReqReg(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string account = jObjRecv["account"].ToString();
            string login_pwd = jObjRecv["login_pwd"].ToString();
            //string vcText = jObjRecv["vc"].ToString();
            string reg_ip = jObjRecv["reg_ip"].ToString();
            string reg_mac = jObjRecv["reg_mac"].ToString();
            string icon = jObjRecv["icon"].ToString();
            JObject jClientConfig =  Configure.Inst.jClientConfig;
            bool needPhone = (bool)jClientConfig["RegConfig"]["needPhone"];
            string phone = "";
            var dbLink = Program.dbSvc.GetLink(workerIndex);
            if (needPhone)
            {
                phone = jObjRecv["phone"].ToString();
                string vc = jObjRecv["vc"].ToString();
                if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(vc))
                {

                }
                if (session.vc != vc)
                {
                    Program.server.SendError(session, cmd, "验证码不正确");
                    return;
                }
                DateTime getVcTime = session.vcTime;
                if ((DateTime.Now - getVcTime).TotalSeconds > VertifyCodeExpiresSeconds)
                {
                    Program.server.SendError(session, cmd, "验证码已过期");
                    return;
                }
                //校验手机号是否已经满5个
                //判断绑定的手机是否超出限制
                if (Program.dbHelper.GetPhoneCount(dbLink, phone) >= 5)
                {
                    Program.server.SendError(session, cmd, "每个手机号码最多可以注册5个账号");
                    return;
                }



            }


            //参数校验
            if (account.Trim().Length == 0)
                return;
            if (login_pwd.Trim().Length == 0)
                return;

            //if (session.imageVCText.Length == 0)
            //{
            //    Program.server.SendError(session, cmd, "请先获取验证码");
            //    return;
            //}

            //if(session.imageVCText != vcText)
            //{
            //    Program.server.SendError(session, cmd, "验证码不正确");
            //    return;
            //}



            login_pwd = EncipherUtil.Md5(login_pwd);

           

            if(!Program.dbHelper.AddUser(dbLink, account, login_pwd))
            {
                Program.server.SendError(session, cmd, "账号已存在");
                return;
            }

            int retry = 0;
            string show_id;
            string nick;

            do
            {
                if (retry >= 100)
	            {
                    Program.server.SendError(session, cmd, "分配昵称失败，请重试");
                    return;
                }
		
	            show_id = CreateShowId();
	            nick = CreateNick();
	
	            retry++;
            }
            while (!Program.dbHelper.UpdateRegUser(dbLink, show_id, nick, icon, reg_ip, reg_mac, account));
            if (needPhone)
            {
                Program.dbHelper.SetUserPhone(dbLink, phone, show_id);
            }



            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["account"] = account;
            jObj["show_id"] = show_id;
            jObj["nick"] = nick;
            if (needPhone)
            {
                jObj["phone"] = phone;
            }
            Program.server.Send(session, jObj);

            Program.dbHelper.RecordAccount(dbLink, "reg", show_id, reg_ip, reg_mac);
        }
        void OnReqEnterHall(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if(!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetUserBaseInfo(dbLink, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    Program.server.SendError(session, cmd, "未找到该用户");
                    return;
                }

                Program.server.SendSuccessWithReader(session, cmd, reader,
                            "login_ip",
                            "login_mac",
                            "login_time",
                            "login_ismobile",
                            "icon",
                            "sign",
                            "money",
                            "bank_money",
                            "bank_pwd",
                            "phone",
                            "last_server",
                            "last_game",
                            "last_grade",
                            "last_table_id");
            }
        }

        void OnReqModifyInfo(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();
            string value = jObjRecv["value"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (value.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            bool ret = false;

            if (type == "1")
            {
                //修改昵称
                ret = Program.dbHelper.SetUserNick(dbLink, value, show_id);
            }
            else if (type == "2")
            {
                //修改签名
                ret = Program.dbHelper.SetUserSign(dbLink, value, show_id);
            }
            else if (type == "3")
            {
                //修改头像
                ret = Program.dbHelper.SetUserIcon(dbLink, value, show_id);
            }
            else
                return;

            if (!ret)
            {
                Program.server.SendError(session, cmd, "已存在该昵称");
                return;
            }

            Program.server.SendSuccess(session, cmd);
        }

        void OnReqModifyPwd(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();
            string oldPwd = jObjRecv["old"].ToString();
            string newPwd = jObjRecv["new"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (newPwd.Trim().Length == 0)
                return;

            newPwd = EncipherUtil.Md5(newPwd);

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (type == "1")
            {
                //修改银行密码
                string bank_pwd = Program.dbHelper.GetUserBankPwd(dbLink, show_id);

                if(bank_pwd.Length > 0)
                {
                    if (bank_pwd != EncipherUtil.Md5(oldPwd))
                    {
                        Program.server.SendError(session, cmd, "旧的银行密码不正确");
                        return;
                    }
                }
                else
                {
                    //开通密码
                }

                if (!Program.dbHelper.SetUserBankPwd(dbLink, newPwd, show_id))
                {
                    Program.server.SendError(session, cmd, "银行密码修改失败");
                    return;
                }
            }
            else if (type == "2")
            {
                if (oldPwd.Trim().Length == 0)
                    return;
                //修改登录密码
                string login_pwd = Program.dbHelper.GetUserLoginPwd(dbLink, show_id);

                if (login_pwd.Length > 0)
                {
                    if (login_pwd != EncipherUtil.Md5(oldPwd))
                    {
                        Program.server.SendError(session, cmd, "旧的登录密码不正确");
                        return;
                    }
                }
                else
                {
                    Program.server.SendError(session, cmd, "未注册？");
                    return;
                }

                if (!Program.dbHelper.SetUserLoginPwd(dbLink, newPwd, show_id))
                {
                    Program.server.SendError(session, cmd, "登录密码修改失败");
                    return;
                }
            }
            else
                return;

            Program.server.SendSuccess(session, cmd);
        }

        void OnReqSms(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string phone = jObjRecv["phone"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if(phone.Length == 0)
            {
                phone = Program.dbHelper.GetUserPhone(dbLink, show_id);

                if(phone.Length == 0)
                {
                    Program.server.SendError(session, cmd, "未设置手机号");
                    return;
                }
            }
            DateTime getVcTime = Program.dbHelper.GetUserGetVcTime(dbLink, show_id);

            if ((DateTime.Now - getVcTime).TotalSeconds <= VertifyCodeExpiresSeconds)
            {
                Program.server.SendError(session, cmd, $"请{VertifyCodeExpiresSeconds}秒后再次尝试发送短信");
                return;
            }


            string vc = GenSmsVC();

            //if(!SendSms(phone, "【云游棋牌】您的验证码为：" + vc))
            //{
            //    Program.server.SendError(session, cmd, "发送验证码失败，请重试");
            //    return;
            //}

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                SendSms(phone, "【云游棋牌】您的验证码为：" + vc);
            }));

            if (!Program.dbHelper.SetUserVc(dbLink, vc, show_id))
            {
                Program.server.SendError(session, cmd, "设置验证码失败");
                return;
            }

            if(!Program.dbHelper.SetUserGetVcTime(dbLink, DateTime.Now, show_id))
            {
                Program.server.SendError(session, cmd, "设置验证码失败");
                return;
            }

            Program.server.SendSuccess(session, cmd);
        }
        //发送短信游戏外
        void OnReqSmsHall(int workIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string account = jObjRecv["account"].ToString();
            string phone = jObjRecv["phone"].ToString();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(phone))
            {
                Program.server.SendError(session, cmd, "参数不完整");
                return;
            }
            string show_id = "";
            var dbLink = Program.dbSvc.GetLink(workIndex);
            using (var reader = Program.dbHelper.GetUserInfoByAccount(dbLink, account))
            {
                if (reader == null || !reader.Read())
                {
                    Program.server.SendError(session, cmd, "用户校验错误");
                    return;
                }
                if (reader["phone"].ToString() != phone)
                {
                    Program.server.SendError(session, cmd, "绑定手机信息错误");
                    return;
                }
                show_id = reader["show_id"].ToString();
            }
            DateTime getVcTime = session.vcTime;

            if ( !string.IsNullOrWhiteSpace(session.vc) && (DateTime.Now - getVcTime).TotalSeconds <= VertifyCodeExpiresSeconds)
            {
                Program.server.SendError(session, cmd, $"请{(DateTime.Now - getVcTime).TotalSeconds}秒后再次尝试发送短信");
                return;
            }


            string vc = GenSmsVC();

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
            {
                SendSms(phone, "【云游棋牌】您的验证码为：" + vc);
            }));

            //if (!SendSms(phone, "【云游棋牌】您的验证码为：" + vc))
            //{
            //    Program.server.SendError(session, cmd, "发送验证码失败，请重试");
            //    return;
            //}
            session.vc = vc;
            session.vcTime = DateTime.Now;
            Program.server.SendSuccess(session, cmd);
        }
        //注册时发送验证码
        void OnReqSmsHallRegister(int workIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            JObject jClientConfig = Configure.Inst.jClientConfig;
            bool needPhone = (bool)jClientConfig["RegConfig"]["needPhone"];
            if (!needPhone)
            {
                Program.server.SendError(session, cmd, "未开启此项功能");
                return;
            }
            string phone = jObjRecv["phone"].ToString();
            DateTime getVcTime = session.vcTime;

            if (!string.IsNullOrWhiteSpace(session.vc) && (DateTime.Now - getVcTime).TotalSeconds <= VertifyCodeExpiresSeconds)
            {
                Program.server.SendError(session, cmd, $"请{(DateTime.Now - getVcTime).TotalSeconds}秒后再次尝试发送短信");
                return;
            }

            string vc = GenSmsVC();



            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate {
                SendSms(phone, "【云游棋牌】您的验证码为：" + vc);
            }));
            session.vc = vc;
            session.vcTime = DateTime.Now;
            Program.server.SendSuccess(session, cmd);
        }





        //大厅修改密码
        void OnReqResetPwdHall(int workIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string account = jObjRecv["account"].ToString();
            string phone = jObjRecv["phone"].ToString();
            string vc = jObjRecv["vc"].ToString();
            string pwd = jObjRecv["pwd"].ToString();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(vc) || string.IsNullOrWhiteSpace(pwd))
            {
                Program.server.SendError(session, cmd, "参数不完整");
                return;
            }
            string show_id = "";
            var dbLink = Program.dbSvc.GetLink(workIndex);
            using (var reader = Program.dbHelper.GetUserInfoByAccount(dbLink, account))
            {
                if (reader == null || !reader.Read())
                {
                    Program.server.SendError(session, cmd, "用户校验错误");
                    return;
                }
                if (reader["phone"].ToString() != phone)
                {
                    Program.server.SendError(session, cmd, "绑定手机信息错误");
                    return;
                }
                show_id = reader["show_id"].ToString();
            }
            if (session.vc!=vc)
            {
                Program.server.SendError(session, cmd, "验证码不正确");
                return;
            }
            DateTime getVcTime = session.vcTime;

            if ((DateTime.Now - getVcTime).TotalSeconds > VertifyCodeExpiresSeconds)
            {
                Program.server.SendError(session, cmd, "验证码已过期");
                return;
            }
            pwd = EncipherUtil.Md5(pwd);
            //重设密码密码
            if (!Program.dbHelper.SetUserLoginPwd(dbLink, pwd, show_id))
            {
                Program.server.SendError(session, cmd, "登录密码修改失败");
                return;
            }
            Program.server.SendSuccess(session, cmd);
        }
        void OnReqResetPwd(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();
            string newPwd = jObjRecv["pwd"].ToString();
            string vc = jObjRecv["vc"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (newPwd.Trim().Length == 0)
                return;
            if (vc.Trim().Length == 0)
                return;

            newPwd = EncipherUtil.Md5(newPwd);

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (Program.dbHelper.GetUserVc(dbLink, show_id) != vc)
            {
                Program.server.SendError(session, cmd, "验证码不正确");
                return;
            }

            DateTime getVcTime = Program.dbHelper.GetUserGetVcTime(dbLink, show_id);

            if ((DateTime.Now - getVcTime).TotalSeconds > VertifyCodeExpiresSeconds)
            {
                Program.server.SendError(session, cmd, "验证码已过期");
                return;
            }

            if (type == "1")
            {
                //重设银行密码
                if (!Program.dbHelper.SetUserBankPwd(dbLink, newPwd, show_id))
                {
                    Program.server.SendError(session, cmd, "银行密码修改失败");
                    return;
                }
            }
            else if (type == "2")
            {
                //重设密码密码
                if (!Program.dbHelper.SetUserLoginPwd(dbLink, newPwd, show_id))
                {
                    Program.server.SendError(session, cmd, "登录密码修改失败");
                    return;
                }
            }
            else
                return;

            Program.server.SendSuccess(session, cmd);
        }

        void OnReqBindPhone(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string phone = jObjRecv["phone"].ToString();
            string loginPwd = jObjRecv["login_pwd"].ToString();
            string vc = jObjRecv["vc"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (phone.Trim().Length == 0)
                return;
            if (loginPwd.Trim().Length == 0)
                return;
            if (vc.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (Program.dbHelper.GetUserVc(dbLink, show_id) != vc)
            {
                Program.server.SendError(session, cmd, "验证码不正确");
                return;
            }

            DateTime getVcTime = Program.dbHelper.GetUserGetVcTime(dbLink, show_id);

            if ((DateTime.Now - getVcTime).TotalSeconds > VertifyCodeExpiresSeconds)
            {
                Program.server.SendError(session, cmd, "验证码已过期");
                return;
            }

            if (Program.dbHelper.GetUserLoginPwd(dbLink, show_id) != EncipherUtil.Md5(loginPwd))
            {
                Program.server.SendError(session, cmd, "登录密码不正确");
                return;
            }
            //判断绑定的手机是否超出限制
            if (Program.dbHelper.GetPhoneCount(dbLink, phone) >= 5)
            {
                Program.server.SendError(session, cmd, "每个手机号码最多可以绑定5个账号");
                return;
            }



            if (!Program.dbHelper.SetUserPhone(dbLink, phone, show_id))
            {
                Program.server.SendError(session, cmd, "绑定手机失败");
                return;
            }

            //Program.server.SendSuccess(session, cmd);
            JObject jObj = new JObject();
            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["phone"] = phone;
            Program.server.Send(session, jObj);
        }

        void OnReqGiftCheckId(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string dest_id = jObjRecv["dest_id"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (dest_id.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            string nick = Program.dbHelper.GetUserNick(dbLink, dest_id);

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["nick"] = nick;

            Program.server.Send(session, jObj);
        }

        void OnReqSendGift(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string receiver_id = jObjRecv["receiver_id"].ToString();
            long send_money = long.Parse(jObjRecv["send_money"].ToString());
            string bank_pwd = jObjRecv["bank_pwd"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (receiver_id.Trim().Length == 0)
                return;
            if (bank_pwd.Trim().Length == 0)
                return;

            if (send_money < 1000000)
            {
                Program.server.SendError(session, cmd, "小于100万");
                return;
            }

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            string user_bank_pwd = Program.dbHelper.GetUserBankPwd(dbLink, show_id);

            if (user_bank_pwd.Length == 0)
            {
                Program.server.SendError(session, cmd, "银行密码未设置");
                return;
            }

            bank_pwd = EncipherUtil.Md5(bank_pwd);

            if (user_bank_pwd != bank_pwd)
            {
                Program.server.SendError(session, cmd, "银行密码不正确");
                return;
            }

            string sender_id = show_id;

            //获取赠送者金币
            long sender_bank_money = Program.dbHelper.GetUserBankMoney(dbLink, sender_id);

            //检查金币是否够送
            if(sender_bank_money < send_money)
            {
                Program.server.SendError(session, cmd, "银行金币不足");
                return;
            }

            //获取接收者金币
            long receiver_bank_money = Program.dbHelper.GetUserBankMoney(dbLink, receiver_id);

            //赠送者减去金币
            sender_bank_money -= send_money;
            //接收者加上金币
            receiver_bank_money += send_money;

            //分别设置接收者和赠送者的金币
            Program.dbHelper.SetUserBankMoney(dbLink, sender_bank_money, sender_id);
            Program.dbHelper.SetUserBankMoney(dbLink, receiver_bank_money, receiver_id);

            string sender_nick = Program.dbHelper.GetUserNick(dbLink, sender_id);
            string receiver_nick = Program.dbHelper.GetUserNick(dbLink, receiver_id);
            DateTime create_time = DateTime.Now;

            dbLink.ExecuteNonQuery("record gift", sender_id, sender_nick, receiver_id, receiver_nick, send_money, create_time);

            string content = string.Format("<#FFFFFF>玩家<#3AB5B3>{0}<#FFFFFF>赠送给你价值<#3AB5B3>{1}<#FFFFFF>的礼物,礼物已兑换到银行,请查收",
                sender_nick, send_money);
            Program.dbHelper.AddMail(dbLink, receiver_id, content);

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["self_money"] = sender_bank_money.ToString();

            JObject jRecord = new JObject();

            jRecord["sender_id"] = sender_id;
            jRecord["sender_nick"] = sender_nick;
            jRecord["receiver_id"] = receiver_id;
            jRecord["receiver_nick"] = receiver_nick;
            jRecord["send_money"] = send_money.ToString();
            jRecord["time"] = create_time.ToString(DateTimeUtil.format);

            jObj["record"] = jRecord;

            Program.server.Send(session, jObj);
        }

        void OnReqGetGiftRecord(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetRecordGift(dbLink, show_id, type))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;
                jObj["type"] = type;

                JArray jList = new JArray();

                while (reader.Read())
                {
                    JObject jListItem = new JObject();

                    string field = (type == "1") ? "receiver_id" : "sender_id";
                    jListItem["peer_id"] = reader.GetStringSafe(field);
                    field = (type == "1") ? "receiver_nick" : "sender_nick";
                    jListItem["peer_nick"] = reader.GetStringSafe(field);
                    jListItem["send_money"] = reader.GetInt64Safe("send_money");
                    jListItem["time"] = reader.GetDateTimeSafe("create_time").ToString(DateTimeUtil.format);

                    jList.Add(jListItem);
                }

                jObj["list"] = jList;

                Program.server.Send(session, jObj);
            }
        }

        void OnReqBankOperate(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();
            long amount = long.Parse(jObjRecv["amount"].ToString());

            //参数校验
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (amount < 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            long money = Program.dbHelper.GetUserMoney(dbLink, show_id);
            long bank_money = Program.dbHelper.GetUserBankMoney(dbLink, show_id);

            long money_before = money;

            if (type == "1")
            {
                //存入
                if (money < amount)
                    return;

                money -= amount;
                bank_money += amount;
            }
            else if(type == "2")
            {
                //取出
                if (bank_money < amount)
                    return;

                string bank_pwd = jObjRecv["bank_pwd"].ToString();

                if (bank_pwd.Length == 0)
                    return;

                bank_pwd = EncipherUtil.Md5(bank_pwd);

                if(bank_pwd != Program.dbHelper.GetUserBankPwd(dbLink, show_id))
                {
                    Program.server.SendError(session, cmd, "银行密码不正确");
                    return;
                }

                bank_money -= amount;
                money += amount;
            }
            else
                return;

            Program.dbHelper.SetUserMoney(dbLink, money, show_id);
            Program.dbHelper.SetUserBankMoney(dbLink, bank_money, show_id);

            string nick = Program.dbHelper.GetUserNick(dbLink, show_id);

            dbLink.ExecuteNonQuery("record bank", DateTime.Now, show_id, nick, type, amount, money_before, money);

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["money"] = money.ToString();
            jObj["bank_money"] = bank_money.ToString();

            Program.server.Send(session, jObj);
        }

        void OnReqGetBankRecord(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();

            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetRecordBank(dbLink, show_id))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;

                JArray jList = new JArray();

                while (reader.Read())
                {
                    JObject jListItem = new JObject();

                    jListItem["time"] = reader.GetDateTimeSafe("create_time").ToString(DateTimeUtil.format);
                    jListItem["type"] = reader.GetStringSafe("operate_type");
                    jListItem["money"] = reader.GetInt64Safe("money");
                    jListItem["money_before"] = reader.GetInt64Safe("money_before");
                    jListItem["money_last"] = reader.GetInt64Safe("money_last");

                    jList.Add(jListItem);
                }

                jObj["list"] = jList;

                Program.server.Send(session, jObj);
            }
        }

        void OnReqGetMailList(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();

            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetUserMail(dbLink, show_id))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;

                JArray jList = new JArray();

                while (reader.Read())
                {
                    JObject jListItem = new JObject();

                    jListItem["time"] = reader.GetDateTimeSafe("create_time").ToString(DateTimeUtil.format);
                    jListItem["content"] = reader.GetStringSafe("content");
                    jListItem["id"] = reader.GetStringSafe("ID");

                    jList.Add(jListItem);
                }

                jObj["list"] = jList;

                Program.server.Send(session, jObj);
            }
        }

        void OnReqDelMail(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();

            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            if (type == "1")
            {
                Program.dbHelper.DelOneMail(dbLink, int.Parse(jObjRecv["id"].ToString()));
            }
            else if (type == "2")
            {
                Program.dbHelper.DelAllMail(dbLink, show_id);
            }
            else
            {
                return;
            }

            Program.server.SendSuccess(session, cmd);
        }

        void OnReqGetRank(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();

            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            JObject jObj = new JObject();
            JObject jSelf = new JObject();
            JArray jList = new JArray();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["type"] = type;

            //先找自己的
            using (var reader = Program.dbHelper.GetRecordRank(dbLink, type, show_id))
            {
                if (reader == null || !reader.Read())
                {
                    Program.server.SendError(session, cmd, "未找到自己记录");
                    return;
                }

                jSelf["show_id"] = show_id;
                jSelf["icon"] = reader.GetStringSafe("icon");
                jSelf["nick"] = reader.GetStringSafe("nick");
                jSelf["sign"] = reader.GetStringSafe("sign");
                jSelf["money"] = reader.GetStringSafe("money");
                jSelf["rank_value"] = reader.GetStringSafe("rank_value");
                jSelf["rank_num"] = reader.GetStringSafe("rank_num");

                jObj["self"] = jSelf;
            }

            using (var reader = Program.dbHelper.GetRecordRank(dbLink, type))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                while (reader.Read())
                {
                    JObject jListItem = new JObject();

                    jListItem["show_id"] = reader.GetStringSafe("show_id");
                    jListItem["icon"] = reader.GetStringSafe("icon");
                    jListItem["nick"] = reader.GetStringSafe("nick");
                    jListItem["sign"] = reader.GetStringSafe("sign");
                    jListItem["money"] = reader.GetStringSafe("money");
                    jListItem["rank_value"] = reader.GetStringSafe("rank_value");
                    jListItem["rank_num"] = reader.GetStringSafe("rank_num");

                    jList.Add(jListItem);
                }

                
                jObj["list"] = jList;
            }

            Program.server.Send(session, jObj);
        }

        void OnReqGetGameList(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string kind = jObjRecv["kind"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (kind.Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetGameList(dbLink, kind))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;
                jObj["kind"] = kind;

                JArray jList = new JArray();

                while (reader.Read())
                {
                    JObject jListItem = new JObject();

                    jListItem["name"] = reader.GetStringSafe("name");
                    jListItem["jackpot"] = 0;

                    jList.Add(jListItem);
                }

                jObj["list"] = jList;

                Program.server.Send(session, jObj);
            }
        }

        void OnReqGetGameServer(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string game_name = jObjRecv["game_name"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;
            if (game_name.Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetGameServer(dbLink, game_name))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;
                jObj["game_name"] = game_name;

                List<string> ip_ports = new List<string>();

                while (reader.Read())
                {
                    string ip_port = reader.GetStringSafe("ip_port");

                    if (ip_port.Length > 0)
                        ip_ports.Add(ip_port);
                }

                if (ip_ports.Count == 0)
                {
                    Program.server.SendError(session, cmd, "无可用的游戏服务器");
                    return;
                }

                //todo 根据负载来选择游戏服务器
                jObj["ip_port"] = ip_ports[rand.Next(ip_ports.Count)];

                Program.server.Send(session, jObj);
            }
        }

        void OnReqGetNotice(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            string type = jObjRecv["type"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            using (var reader = Program.dbHelper.GetGetNotice(dbLink, type))
            {
                if (reader == null)
                {
                    Program.server.SendError(session, cmd, "未找到任何记录");
                    return;
                }

                JObject jObj = new JObject();

                jObj["cmd"] = cmd;
                jObj["ret_code"] = 0;
                jObj["type"] = type;

                JArray jList = new JArray();

                while (reader.Read())
                {
                    JObject jItem = new JObject();

                    jItem["time"] = reader.GetDateTimeSafe("create_time").ToString(DateTimeUtil.format);
                    jItem["title"] = reader.GetStringSafe("title");
                    jItem["content"] = reader.GetStringSafe("content");

                    jList.Add(jItem);
                }

                jObj["list"] = jList;

                Program.server.Send(session, jObj);
            }
        }

        void OnGetConfig(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["data"] = Configure.Inst.jClientConfig;

            Program.server.Send(session, jObj);
        }

        void OnPay(int workerIndex, HallServerSession session, string cmd, JObject jObjRecv)
        {
            string show_id = jObjRecv["show_id"].ToString();
            string token = jObjRecv["token"].ToString();
            int id = (int)jObjRecv["id"];
            string type = jObjRecv["type"].ToString();

            //参数验证
            if (show_id.Trim().Length == 0)
                return;
            if (token.Trim().Length == 0)
                return;

            var dbLink = Program.dbSvc.GetLink(workerIndex);

            if (!CheckToken(dbLink, show_id, token))
            {
                Program.server.SendError(session, cmd, "token非法或已过期");
                return;
            }

            JArray jShopConfig = (JArray)Configure.Inst.jClientConfig["ShopConfig"];

            if (id < 0 || id >= jShopConfig.Count)
                return;

            int money = (int)jShopConfig[id]["needMoney"];
            string tradeNumber = EncipherUtil.NewGuid();

            Program.dbHelper.RecordTrade(dbLink, tradeNumber, show_id, id, type, money * 10000);

            if (type == "AliWap")
            {
                ThreadPool.QueueUserWorkItem(Pay, 
                    new object[] { show_id, money, tradeNumber, session, cmd, "pay.alipay.wap" });
            }
            if (type == "WxWap")
            {

            }
            if (type == "QQWap")
            {

            }
            if (type == "BankQuick")
            {

            }
        }

        static string PayXmlSendFormat1 =
            "<xml>" +
                "<service>{0}</service >" +
                "<mch_id>{1}</mch_id>" +
                "<total_fee>{2}</total_fee>" +
                "<out_trade_no>{3}</out_trade_no>" +
                "<spbill_create_ip>{4}</spbill_create_ip>" +
                "<nonce_str>{5}</nonce_str>" +
                "<body>{6}</body>" +
                "<notify_url>{7}</notify_url>" +
                "<sign>{8}</sign>" +
            "</xml>";

        static string payUrl = "https://spay.6-pay.com/api/gateway";
        static string mch_id = "17205";
        static string key = "1f78cbdc37909b924f7f0a94121a1250";

        string GetPayParamValue(string name, List<PayParam> payParams)
        {
            foreach(var p in payParams)
            {
                if (p.name == name)
                    return p.value;
            }

            return null;
        }
        void Pay(object para)
        {
            object[] paras = (object[])para;

            string show_id = paras[0].ToString();
            int money = (int)paras[1];
            string tradeNumber = paras[2].ToString();
            HallServerSession session = (HallServerSession)paras[3];
            string cmd = paras[4].ToString();
            string service = paras[5].ToString();

            List<PayParam> payParams = new List<PayParam>();

            payParams.Add(new PayParam("service", service));
            payParams.Add(new PayParam("mch_id", mch_id));
            payParams.Add(new PayParam("total_fee", money.ToString()));
            payParams.Add(new PayParam("out_trade_no", tradeNumber));
            payParams.Add(new PayParam("spbill_create_ip", "182.148.56.23"));
            payParams.Add(new PayParam("nonce_str", show_id));
            payParams.Add(new PayParam("body", "body"));
            payParams.Add(new PayParam("notify_url", Configure.Inst.GetPayCallbackUrl("AliWap")));

            payParams.Sort((PayParam p1, PayParam p2) =>
            {
                return p1.name.CompareTo(p2.name);
            });

            string stringA = "";

            foreach(PayParam p in payParams)
            {
                stringA += (p.name + "=" + p.value);
                stringA += "&";
            }

            string stringSignTemp = stringA + "key=" + key;

            string sign = EncipherUtil.Md5(stringSignTemp).ToUpper();

            string xmlRequest = string.Format(PayXmlSendFormat1,
                GetPayParamValue("service", payParams),
                GetPayParamValue("mch_id", payParams),
                GetPayParamValue("total_fee", payParams),
                GetPayParamValue("out_trade_no", payParams),
                GetPayParamValue("spbill_create_ip", payParams),
                GetPayParamValue("nonce_str", payParams),
                GetPayParamValue("body", payParams),
                GetPayParamValue("notify_url", payParams),
                sign);

            string response = HttpUtil.PostSSL(payUrl, "application/xml", xmlRequest);

            XmlDocument xmlResponse = new XmlDocument();
            xmlResponse.LoadXml(response);

            XmlNode xmlNode = xmlResponse.SelectSingleNode("xml/status");
            string status = xmlNode.FirstChild.InnerText;

            if(status != "0")
            {
                xmlNode = xmlResponse.SelectSingleNode("xml/message");
                string message = xmlNode.FirstChild.InnerText;
                Program.server.SendError(session, cmd, message);
                return;
            }

            xmlNode = xmlResponse.SelectSingleNode("xml/code_url");
            string code_url = xmlNode.FirstChild.InnerText;

            JObject jObj = new JObject();

            jObj["cmd"] = cmd;
            jObj["ret_code"] = 0;
            jObj["url"] = code_url;

            Program.server.Send(session, jObj);
        }

        bool QQWapPay()
        {
            return true;
        }

        bool BankQuickPay()
        {
            return true;
        }
    }
}
