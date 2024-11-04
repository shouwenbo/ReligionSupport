namespace TextReplacer
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
            gp_replace = new GroupBox();
            ckb_capture = new CheckBox();
            rich_maintext = new RichTextBox();
            ckb_srt = new CheckBox();
            btn_replace = new Button();
            gp_config = new GroupBox();
            btn_add_config = new Button();
            txt_to = new TextBox();
            lbl_to = new Label();
            txt_find = new TextBox();
            lbl_find = new Label();
            btn_show = new Button();
            btn_batch_replace = new Button();
            lbl_batch_progress = new Label();
            gp_replace.SuspendLayout();
            gp_config.SuspendLayout();
            SuspendLayout();
            // 
            // gp_replace
            // 
            gp_replace.Controls.Add(ckb_capture);
            gp_replace.Controls.Add(rich_maintext);
            gp_replace.Controls.Add(ckb_srt);
            gp_replace.Controls.Add(btn_replace);
            gp_replace.Location = new Point(12, 12);
            gp_replace.Name = "gp_replace";
            gp_replace.Size = new Size(222, 185);
            gp_replace.TabIndex = 0;
            gp_replace.TabStop = false;
            gp_replace.Text = "文本替换";
            // 
            // ckb_capture
            // 
            ckb_capture.AutoSize = true;
            ckb_capture.Location = new Point(90, 49);
            ckb_capture.Name = "ckb_capture";
            ckb_capture.Size = new Size(75, 21);
            ckb_capture.TabIndex = 4;
            ckb_capture.Text = "截图专用";
            ckb_capture.UseVisualStyleBackColor = true;
            // 
            // rich_maintext
            // 
            rich_maintext.Location = new Point(9, 74);
            rich_maintext.Name = "rich_maintext";
            rich_maintext.Size = new Size(207, 105);
            rich_maintext.TabIndex = 3;
            rich_maintext.Text = "";
            // 
            // ckb_srt
            // 
            ckb_srt.AutoSize = true;
            ckb_srt.Location = new Point(9, 49);
            ckb_srt.Name = "ckb_srt";
            ckb_srt.Size = new Size(75, 21);
            ckb_srt.TabIndex = 2;
            ckb_srt.Text = "处理字幕";
            ckb_srt.UseVisualStyleBackColor = true;
            // 
            // btn_replace
            // 
            btn_replace.Location = new Point(6, 22);
            btn_replace.Name = "btn_replace";
            btn_replace.Size = new Size(210, 23);
            btn_replace.TabIndex = 1;
            btn_replace.Text = "一键替换";
            btn_replace.UseVisualStyleBackColor = true;
            btn_replace.Click += btn_replace_Click;
            // 
            // gp_config
            // 
            gp_config.Controls.Add(btn_add_config);
            gp_config.Controls.Add(txt_to);
            gp_config.Controls.Add(lbl_to);
            gp_config.Controls.Add(txt_find);
            gp_config.Controls.Add(lbl_find);
            gp_config.Controls.Add(btn_show);
            gp_config.Location = new Point(248, 12);
            gp_config.Name = "gp_config";
            gp_config.Size = new Size(222, 185);
            gp_config.TabIndex = 1;
            gp_config.TabStop = false;
            gp_config.Text = "替换配置";
            // 
            // btn_add_config
            // 
            btn_add_config.Location = new Point(6, 156);
            btn_add_config.Name = "btn_add_config";
            btn_add_config.Size = new Size(210, 23);
            btn_add_config.TabIndex = 5;
            btn_add_config.Text = "添加替换配置";
            btn_add_config.UseVisualStyleBackColor = true;
            btn_add_config.Click += btn_add_config_Click;
            // 
            // txt_to
            // 
            txt_to.Location = new Point(6, 125);
            txt_to.Name = "txt_to";
            txt_to.Size = new Size(210, 23);
            txt_to.TabIndex = 4;
            // 
            // lbl_to
            // 
            lbl_to.AutoSize = true;
            lbl_to.Location = new Point(6, 105);
            lbl_to.Name = "lbl_to";
            lbl_to.Size = new Size(56, 17);
            lbl_to.TabIndex = 3;
            lbl_to.Text = "替换为：";
            // 
            // txt_find
            // 
            txt_find.Location = new Point(6, 74);
            txt_find.Name = "txt_find";
            txt_find.Size = new Size(210, 23);
            txt_find.TabIndex = 2;
            // 
            // lbl_find
            // 
            lbl_find.AutoSize = true;
            lbl_find.Location = new Point(6, 54);
            lbl_find.Name = "lbl_find";
            lbl_find.Size = new Size(68, 17);
            lbl_find.TabIndex = 1;
            lbl_find.Text = "查找内容：";
            // 
            // btn_show
            // 
            btn_show.Location = new Point(6, 22);
            btn_show.Name = "btn_show";
            btn_show.Size = new Size(210, 23);
            btn_show.TabIndex = 0;
            btn_show.Text = "查看配置文件";
            btn_show.UseVisualStyleBackColor = true;
            btn_show.Click += btn_show_Click;
            // 
            // btn_batch_replace
            // 
            btn_batch_replace.Location = new Point(12, 203);
            btn_batch_replace.Name = "btn_batch_replace";
            btn_batch_replace.Size = new Size(222, 23);
            btn_batch_replace.TabIndex = 4;
            btn_batch_replace.Text = "批量替换";
            btn_batch_replace.UseVisualStyleBackColor = true;
            btn_batch_replace.Click += btn_batch_replace_Click;
            // 
            // lbl_batch_progress
            // 
            lbl_batch_progress.AutoSize = true;
            lbl_batch_progress.Location = new Point(248, 206);
            lbl_batch_progress.Name = "lbl_batch_progress";
            lbl_batch_progress.Size = new Size(80, 17);
            lbl_batch_progress.TabIndex = 5;
            lbl_batch_progress.Text = "批量替换进度";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(482, 236);
            Controls.Add(lbl_batch_progress);
            Controls.Add(btn_batch_replace);
            Controls.Add(gp_config);
            Controls.Add(gp_replace);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            Text = "文本替换器";
            Load += MainForm_Load;
            gp_replace.ResumeLayout(false);
            gp_replace.PerformLayout();
            gp_config.ResumeLayout(false);
            gp_config.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox gp_replace;
        private GroupBox gp_config;
        private Button btn_replace;
        private Button btn_show;
        private Label lbl_find;
        private TextBox txt_find;
        private Button btn_add_config;
        private TextBox txt_to;
        private Label lbl_to;
        private CheckBox ckb_srt;
        private RichTextBox rich_maintext;
        private Button btn_batch_replace;
        private Label lbl_batch_progress;
        private CheckBox ckb_capture;
    }
}
