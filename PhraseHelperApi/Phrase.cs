using System.Text.RegularExpressions;

namespace PhraseHelperApi
{
    public class Phrase
    {
        public PhraseType Type { get; set; }
        public string Title { get; set; }

        private string _content;
        public string Content
        {
            get { return ProcessContent(_content); }
            set { _content = value; }
        }

        private string ProcessContent(string originalContent)
        {
            if (originalContent == null)
                return null;

            string processedContent = originalContent;

            var replacedSeasons = new HashSet<string>();

            processedContent = Regex.Replace(processedContent, @"\{其他四季\}", match =>
            {
                string randomSeason = GetOtherSeasons();

                while (replacedSeasons.Contains(randomSeason))
                {
                    randomSeason = GetOtherSeasons();
                }

                replacedSeasons.Add(randomSeason);

                return randomSeason;
            });

            processedContent = processedContent
                .Replace("{四季}", GetSeasons())
                .Replace("{上午学习}", "9点~10点半")
                .Replace("{下午学习}", "2点半~4点")
                .Replace("{晚上学习}", "8点~9点半")
                .Replace("{上午学习HHmm}", "9:00~10:30")
                .Replace("{下午学习HHmm}", "14:30~16:00")
                .Replace("{晚上学习HHmm}", "20:00~21:30")
                .Replace("{上午学习开始}", "9点")
                .Replace("{下午学习开始}", "2点半")
                .Replace("{晚上学习开始}", "8点")
                .Replace("{上午学习开始HHmm}", "9:00")
                .Replace("{下午学习开始HHmm}", "14:30")
                .Replace("{晚上学习开始HHmm}", "20:00")
                .Replace("{时间段}", GetTimeOfDay())
                .Replace("{姓氏}", "寿")
                .Replace("{弟兄或姊妹}", "弟兄");

            return processedContent;
        }

