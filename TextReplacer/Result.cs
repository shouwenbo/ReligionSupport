using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextReplacer
{
    public partial class Result : Form
    {
        public Dictionary<string, int> replacementCount;
        public Result(string txt, Dictionary<string, int> replacementCount, IEnumerable<string> loopWords)
        {
            InitializeComponent();
            this.rich_result.Text = txt;
            this.replacementCount = replacementCount;

            if (loopWords.Any())
            {
                this.list_replace.Items.Add("==== 检测到死循环词 ====");
                foreach (var word in loopWords.Distinct())
                {
                    this.list_replace.Items.Add($"⚠ 死循环：{word}");
                }
            }

            // 假设有一个 ListBox 控件，名字是 listBoxWords
            this.list_replace.Items.Clear();

            // 按替换次数从多到少排序
            var sortedReplacementCount = replacementCount
                .OrderByDescending(item => item.Value)
                .ToList();

            // 将排序结果绑定到 ListBox
            foreach (var item in sortedReplacementCount)
            {
                this.list_replace.Items.Add($"{item.Key} 替换 {item.Value} 次");
            }
        }

        public Result(string txt)
        {
            InitializeComponent();
            this.rich_result.Text = txt;
        }

        private void btn_copy_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(rich_result.Text.ToString());
            this.Close();
        }
    }
}
