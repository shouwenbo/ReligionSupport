using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoFileDownloaderApp
{
    public partial class Downloader : Form
    {
        public string Dir { get; set; }
        public string Title { get; set; }
        public List<AudioItem> ItemList { get; set; }

        public Downloader(string dir, string title, List<AudioItem> list)
        {
            InitializeComponent();
            this.Dir = dir;
            this.Title = title;
            this.ItemList = list;
            CheckForIllegalCrossThreadCalls = false;
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }

            List<string> urls = ItemList.Select(item => item.Url).ToList();

            var audioUrls = urls.Where(u => u.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)).ToList();
            var imageUrls = urls.Where(u => u.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                            u.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                            u.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToList();

            var audioCount = audioUrls.Count;
            var imageCount = imageUrls.Count;

            Task.Run(() =>
            {
                var thread = new Thread(async () =>
                {
                    try
                    {
                        string[] chineseNumbers = { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩" };
                        for (int i = 0; i < audioUrls.Count; i++)
                        {
                            string url = audioUrls[i];
                            string extension = Path.GetExtension(url).ToLower();
                            string fileName;

                            if (audioUrls.Count == 1)
                            {
                                fileName = $"{Dir}\\{Title}{extension}";
                            }
                            else
                            {
                                fileName = $"{Dir}\\{Title}（{i + 1}）{extension}";
                            }

                            try
                            {
                                this.lbl_progress.Text = $"{this.Title} 正在下载音频：{i + 1}/{audioCount}";
                                using HttpClient client = new();
                                byte[] data = await client.GetByteArrayAsync(url);
                                await File.WriteAllBytesAsync(fileName, data);
                            }
                            catch (Exception ex)
                            {
                                this.lbl_status.Text = $"下载音频失败：{url}，错误：{ex.Message}";
                                MessageBox.Show($"下载音频失败：{url}，错误：{ex.Message}");
                            }
                        }
                        for (int i = 0; i < imageUrls.Count; i++)
                        {
                            string url = imageUrls[i];
                            string extension = Path.GetExtension(url).ToLower();
                            string fileName;

                            if (imageUrls.Count == 1)
                            {
                                fileName = $"{Dir}\\{Title}{extension}";
                            }
                            else
                            {
                                fileName = $"{Dir}\\{Title}{chineseNumbers[i]}{extension}";
                            }

                            try
                            {
                                this.lbl_progress.Text = $"{this.Title} 正在下载图片：{i + 1}/{imageCount}";
                                using HttpClient client = new();
                                byte[] data = await client.GetByteArrayAsync(url);
                                await File.WriteAllBytesAsync(fileName, data);
                            }
                            catch (Exception ex)
                            {
                                this.lbl_status.Text = $"下载图片失败：{url}，错误：{ex.Message}";
                                MessageBox.Show($"下载图片失败：{url}，错误：{ex.Message}");
                            }
                        }

                        // ★★★ 下载全部完成后自动关闭窗口 ★★★
                        this.Invoke(new Action(() =>
                        {
                            this.lbl_status.Text = "所有下载已完成，窗口将自动关闭。";
                            this.Close();
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            });
        }
    }
}