        public void OutputHandle(int type)
        {
            if (type == 1) // 轻度替换敏感词
            {
                Content = Content
                    .Replace("神学", "神+学")
                    .Replace("神学班", "神+学班")
                    .Replace("学费", "学+费")
                    .Replace("开班", "开班")
                    .Replace("免费", "免+费")
                    .Replace("课程", "课+程")
                    .Replace("名字", "名字")
                    .Replace("电话", "电话")
                    .Replace("报名", "报+名")
                    .Replace("神", "神")
                    .Replace("耶和华", "耶和华")
                    .Replace("圣经", "圣+经")
                    .Replace("查经", "查+经")
                    .Replace("蒙福", "蒙+福")
                    .Replace("天父", "天+父")
                    .Replace("奥秘", "奥+秘")
                    .Replace("天国", "天+国")
                    .Replace("属灵", "属+灵")
                    .Replace("侍奉", "侍+奉")
                    .Replace("旨意", "旨+意")
                    .Replace("名额", "名额")
                    .Replace("联系方式", "联系方式")
                    .Replace("报名", "报+名")
                    .Replace("耶稣", "耶+稣")
                    .Replace("恩典", "恩+典")
                    .Replace("警醒", "警+醒")
                    .Replace("启示录", "启示录")
                    .Replace("福音", "福+音")
                    .Replace("祷告", "祷+告")
                    .Replace("使徒", "使+徒")
                    .Replace("圣灵", "圣+灵")
                    .Replace("传道", "传+道")
                    .Replace("布道", "布+道")
                    .Replace("见证", "见+证")
                    .Replace("信仰", "信+仰")
                    .Replace("基督", "基+督")
                    .Replace("礼拜", "礼+拜")
                    .Replace("教堂", "教+堂")
                    .Replace("恩赐", "恩+赐")
                    .Replace("赞美", "赞+美")
                    .Replace("救恩", "救+恩")
                    .Replace("奉献", "奉+献")
                    .Replace("异象", "异+象")
                    .Replace("复活", "复+活")
                    .Replace("门徒", "门+徒")
                    .Replace("荣耀", "荣+耀")
                    .Replace("悔改", "悔+改")
                    .Replace("救赎", "救+赎")
                    .Replace("洗礼", "洗+礼")
                    .Replace("传福音", "传+福+音")
                    .Replace("信徒", "信+徒")
                    .Replace("灵修", "灵+修")
                    .Replace("恩膏", "恩+膏")
                    .Replace("祝福", "祝+福")
                    .Replace("祝祷", "祝+祷")
                    .Replace("安慰", "安+慰")
                    .Replace("敬拜", "敬+拜")
                    .Replace("预言", "预+言");
            }

            if (type == 2) // 重度替换敏感词
            {
                Content = Content
                    .Replace("神学", "s学")
                    .Replace("神学班", "s学ban")
                    .Replace("学费", "学fei")
                    .Replace("开班", "开b")
                    .Replace("免费", "免~fei")
                    .Replace("课程", "课c")
                    .Replace("名字", "名z")
                    .Replace("电话", "电hua")
                    .Replace("报名", "报ming")
                    .Replace("神", "s")
                    .Replace("耶和华", "父亲")
                    .Replace("圣经", "智慧书")
                    .Replace("查经", "查jing")
                    .Replace("蒙福", "蒙f")
                    .Replace("天父", "天fu")
                    .Replace("奥秘", "奥mi")
                    .Replace("天国", "tg")
                    .Replace("属灵", "属ling")
                    .Replace("侍奉", "侍feng")
                    .Replace("旨意", "旨yi")
                    .Replace("名额", "名e")
                    .Replace("联系方式", "联xi方shi")
                    .Replace("报名", "报ming")
                    .Replace("耶稣", "ye稣")
                    .Replace("恩典", "恩dian")
                    .Replace("警醒", "警xing")
                    .Replace("启示录", "qsl")
                    .Replace("福音", "f音")
                    .Replace("祷告", "祷g")
                    .Replace("使徒", "使t")
                    .Replace("圣灵", "圣ling")
                    .Replace("传道", "传d")
                    .Replace("布道", "布d")
                    .Replace("见证", "见zheng")
                    .Replace("信仰", "信yang")
                    .Replace("基督", "基du")
                    .Replace("礼拜", "礼b")
                    .Replace("教堂", "教t")
                    .Replace("恩赐", "恩ci")
                    .Replace("赞美", "zan美")
                    .Replace("救恩", "救en")
                    .Replace("奉献", "奉xian")
                    .Replace("异象", "异xiang")
                    .Replace("复活", "复huo")
                    .Replace("门徒", "门tu")
                    .Replace("荣耀", "荣yao")
                    .Replace("悔改", "悔gai")
                    .Replace("救赎", "救shu")
                    .Replace("洗礼", "洗li")
                    .Replace("传福音", "传f音")
                    .Replace("信徒", "信tu")
                    .Replace("灵修", "灵xiu")
                    .Replace("恩膏", "恩gao")
                    .Replace("祝福", "祝fu")
                    .Replace("祝祷", "祝dao")
                    .Replace("安慰", "安wei")
                    .Replace("敬拜", "敬bai")
                    .Replace("预言", "预yan");
            }
        }

        private string GetSeasons()
        {
            // 根据当前时间获取季节
            DateTime now = DateTime.Now;
            int month = now.Month;
            switch (month)
            {
                case 1:
                case 2:
                case 3:
                    return "春季";
                case 4:
                case 5:
                case 6:
                    return "夏季";
                case 7:
                case 8:
                case 9:
                    return "秋季";
                case 10:
                case 11:
                case 12:
                    return "冬季";
                default:
                    return "未知";
            }
        }

        private string GetOtherSeasons()
        {
            var currentSeason = GetSeasons();
            var otherSeasons = new List<string> { "春季", "夏季", "秋季", "冬季" };

            otherSeasons.Remove(currentSeason);

            Random random = new Random();
            int index = random.Next(0, otherSeasons.Count);
            return otherSeasons[index];
        }

        private string GetTimeOfDay()
        {
            // 根据当前时间返回时间段
            DateTime now = DateTime.Now;
            int hour = now.Hour;
            if (hour >= 5 && hour < 12)
                return "早上";
            else if (hour >= 12 && hour < 13)
                return "中午";
            else if (hour >= 13 && hour < 18)
                return "下午";
            else
                return "晚上";
        }
    }

    public enum PhraseType
    {
        ShortPhrase = 1,
        LongPhrase = 2,
        StudyPhrase = 3
    }
}