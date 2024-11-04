using System.Text.RegularExpressions;

namespace SrtCleaner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /// 网易见外的图盘
            var path = @"D:\Users\Downloads\CHS_总会特别教育216次.srt";
            var text = File.ReadAllText(path);
            var text_split = text.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            foreach (var text_item in text_split)
            {
                if(Regex.IsMatch(text_item, @"[\u4e00-\u9fa5]"))
                {
                    list.AddRange(text_item.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                }
            }
            File.WriteAllText(@"D:\Users\Downloads\总会特别教育216次.txt", string.Join("，", list));
        }
    }
}
