namespace VerseNotesMaker
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
            lbl_title = new Label();
            txt_title = new TextBox();
            lbl_desc = new Label();
            txt_desc = new TextBox();
            lbl_chapter = new Label();
            txt_chapter = new TextBox();
            lbl_verse = new Label();
            txt_verse = new TextBox();
            lbl_content = new Label();
            txt_content = new TextBox();
            lbl_tag = new Label();
            txt_tag1 = new TextBox();
            txt_tag2 = new TextBox();
            txt_tag3 = new TextBox();
            txt_tag4 = new TextBox();
            txt_tag5 = new TextBox();
            lbl_config = new Label();
            cbx_read = new CheckBox();
            cbx_public_account = new CheckBox();
            btn_maker = new Button();
            statusStrip1 = new StatusStrip();
            lbl_status = new ToolStripStatusLabel();
            cbx_cyx = new CheckBox();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lbl_title
            // 
            lbl_title.AutoSize = true;
            lbl_title.Location = new Point(15, 11);
            lbl_title.Margin = new Padding(4, 0, 4, 0);
            lbl_title.Name = "lbl_title";
            lbl_title.Size = new Size(54, 20);
            lbl_title.TabIndex = 0;
            lbl_title.Text = "标题：";
            // 
            // txt_title
            // 
            txt_title.Location = new Point(80, 7);
            txt_title.Margin = new Padding(4, 4, 4, 4);
            txt_title.Name = "txt_title";
            txt_title.Size = new Size(475, 27);
            txt_title.TabIndex = 1;
            // 
            // lbl_desc
            // 
            lbl_desc.AutoSize = true;
            lbl_desc.Location = new Point(15, 51);
            lbl_desc.Margin = new Padding(4, 0, 4, 0);
            lbl_desc.Name = "lbl_desc";
            lbl_desc.Size = new Size(54, 20);
            lbl_desc.TabIndex = 2;
            lbl_desc.Text = "简介：";
            // 
            // txt_desc
            // 
            txt_desc.Location = new Point(80, 47);
            txt_desc.Margin = new Padding(4, 4, 4, 4);
            txt_desc.Multiline = true;
            txt_desc.Name = "txt_desc";
            txt_desc.Size = new Size(475, 60);
            txt_desc.TabIndex = 3;
            // 
            // lbl_chapter
            // 
            lbl_chapter.AutoSize = true;
            lbl_chapter.Location = new Point(17, 125);
            lbl_chapter.Margin = new Padding(4, 0, 4, 0);
            lbl_chapter.Name = "lbl_chapter";
            lbl_chapter.Size = new Size(54, 20);
            lbl_chapter.TabIndex = 4;
            lbl_chapter.Text = "章节：";
            // 
            // txt_chapter
            // 
            txt_chapter.Location = new Point(80, 121);
            txt_chapter.Margin = new Padding(4, 4, 4, 4);
            txt_chapter.Name = "txt_chapter";
            txt_chapter.Size = new Size(475, 27);
            txt_chapter.TabIndex = 5;
            txt_chapter.KeyDown += txt_chapter_KeyDown;
            // 
            // lbl_verse
            // 
            lbl_verse.AutoSize = true;
            lbl_verse.Location = new Point(18, 169);
            lbl_verse.Margin = new Padding(4, 0, 4, 0);
            lbl_verse.Name = "lbl_verse";
            lbl_verse.Size = new Size(54, 20);
            lbl_verse.TabIndex = 6;
            lbl_verse.Text = "经文：";
            // 
            // txt_verse
            // 
            txt_verse.Location = new Point(80, 166);
            txt_verse.Margin = new Padding(4, 4, 4, 4);
            txt_verse.Multiline = true;
            txt_verse.Name = "txt_verse";
            txt_verse.Size = new Size(475, 60);
            txt_verse.TabIndex = 7;
            // 
            // lbl_content
            // 
            lbl_content.AutoSize = true;
            lbl_content.Location = new Point(18, 249);
            lbl_content.Margin = new Padding(4, 0, 4, 0);
            lbl_content.Name = "lbl_content";
            lbl_content.Size = new Size(54, 20);
            lbl_content.TabIndex = 8;
            lbl_content.Text = "内容：";
            // 
            // txt_content
            // 
            txt_content.Location = new Point(80, 246);
            txt_content.Margin = new Padding(4, 4, 4, 4);
            txt_content.Multiline = true;
            txt_content.Name = "txt_content";
            txt_content.Size = new Size(475, 211);
            txt_content.TabIndex = 9;
            // 
            // lbl_tag
            // 
            lbl_tag.AutoSize = true;
            lbl_tag.Location = new Point(15, 476);
            lbl_tag.Margin = new Padding(4, 0, 4, 0);
            lbl_tag.Name = "lbl_tag";
            lbl_tag.Size = new Size(54, 20);
            lbl_tag.TabIndex = 10;
            lbl_tag.Text = "标签：";
            // 
            // txt_tag1
            // 
            txt_tag1.Location = new Point(80, 473);
            txt_tag1.Margin = new Padding(4, 4, 4, 4);
            txt_tag1.Name = "txt_tag1";
            txt_tag1.Size = new Size(71, 27);
            txt_tag1.TabIndex = 11;
            // 
            // txt_tag2
            // 
            txt_tag2.Location = new Point(180, 473);
            txt_tag2.Margin = new Padding(4, 4, 4, 4);
            txt_tag2.Name = "txt_tag2";
            txt_tag2.Size = new Size(71, 27);
            txt_tag2.TabIndex = 12;
            // 
            // txt_tag3
            // 
            txt_tag3.Location = new Point(280, 473);
            txt_tag3.Margin = new Padding(4, 4, 4, 4);
            txt_tag3.Name = "txt_tag3";
            txt_tag3.Size = new Size(71, 27);
            txt_tag3.TabIndex = 13;
            // 
            // txt_tag4
            // 
            txt_tag4.Location = new Point(381, 473);
            txt_tag4.Margin = new Padding(4, 4, 4, 4);
            txt_tag4.Name = "txt_tag4";
            txt_tag4.Size = new Size(71, 27);
            txt_tag4.TabIndex = 14;
            // 
            // txt_tag5
            // 
            txt_tag5.Location = new Point(481, 473);
            txt_tag5.Margin = new Padding(4, 4, 4, 4);
            txt_tag5.Name = "txt_tag5";
            txt_tag5.Size = new Size(71, 27);
            txt_tag5.TabIndex = 15;
            // 
            // lbl_config
            // 
            lbl_config.AutoSize = true;
            lbl_config.Location = new Point(15, 518);
            lbl_config.Margin = new Padding(4, 0, 4, 0);
            lbl_config.Name = "lbl_config";
            lbl_config.Size = new Size(54, 20);
            lbl_config.TabIndex = 16;
            lbl_config.Text = "选项：";
            // 
            // cbx_read
            // 
            cbx_read.AutoSize = true;
            cbx_read.Checked = true;
            cbx_read.CheckState = CheckState.Checked;
            cbx_read.Location = new Point(80, 515);
            cbx_read.Margin = new Padding(4, 4, 4, 4);
            cbx_read.Name = "cbx_read";
            cbx_read.Size = new Size(91, 24);
            cbx_read.TabIndex = 17;
            cbx_read.Text = "读经感悟";
            cbx_read.UseVisualStyleBackColor = true;
            // 
            // cbx_public_account
            // 
            cbx_public_account.AutoSize = true;
            cbx_public_account.Checked = true;
            cbx_public_account.CheckState = CheckState.Checked;
            cbx_public_account.Location = new Point(180, 515);
            cbx_public_account.Margin = new Padding(4, 4, 4, 4);
            cbx_public_account.Name = "cbx_public_account";
            cbx_public_account.Size = new Size(106, 24);
            cbx_public_account.TabIndex = 18;
            cbx_public_account.Text = "公众号文案";
            cbx_public_account.UseVisualStyleBackColor = true;
            // 
            // btn_maker
            // 
            btn_maker.Location = new Point(421, 518);
            btn_maker.Margin = new Padding(4, 4, 4, 4);
            btn_maker.Name = "btn_maker";
            btn_maker.Size = new Size(132, 46);
            btn_maker.TabIndex = 19;
            btn_maker.Text = "生成";
            btn_maker.UseVisualStyleBackColor = true;
            btn_maker.Click += btn_maker_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lbl_status });
            statusStrip1.Location = new Point(0, 568);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 18, 0);
            statusStrip1.Size = new Size(571, 26);
            statusStrip1.TabIndex = 20;
            statusStrip1.Text = "statusStrip1";
            // 
            // lbl_status
            // 
            lbl_status.Name = "lbl_status";
            lbl_status.Size = new Size(189, 20);
            lbl_status.Text = "欢迎使用经文笔记导出工具";
            // 
            // cbx_cyx
            // 
            cbx_cyx.AutoSize = true;
            cbx_cyx.Location = new Point(294, 515);
            cbx_cyx.Margin = new Padding(4);
            cbx_cyx.Name = "cbx_cyx";
            cbx_cyx.Size = new Size(91, 24);
            cbx_cyx.TabIndex = 21;
            cbx_cyx.Text = "湘湘模式";
            cbx_cyx.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(571, 594);
            Controls.Add(cbx_cyx);
            Controls.Add(statusStrip1);
            Controls.Add(btn_maker);
            Controls.Add(cbx_public_account);
            Controls.Add(cbx_read);
            Controls.Add(lbl_config);
            Controls.Add(txt_tag5);
            Controls.Add(txt_tag4);
            Controls.Add(txt_tag3);
            Controls.Add(txt_tag2);
            Controls.Add(txt_tag1);
            Controls.Add(lbl_tag);
            Controls.Add(txt_content);
            Controls.Add(lbl_content);
            Controls.Add(txt_verse);
            Controls.Add(lbl_verse);
            Controls.Add(txt_chapter);
            Controls.Add(lbl_chapter);
            Controls.Add(txt_desc);
            Controls.Add(lbl_desc);
            Controls.Add(txt_title);
            Controls.Add(lbl_title);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 4, 4, 4);
            Name = "MainForm";
            Text = "经文笔记导出工具";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbl_title;
        private TextBox txt_title;
        private Label lbl_desc;
        private TextBox txt_desc;
        private Label lbl_chapter;
        private TextBox txt_chapter;
        private Label lbl_verse;
        private TextBox txt_verse;
        private Label lbl_content;
        private TextBox txt_content;
        private Label lbl_tag;
        private TextBox txt_tag1;
        private TextBox txt_tag2;
        private TextBox txt_tag3;
        private TextBox txt_tag4;
        private TextBox txt_tag5;
        private Label lbl_config;
        private CheckBox cbx_read;
        private CheckBox cbx_public_account;
        private Button btn_maker;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lbl_status;
        private CheckBox cbx_cyx;
    }
}
