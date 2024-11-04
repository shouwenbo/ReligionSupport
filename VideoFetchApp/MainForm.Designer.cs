namespace VideoFetchApp
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            lbl_link = new Label();
            lbl_dir = new Label();
            txt_dir = new TextBox();
            btn_select_dir = new Button();
            txt_video_link = new TextBox();
            btn_download = new Button();
            gpx_single = new GroupBox();
            gpx_multi = new GroupBox();
            label1 = new Label();
            btn_show_log = new Button();
            gpx_single.SuspendLayout();
            gpx_multi.SuspendLayout();
            SuspendLayout();
            // 
            // lbl_link
            // 
            lbl_link.AutoSize = true;
            lbl_link.Location = new Point(5, 25);
            lbl_link.Name = "lbl_link";
            lbl_link.Size = new Size(68, 17);
            lbl_link.TabIndex = 0;
            lbl_link.Text = "视频链接：";
            // 
            // lbl_dir
            // 
            lbl_dir.AutoSize = true;
            lbl_dir.Location = new Point(5, 21);
            lbl_dir.Margin = new Padding(2, 0, 2, 0);
            lbl_dir.Name = "lbl_dir";
            lbl_dir.Size = new Size(68, 17);
            lbl_dir.TabIndex = 4;
            lbl_dir.Text = "文件目录：";
            // 
            // txt_dir
            // 
            txt_dir.Location = new Point(75, 18);
            txt_dir.Margin = new Padding(2, 3, 2, 3);
            txt_dir.Name = "txt_dir";
            txt_dir.Size = new Size(545, 23);
            txt_dir.TabIndex = 5;
            // 
            // btn_select_dir
            // 
            btn_select_dir.Location = new Point(624, 17);
            btn_select_dir.Margin = new Padding(2, 3, 2, 3);
            btn_select_dir.Name = "btn_select_dir";
            btn_select_dir.Size = new Size(73, 25);
            btn_select_dir.TabIndex = 6;
            btn_select_dir.Text = "选择文件夹";
            btn_select_dir.UseVisualStyleBackColor = true;
            btn_select_dir.Click += btn_select_dir_Click;
            // 
            // txt_video_link
            // 
            txt_video_link.Location = new Point(75, 22);
            txt_video_link.Margin = new Padding(2, 3, 2, 3);
            txt_video_link.Name = "txt_video_link";
            txt_video_link.Size = new Size(620, 23);
            txt_video_link.TabIndex = 7;
            // 
            // btn_download
            // 
            btn_download.Location = new Point(624, 51);
            btn_download.Name = "btn_download";
            btn_download.Size = new Size(73, 25);
            btn_download.TabIndex = 8;
            btn_download.Text = "开始下载";
            btn_download.UseVisualStyleBackColor = true;
            btn_download.Click += btn_download_Click;
            // 
            // gpx_single
            // 
            gpx_single.Controls.Add(lbl_link);
            gpx_single.Controls.Add(txt_video_link);
            gpx_single.Controls.Add(btn_download);
            gpx_single.Location = new Point(12, 12);
            gpx_single.Name = "gpx_single";
            gpx_single.Size = new Size(708, 86);
            gpx_single.TabIndex = 10;
            gpx_single.TabStop = false;
            gpx_single.Text = "单文件下载（仅支持Youtube）";
            // 
            // gpx_multi
            // 
            gpx_multi.Controls.Add(label1);
            gpx_multi.Controls.Add(lbl_dir);
            gpx_multi.Controls.Add(btn_select_dir);
            gpx_multi.Controls.Add(txt_dir);
            gpx_multi.Location = new Point(12, 104);
            gpx_multi.Name = "gpx_multi";
            gpx_multi.Size = new Size(708, 100);
            gpx_multi.TabIndex = 11;
            gpx_multi.TabStop = false;
            gpx_multi.Text = "播放列表下载";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(311, 58);
            label1.Name = "label1";
            label1.Size = new Size(80, 17);
            label1.TabIndex = 7;
            label1.Text = "开发中............";
            // 
            // btn_show_log
            // 
            btn_show_log.Location = new Point(12, 210);
            btn_show_log.Name = "btn_show_log";
            btn_show_log.Size = new Size(73, 25);
            btn_show_log.TabIndex = 9;
            btn_show_log.Text = "查看日志";
            btn_show_log.UseVisualStyleBackColor = true;
            btn_show_log.Click += btn_show_log_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(735, 244);
            Controls.Add(btn_show_log);
            Controls.Add(gpx_multi);
            Controls.Add(gpx_single);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "在线视频下载器";
            gpx_single.ResumeLayout(false);
            gpx_single.PerformLayout();
            gpx_multi.ResumeLayout(false);
            gpx_multi.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lbl_link;
        private Label lbl_dir;
        private TextBox txt_dir;
        private Button btn_select_dir;
        private TextBox txt_video_link;
        private Button btn_download;
        private GroupBox gpx_single;
        private GroupBox gpx_multi;
        private Label label1;
        private Button btn_show_log;
    }
}
