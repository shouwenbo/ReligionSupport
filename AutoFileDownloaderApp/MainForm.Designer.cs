namespace AutoFileDownloaderApp
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
            list_url = new ListBox();
            lbl_dir = new Label();
            txt_dir = new TextBox();
            btn_select_dir = new Button();
            lbl_title = new Label();
            txt_title = new TextBox();
            btn_download = new Button();
            btn_copy = new Button();
            gpx_single_file = new GroupBox();
            gpx_great_super = new GroupBox();
            btn_great_supper_cd_download = new Button();
            lbl_great_supper_cookie = new Label();
            txt_great_supper_cookie = new TextBox();
            gpx_single_file.SuspendLayout();
            gpx_great_super.SuspendLayout();
            SuspendLayout();
            // 
            // list_url
            // 
            list_url.FormattingEnabled = true;
            list_url.ItemHeight = 17;
            list_url.Location = new Point(15, 91);
            list_url.Margin = new Padding(2, 3, 2, 3);
            list_url.Name = "list_url";
            list_url.Size = new Size(693, 174);
            list_url.TabIndex = 0;
            // 
            // lbl_dir
            // 
            lbl_dir.AutoSize = true;
            lbl_dir.Location = new Point(15, 28);
            lbl_dir.Margin = new Padding(2, 0, 2, 0);
            lbl_dir.Name = "lbl_dir";
            lbl_dir.Size = new Size(68, 17);
            lbl_dir.TabIndex = 1;
            lbl_dir.Text = "文件目录：";
            // 
            // txt_dir
            // 
            txt_dir.Location = new Point(85, 25);
            txt_dir.Margin = new Padding(2, 3, 2, 3);
            txt_dir.Name = "txt_dir";
            txt_dir.Size = new Size(545, 23);
            txt_dir.TabIndex = 2;
            // 
            // btn_select_dir
            // 
            btn_select_dir.Location = new Point(634, 24);
            btn_select_dir.Margin = new Padding(2, 3, 2, 3);
            btn_select_dir.Name = "btn_select_dir";
            btn_select_dir.Size = new Size(73, 25);
            btn_select_dir.TabIndex = 3;
            btn_select_dir.Text = "选择文件夹";
            btn_select_dir.UseVisualStyleBackColor = true;
            btn_select_dir.Click += btn_select_dir_Click;
            // 
            // lbl_title
            // 
            lbl_title.AutoSize = true;
            lbl_title.Location = new Point(39, 60);
            lbl_title.Margin = new Padding(2, 0, 2, 0);
            lbl_title.Name = "lbl_title";
            lbl_title.Size = new Size(44, 17);
            lbl_title.TabIndex = 4;
            lbl_title.Text = "标题：";
            // 
            // txt_title
            // 
            txt_title.Location = new Point(85, 57);
            txt_title.Margin = new Padding(2, 3, 2, 3);
            txt_title.Name = "txt_title";
            txt_title.Size = new Size(237, 23);
            txt_title.TabIndex = 5;
            // 
            // btn_download
            // 
            btn_download.Location = new Point(532, 56);
            btn_download.Margin = new Padding(2, 3, 2, 3);
            btn_download.Name = "btn_download";
            btn_download.Size = new Size(176, 25);
            btn_download.TabIndex = 6;
            btn_download.Text = "开始下载";
            btn_download.UseVisualStyleBackColor = true;
            btn_download.Click += btn_download_Click;
            // 
            // btn_copy
            // 
            btn_copy.Location = new Point(342, 56);
            btn_copy.Margin = new Padding(2, 3, 2, 3);
            btn_copy.Name = "btn_copy";
            btn_copy.Size = new Size(176, 25);
            btn_copy.TabIndex = 7;
            btn_copy.Text = "复制吗哪脚本";
            btn_copy.UseVisualStyleBackColor = true;
            btn_copy.Click += btn_copy_Click;
            // 
            // gpx_single_file
            // 
            gpx_single_file.Controls.Add(lbl_dir);
            gpx_single_file.Controls.Add(btn_copy);
            gpx_single_file.Controls.Add(list_url);
            gpx_single_file.Controls.Add(btn_download);
            gpx_single_file.Controls.Add(txt_dir);
            gpx_single_file.Controls.Add(txt_title);
            gpx_single_file.Controls.Add(btn_select_dir);
            gpx_single_file.Controls.Add(lbl_title);
            gpx_single_file.Location = new Point(12, 12);
            gpx_single_file.Name = "gpx_single_file";
            gpx_single_file.Size = new Size(724, 281);
            gpx_single_file.TabIndex = 8;
            gpx_single_file.TabStop = false;
            gpx_single_file.Text = "单文件下载";
            // 
            // gpx_great_super
            // 
            gpx_great_super.Controls.Add(btn_great_supper_cd_download);
            gpx_great_super.Controls.Add(lbl_great_supper_cookie);
            gpx_great_super.Controls.Add(txt_great_supper_cookie);
            gpx_great_super.Location = new Point(12, 299);
            gpx_great_super.Name = "gpx_great_super";
            gpx_great_super.Size = new Size(724, 90);
            gpx_great_super.TabIndex = 9;
            gpx_great_super.TabStop = false;
            gpx_great_super.Text = "GreatSupper多文件下载";
            // 
            // btn_great_supper_cd_download
            // 
            btn_great_supper_cd_download.Location = new Point(15, 52);
            btn_great_supper_cd_download.Margin = new Padding(2, 3, 2, 3);
            btn_great_supper_cd_download.Name = "btn_great_supper_cd_download";
            btn_great_supper_cd_download.Size = new Size(119, 25);
            btn_great_supper_cd_download.TabIndex = 8;
            btn_great_supper_cd_download.Text = "午间CD批量下载";
            btn_great_supper_cd_download.UseVisualStyleBackColor = true;
            btn_great_supper_cd_download.Click += btn_great_supper_cd_download_Click;
            // 
            // lbl_great_supper_cookie
            // 
            lbl_great_supper_cookie.AutoSize = true;
            lbl_great_supper_cookie.Location = new Point(15, 26);
            lbl_great_supper_cookie.Margin = new Padding(2, 0, 2, 0);
            lbl_great_supper_cookie.Name = "lbl_great_supper_cookie";
            lbl_great_supper_cookie.Size = new Size(61, 17);
            lbl_great_supper_cookie.TabIndex = 8;
            lbl_great_supper_cookie.Text = "Cookie：";
            // 
            // txt_great_supper_cookie
            // 
            txt_great_supper_cookie.Location = new Point(80, 23);
            txt_great_supper_cookie.Margin = new Padding(2, 3, 2, 3);
            txt_great_supper_cookie.Name = "txt_great_supper_cookie";
            txt_great_supper_cookie.Size = new Size(627, 23);
            txt_great_supper_cookie.TabIndex = 9;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(748, 400);
            Controls.Add(gpx_great_super);
            Controls.Add(gpx_single_file);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2, 3, 2, 3);
            Name = "MainForm";
            Text = "文件自动下载器";
            gpx_single_file.ResumeLayout(false);
            gpx_single_file.PerformLayout();
            gpx_great_super.ResumeLayout(false);
            gpx_great_super.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ListBox list_url;
        private Label lbl_dir;
        private TextBox txt_dir;
        private Button btn_select_dir;
        private Label lbl_title;
        private TextBox txt_title;
        private Button btn_download;
        private Button btn_copy;
        private GroupBox gpx_single_file;
        private GroupBox gpx_great_super;
        private Label lbl_great_supper_cookie;
        private TextBox txt_great_supper_cookie;
        private Button btn_great_supper_cd_download;
    }
}
