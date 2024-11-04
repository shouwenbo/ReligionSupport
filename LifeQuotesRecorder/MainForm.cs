using System.Diagnostics;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace LifeQuotesRecorder
{
    public partial class MainForm : Form
    {
        private const string rootDir = @"F:\粮食\粮食 & 生命语录";
        private const string rootSmylRepositoryDir = @$"{rootDir}\生命语录 - 仓库";
        private const string rootSmylDaogaoRepositoryDir = @$"{rootDir}\生命语录 - 仓库\祷告";
        private const string rootSmylChuandaoRepositoryDir = @$"{rootDir}\生命语录 - 仓库\传道";
        private const string rootSmylXinxinRepositoryDir = @$"{rootDir}\生命语录 - 仓库\信心";
        private const string rootSmylShouyinRepositoryDir = @$"{rootDir}\生命语录 - 仓库\受印";
        private const string rootSmylXinyangguanliRepositoryDir = @$"{rootDir}\生命语录 - 仓库\信仰管理";
        private const string rootSmylShimingRepositoryDir = @$"{rootDir}\生命语录 - 仓库\使命";
        private const string rootSmylXingweiRepositoryDir = @$"{rootDir}\生命语录 - 仓库\行为";
        private const string rootSmylZongheRepositoryDir = @$"{rootDir}\生命语录 - 仓库\综合";
        private const string rootSmylDir = @$"{rootDir}\生命语录 - 汇总";

        private const string rootJwRepositoryDir = @$"{rootDir}\经文分享 - 仓库";
        private const string rootJwDaogaoRepositoryDir = @$"{rootDir}\经文分享 - 仓库\祷告";
        private const string rootJwChuandaoRepositoryDir = @$"{rootDir}\经文分享 - 仓库\传道";
        private const string rootJwXinxinRepositoryDir = @$"{rootDir}\经文分享 - 仓库\信心";
        private const string rootJwShouyinRepositoryDir = @$"{rootDir}\经文分享 - 仓库\受印";
        private const string rootJwXinyangguanliRepositoryDir = @$"{rootDir}\经文分享 - 仓库\信仰管理";
        private const string rootJwShimingRepositoryDir = @$"{rootDir}\经文分享 - 仓库\使命";
        private const string rootJwXingweiRepositoryDir = @$"{rootDir}\经文分享 - 仓库\行为";
        private const string rootJwZongheRepositoryDir = @$"{rootDir}\经文分享 - 仓库\综合";
        private const string rootJwDir = @$"{rootDir}\经文分享 - 汇总";

        public MainForm()
        {
            InitializeComponent();
            if (!Directory.Exists(rootDir))
            {
                MessageBox.Show("根目录不存在");
                Application.Exit();
                System.Environment.Exit(0);
                return;
            }
            if (!Directory.Exists(rootSmylRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylRepositoryDir);
            }
            if (!Directory.Exists(rootSmylDaogaoRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylDaogaoRepositoryDir);
            }
            if (!Directory.Exists(rootSmylChuandaoRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylChuandaoRepositoryDir);
            }
            if (!Directory.Exists(rootSmylXinxinRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylXinxinRepositoryDir);
            }
            if (!Directory.Exists(rootSmylShouyinRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylShouyinRepositoryDir);
            }
            if (!Directory.Exists(rootSmylXinyangguanliRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylXinyangguanliRepositoryDir);
            }
            if (!Directory.Exists(rootSmylShimingRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylShimingRepositoryDir);
            }
            if (!Directory.Exists(rootSmylXingweiRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylXingweiRepositoryDir);
            }
            if (!Directory.Exists(rootSmylZongheRepositoryDir))
            {
                Directory.CreateDirectory(rootSmylZongheRepositoryDir);
            }
            if (!Directory.Exists(rootSmylDir))
            {
                Directory.CreateDirectory(rootSmylDir);
            }

            if (!Directory.Exists(rootJwRepositoryDir))
            {
                Directory.CreateDirectory(rootJwRepositoryDir);
            }
            if (!Directory.Exists(rootJwDaogaoRepositoryDir))
            {
                Directory.CreateDirectory(rootJwDaogaoRepositoryDir);
            }
            if (!Directory.Exists(rootJwChuandaoRepositoryDir))
            {
                Directory.CreateDirectory(rootJwChuandaoRepositoryDir);
            }
            if (!Directory.Exists(rootJwXinxinRepositoryDir))
            {
                Directory.CreateDirectory(rootJwXinxinRepositoryDir);
            }
            if (!Directory.Exists(rootJwShouyinRepositoryDir))
            {
                Directory.CreateDirectory(rootJwShouyinRepositoryDir);
            }
            if (!Directory.Exists(rootJwXinyangguanliRepositoryDir))
            {
                Directory.CreateDirectory(rootJwXinyangguanliRepositoryDir);
            }
            if (!Directory.Exists(rootJwShimingRepositoryDir))
            {
                Directory.CreateDirectory(rootJwShimingRepositoryDir);
            }
            if (!Directory.Exists(rootJwXingweiRepositoryDir))
            {
                Directory.CreateDirectory(rootJwXingweiRepositoryDir);
            }
            if (!Directory.Exists(rootJwZongheRepositoryDir))
            {
                Directory.CreateDirectory(rootJwZongheRepositoryDir);
            }
            if (!Directory.Exists(rootJwDir))
            {
                Directory.CreateDirectory(rootJwDir);
            }
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            var date = GetDate(out string dateErr);
            if (date == null)
            {
                this.lbl_status.Text = dateErr;
                return;
            }

            if (!pl_type.Controls.OfType<RadioButton>().Any(r => r.Checked))
            {
                this.lbl_status.Text = "请选择一个类型";
                return;
            }

            if (!pl_direction.Controls.OfType<RadioButton>().Any(r => r.Checked))
            {
                this.lbl_status.Text = "请选择一个方向";
                return;
            }

            var content = this.txt_content.Text
                .Replace("🌈生命语录", "")
                .Replace("💦💦💦经文分享", "")
                .Trim();

            if (content.Length == 0)
            {
                this.lbl_status.Text = "请输入内容";
                return;
            }

            if (this.radio_smyl.Checked)
            {
                #region 单个文件保存

                File.WriteAllText(@$"{rootSmylRepositoryDir}\{date.Value:yy.M.d}.txt", content);

                if (this.radio_daogao.Checked) // 祷告
                {
                    File.WriteAllText(@$"{rootSmylDaogaoRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_chuandao.Checked) // 传道
                {
                    File.WriteAllText(@$"{rootSmylChuandaoRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_xinxin.Checked) // 信心
                {
                    File.WriteAllText(@$"{rootSmylXinxinRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_shouyin.Checked) // 受印
                {
                    File.WriteAllText(@$"{rootSmylShouyinRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_xinyangguanli.Checked) // 信仰管理
                {
                    File.WriteAllText(@$"{rootSmylXinyangguanliRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_shiming.Checked) // 使命
                {
                    File.WriteAllText(@$"{rootSmylShimingRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_xingwei.Checked) // 行为
                {
                    File.WriteAllText(@$"{rootSmylXingweiRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_zonghe.Checked) // 综合
                {
                    File.WriteAllText(@$"{rootSmylZongheRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }

                #endregion

                #region 整体保存

                SaveDocXFromSomeRules(rootSmylRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（全部）");
                SaveDocXFromSomeRules(rootSmylRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（全部）");
                SaveDocXFromSomeRules(rootSmylRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（全部）");
                SaveDocXFromSomeRules(rootSmylRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（全部）");

                if (this.radio_daogao.Checked) // 祷告
                {
                    SaveDocXFromSomeRules(rootSmylDaogaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（祷告）");
                    SaveDocXFromSomeRules(rootSmylDaogaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（祷告）");
                    SaveDocXFromSomeRules(rootSmylDaogaoRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（祷告）");
                    SaveDocXFromSomeRules(rootSmylDaogaoRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（祷告）");
                }
                if (this.radio_chuandao.Checked) // 传道
                {
                    SaveDocXFromSomeRules(rootSmylChuandaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（传道）");
                    SaveDocXFromSomeRules(rootSmylChuandaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（传道）");
                    SaveDocXFromSomeRules(rootSmylChuandaoRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（传道）");
                    SaveDocXFromSomeRules(rootSmylChuandaoRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（传道）");
                }
                if (this.radio_xinxin.Checked) // 信心
                {
                    SaveDocXFromSomeRules(rootSmylXinxinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（信心）");
                    SaveDocXFromSomeRules(rootSmylXinxinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（信心）");
                    SaveDocXFromSomeRules(rootSmylXinxinRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（信心）");
                    SaveDocXFromSomeRules(rootSmylXinxinRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（信心）");
                }
                if (this.radio_shouyin.Checked) // 受印
                {
                    SaveDocXFromSomeRules(rootSmylShouyinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（受印）");
                    SaveDocXFromSomeRules(rootSmylShouyinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（受印）");
                    SaveDocXFromSomeRules(rootSmylShouyinRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（受印）");
                    SaveDocXFromSomeRules(rootSmylShouyinRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（受印）");
                }
                if (this.radio_xinyangguanli.Checked) // 信仰管理
                {
                    SaveDocXFromSomeRules(rootSmylXinyangguanliRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（信仰管理）");
                    SaveDocXFromSomeRules(rootSmylXinyangguanliRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（信仰管理）");
                    SaveDocXFromSomeRules(rootSmylXinyangguanliRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（信仰管理）");
                    SaveDocXFromSomeRules(rootSmylXinyangguanliRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（信仰管理）");
                }
                if (this.radio_shiming.Checked) // 使命
                {
                    SaveDocXFromSomeRules(rootSmylShimingRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（使命）");
                    SaveDocXFromSomeRules(rootSmylShimingRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（使命）");
                    SaveDocXFromSomeRules(rootSmylShimingRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（使命）");
                    SaveDocXFromSomeRules(rootSmylShimingRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（使命）");
                }
                if (this.radio_xingwei.Checked) // 行为
                {
                    SaveDocXFromSomeRules(rootSmylXingweiRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（行为）");
                    SaveDocXFromSomeRules(rootSmylXingweiRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（行为）");
                    SaveDocXFromSomeRules(rootSmylXingweiRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（行为）");
                    SaveDocXFromSomeRules(rootSmylXingweiRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（行为）");
                }
                if (this.radio_zonghe.Checked) // 综合
                {
                    SaveDocXFromSomeRules(rootSmylZongheRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootSmylDir}\{date.Value:yy.MM} 生命语录汇总（综合）");
                    SaveDocXFromSomeRules(rootSmylZongheRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootSmylDir}\{date.Value:yy} 生命语录汇总（综合）");
                    SaveDocXFromSomeRules(rootSmylZongheRepositoryDir, file => true, @$"{rootSmylDir}\生命语录汇总（综合）");
                    SaveDocXFromSomeRules(rootSmylZongheRepositoryDir, file => true, @$"{rootDir}\生命语录汇总（综合）");
                }

                #endregion
            }

            if (this.radio_jwfx.Checked)
            {
                #region 单个文件保存

                File.WriteAllText(@$"{rootJwRepositoryDir}\{date.Value:yy.M.d}.txt", content);

                if (this.radio_daogao.Checked) // 祷告
                {
                    File.WriteAllText(@$"{rootJwDaogaoRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_chuandao.Checked) // 传道
                {
                    File.WriteAllText(@$"{rootJwChuandaoRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_xinxin.Checked) // 信心
                {
                    File.WriteAllText(@$"{rootJwXinxinRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_shouyin.Checked) // 受印
                {
                    File.WriteAllText(@$"{rootJwShouyinRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_xinyangguanli.Checked) // 信仰管理
                {
                    File.WriteAllText(@$"{rootJwXinyangguanliRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_shiming.Checked) // 使命
                {
                    File.WriteAllText(@$"{rootJwShimingRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_xingwei.Checked) // 行为
                {
                    File.WriteAllText(@$"{rootJwXingweiRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }
                if (this.radio_zonghe.Checked) // 综合
                {
                    File.WriteAllText(@$"{rootJwZongheRepositoryDir}\{date.Value:yy.M.d}.txt", content);
                }

                #endregion

                #region 整体保存

                SaveDocXFromSomeRules(rootJwRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（全部）");
                SaveDocXFromSomeRules(rootJwRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（全部）");
                SaveDocXFromSomeRules(rootJwRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（全部）");
                SaveDocXFromSomeRules(rootJwRepositoryDir, file => true, @$"{rootDir}\经文汇总（全部）");

                if (this.radio_daogao.Checked) // 祷告
                {
                    SaveDocXFromSomeRules(rootJwDaogaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（祷告）");
                    SaveDocXFromSomeRules(rootJwDaogaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（祷告）");
                    SaveDocXFromSomeRules(rootJwDaogaoRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（祷告）");
                    SaveDocXFromSomeRules(rootJwDaogaoRepositoryDir, file => true, @$"{rootDir}\经文汇总（祷告）");
                }
                if (this.radio_chuandao.Checked) // 传道
                {
                    SaveDocXFromSomeRules(rootJwChuandaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（传道）");
                    SaveDocXFromSomeRules(rootJwChuandaoRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（传道）");
                    SaveDocXFromSomeRules(rootJwChuandaoRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（传道）");
                    SaveDocXFromSomeRules(rootJwChuandaoRepositoryDir, file => true, @$"{rootDir}\经文汇总（传道）");
                }
                if (this.radio_xinxin.Checked) // 信心
                {
                    SaveDocXFromSomeRules(rootJwXinxinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（信心）");
                    SaveDocXFromSomeRules(rootJwXinxinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（信心）");
                    SaveDocXFromSomeRules(rootJwXinxinRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（信心）");
                    SaveDocXFromSomeRules(rootJwXinxinRepositoryDir, file => true, @$"{rootDir}\经文汇总（信心）");
                }
                if (this.radio_shouyin.Checked) // 受印
                {
                    SaveDocXFromSomeRules(rootJwShouyinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（受印）");
                    SaveDocXFromSomeRules(rootJwShouyinRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（受印）");
                    SaveDocXFromSomeRules(rootJwShouyinRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（受印）");
                    SaveDocXFromSomeRules(rootJwShouyinRepositoryDir, file => true, @$"{rootDir}\经文汇总（受印）");
                }
                if (this.radio_xinyangguanli.Checked) // 信仰管理
                {
                    SaveDocXFromSomeRules(rootJwXinyangguanliRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（信仰管理）");
                    SaveDocXFromSomeRules(rootJwXinyangguanliRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（信仰管理）");
                    SaveDocXFromSomeRules(rootJwXinyangguanliRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（信仰管理）");
                    SaveDocXFromSomeRules(rootJwXinyangguanliRepositoryDir, file => true, @$"{rootDir}\经文汇总（信仰管理）");
                }
                if (this.radio_shiming.Checked) // 使命
                {
                    SaveDocXFromSomeRules(rootJwShimingRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（使命）");
                    SaveDocXFromSomeRules(rootJwShimingRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（使命）");
                    SaveDocXFromSomeRules(rootJwShimingRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（使命）");
                    SaveDocXFromSomeRules(rootJwShimingRepositoryDir, file => true, @$"{rootDir}\经文汇总（使命）");
                }
                if (this.radio_xingwei.Checked) // 行为
                {
                    SaveDocXFromSomeRules(rootJwXingweiRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（行为）");
                    SaveDocXFromSomeRules(rootJwXingweiRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（行为）");
                    SaveDocXFromSomeRules(rootJwXingweiRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（行为）");
                    SaveDocXFromSomeRules(rootJwXingweiRepositoryDir, file => true, @$"{rootDir}\经文汇总（行为）");
                }
                if (this.radio_zonghe.Checked) // 综合
                {
                    SaveDocXFromSomeRules(rootJwZongheRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy.M}"), @$"{rootJwDir}\{date.Value:yy.MM} 经文汇总（综合）");
                    SaveDocXFromSomeRules(rootJwZongheRepositoryDir, file => Path.GetFileName(file).StartsWith($"{date.Value:yy}"), @$"{rootJwDir}\{date.Value:yy} 经文汇总（综合）");
                    SaveDocXFromSomeRules(rootJwZongheRepositoryDir, file => true, @$"{rootJwDir}\经文汇总（综合）");
                    SaveDocXFromSomeRules(rootJwZongheRepositoryDir, file => true, @$"{rootDir}\经文汇总（综合）");
                }

                #endregion
            }

            #region 清空选择

            foreach (var ctl in pl_type.Controls)
            {
                if (ctl is RadioButton radio)
                {
                    radio.Checked = false;
                }
            }

            foreach (var ctl in pl_direction.Controls)
            {
                if (ctl is RadioButton radio)
                {
                    radio.Checked = false;
                }
            }

            this.txt_content.Clear();
            if (this.txt_date.Text.Length > 4)
            {
                this.txt_date.Text = this.txt_date.Text[..4];
            }

            #endregion

            this.lbl_status.Text = $"操作成功_{DateTime.Now:HHmmss}";
        }

        private void btn_clear_Click(object sender, EventArgs e)
        {
            this.txt_date.Clear();
            this.txt_content.Clear();
        }

        private DateTime? GetDate(out string err)
        {
            err = "";
            var dateStr = this.txt_date.Text.Trim();
            if (dateStr.Length == 0)
            {
                return DateTime.Now.Date.AddYears(-1983);
            }
            if (dateStr.Length != 6)
            {
                err = "日期的长度必须为6";
                return null;
            }
            var yearStr = dateStr[..2];
            if (!int.TryParse(yearStr, out int year))
            {
                err = "年份必须是数字";
                return null;
            }
            year += 1983;
            var month = dateStr.Substring(2, 2);
            var day = dateStr.Substring(4, 2);
            if (!DateTime.TryParse($"{year}/{month}/{day}", out DateTime result))
            {
                err = "日期格式错误";
                return null;
            }
            return result.AddYears(-1983);
        }

        private DateTime ParseFileName(string fileName)
        {
            if (DateTime.TryParseExact(fileName, "yy.M.d", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }

            throw new Exception($"{fileName}不是yy.M.d格式按");
        }

        private List<string> GetFilesFromSomeRules(string dir, Func<string, bool> rule)
        {
            var files = Directory.GetFiles(dir)
                .Where(file => rule(file))
                .OrderBy(file => ParseFileName(Path.GetFileNameWithoutExtension(file)))
                .ToList();

            return files;
        }

        private void SaveDocXFromFileList(List<string> files, string fileName)
        {
            var contentList = new List<string>();

            foreach (var file in files)
            {
                contentList.Add($"{Path.GetFileName(file).Replace(".txt", "")}\r\n{File.ReadAllText(file)}");
            }

            using (var document = DocX.Create(fileName, Xceed.Document.NET.DocumentTypes.Document))
            {
                document.InsertParagraph().Append(string.Join("\r\n\r\n", contentList));

                document.Save();
            }
        }

        private void SaveDocXFromSomeRules(string dir, Func<string, bool> rule, string fileName)
        {
            SaveDocXFromFileList(GetFilesFromSomeRules(dir, rule), fileName);
        }

        private void btn_random_Click(object sender, EventArgs e)
        {
            // 获取所有 .txt 文件
            var txtFiles = Directory.GetFiles(rootSmylRepositoryDir, "*.txt");

            if (txtFiles.Length == 0)
            {
                Console.WriteLine("目录下没有txt文件。");
                return;
            }

            // 随机选择一个文件
            var random = new Random();
            string selectedFile = txtFiles[random.Next(txtFiles.Length)];

            // 启动记事本打开文件
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{selectedFile}\"",
                UseShellExecute = true
            });

            Console.WriteLine($"已打开文件: {selectedFile}");
        }
    }
}
