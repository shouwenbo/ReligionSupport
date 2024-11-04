using Xceed.Words.NET;

namespace WeeknoteBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = @"F:\个人 & 日记";
            DateTime today = DateTime.Today;
            DateTime lastSunday = GetLastSunday(today);
            var day1 = lastSunday.AddDays(-6).AddYears(-1983);
            var day2 = lastSunday.AddDays(-5).AddYears(-1983);
            var day3 = lastSunday.AddDays(-4).AddYears(-1983);
            var day4 = lastSunday.AddDays(-3).AddYears(-1983);
            var day5 = lastSunday.AddDays(-2).AddYears(-1983);
            var day6 = lastSunday.AddDays(-1).AddYears(-1983);
            var day7 = lastSunday.AddYears(-1983);
            var day1FileName = $@"{dir}\{day1.Year}.{day1.Month}.{day1.Day}.txt";
            var day2FileName = $@"{dir}\{day2.Year}.{day2.Month}.{day2.Day}.txt";
            var day3FileName = $@"{dir}\{day3.Year}.{day3.Month}.{day3.Day}.txt";
            var day4FileName = $@"{dir}\{day4.Year}.{day4.Month}.{day4.Day}.txt";
            var day5FileName = $@"{dir}\{day5.Year}.{day5.Month}.{day5.Day}.txt";
            var day6FileName = $@"{dir}\{day6.Year}.{day6.Month}.{day6.Day}.txt";
            var day7FileName = $@"{dir}\{day7.Year}.{day7.Month}.{day7.Day}.txt";
            if (!File.Exists(day1FileName))
            {
                Console.WriteLine($"{day1FileName} 文件不存在");
                return;
            }
            if (!File.Exists(day2FileName))
            {
                Console.WriteLine($"{day2FileName} 文件不存在");
                return;
            }
            if (!File.Exists(day3FileName))
            {
                Console.WriteLine($"{day3FileName} 文件不存在");
                return;
            }
            if (!File.Exists(day4FileName))
            {
                Console.WriteLine($"{day4FileName} 文件不存在");
                return;
            }
            if (!File.Exists(day5FileName))
            {
                Console.WriteLine($"{day5FileName} 文件不存在");
                return;
            }
            if (!File.Exists(day6FileName))
            {
                Console.WriteLine($"{day6FileName} 文件不存在");
                return;
            }
            if (!File.Exists(day7FileName))
            {
                Console.WriteLine($"{day7FileName} 文件不存在");
                return;
            }
            var day1content = File.ReadAllText(day1FileName);
            var day2content = File.ReadAllText(day2FileName);
            var day3content = File.ReadAllText(day3FileName);
            var day4content = File.ReadAllText(day4FileName);
            var day5content = File.ReadAllText(day5FileName);
            var day6content = File.ReadAllText(day6FileName);
            var day7content = File.ReadAllText(day7FileName);


            var startDate = day1.ToString("yy-M-d");
            var endDate = day7.ToString("yy-M-d");
            var path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\{endDate} 周记.docx";

            using (var document = DocX.Create(path, Xceed.Document.NET.DocumentTypes.Document))
            {
                document.InsertParagraph().Append("周记：寿文博");
                document.InsertParagraph().Append($"时间段：{startDate} - {endDate}");
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周一：");
                document.InsertParagraph().Append(day1content);
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周二：");
                document.InsertParagraph().Append(day2content);
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周三：");
                document.InsertParagraph().Append(day3content);
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周四：");
                document.InsertParagraph().Append(day4content);
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周五：");
                document.InsertParagraph().Append(day5content);
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周六：");
                document.InsertParagraph().Append(day6content);
                document.InsertParagraph().Append("");

                document.InsertParagraph().Append("周日：");
                document.InsertParagraph().Append(day7content);
                document.InsertParagraph().Append("");

                document.Save();
            }
        }
        static DateTime GetLastSunday(DateTime currentDate)
        {
            // 获取当前日期的星期几
            DayOfWeek currentDayOfWeek = currentDate.DayOfWeek;

            // 如果今天是星期天，返回今天
            if (currentDayOfWeek == DayOfWeek.Sunday)
            {
                return currentDate;
            }

            // 计算距离上一个星期天的天数
            int daysUntilLastSunday = (int)currentDayOfWeek;

            // 减去天数得到上一个星期天
            DateTime lastSunday = currentDate.AddDays(-daysUntilLastSunday);

            return lastSunday;
        }
    }
}
