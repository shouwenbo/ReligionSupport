using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace AutoFileDownloaderApp
{
    public class LinkResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<LinkData> Data { get; set; }
    }

    public class LinkData
    {
        [JsonProperty("category_name")]
        public string CategoryName { get; set; }
        [JsonProperty("filenames")]
        public string FileNames { get; set; }
        public List<AudioItem> FileList
        {
            get
            {
                var list = new List<string>();
                try
                {
                    list = JsonConvert.DeserializeObject<List<string>>(FileNames ?? "") ?? [];
                    return [.. list.Select(p => new AudioItem
                    {
                        DisplayName = $"https://cdn.great-supper.com/{p}",
                        Url = $"https://cdn.great-supper.com/{Uri.EscapeDataString(p)}"
                    })];
                }
                catch
                {
                    return new List<AudioItem>();
                }
            }
        }
        public string Dir
        {
            get
            {
                return $@"F:\福音房\上海 午间CD分享\{FileName}\";
            }
        }
        public string FileName
        {
            get
            {
                return Title.Replace("-午间CD分享-", " ");
            }
        }
        [JsonProperty("title")]
        public string Title { get; set; }
    }
    
    public static class LinkHelper
    {
        public static LinkResponse SaveAndOpenLinkInfo(string jsonString)
        {
            string outputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LinkInfo.txt");

            try
            {
                var responseObj = JsonConvert.DeserializeObject<LinkResponse>(jsonString);
                using StreamWriter sw = new StreamWriter(outputFile, false);
                
                if (responseObj?.Data != null)
                {
                    foreach (var item in responseObj.Data)
                    {
                        sw.WriteLine($"Dir: {item.Dir}");
                        sw.WriteLine($"FileName: {item.FileName}");
                        sw.WriteLine("Filenames:");
                        if (item.FileList != null)
                        {
                            foreach (var f in item.FileList)
                            {
                                sw.WriteLine($"Url：{f.Url}");
                                sw.WriteLine($"DisplayName：{f.DisplayName}");
                            }
                        }
                        sw.WriteLine(new string('-', 50));
                    }
                }
                else
                {
                    sw.WriteLine("无数据");
                }

                sw.Flush();

                // 自动用默认程序打开文件（通常是记事本）
                // ProcessStartInfo psi = new ProcessStartInfo
                // {
                //     FileName = outputFile,
                //     UseShellExecute = true
                // };
                // Process.Start(psi);
                // 
                // MessageBox.Show($"数据已保存并打开：{outputFile}");
                
                return responseObj;
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败：" + ex.Message);
                return null;
            }
        }
    }
}
