namespace BibleVerseLookupApp
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
            statusStrip1 = new StatusStrip();
            lbl_msg = new ToolStripStatusLabel();
            panel1 = new Panel();
            rb_type4 = new RadioButton();
            rb_type3 = new RadioButton();
            rb_type2 = new RadioButton();
            rb_type1 = new RadioButton();
            lbl_helper = new Label();
            lbl_help = new Label();
            statusStrip1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lbl_msg });
            statusStrip1.Location = new Point(0, 77);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 9, 0);
            statusStrip1.Size = new Size(338, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lbl_msg
            // 
            lbl_msg.Name = "lbl_msg";
            lbl_msg.Size = new Size(56, 17);
            lbl_msg.Text = "状态提示";
            // 
            // panel1
            // 
            panel1.Controls.Add(rb_type4);
            panel1.Controls.Add(rb_type3);
            panel1.Controls.Add(rb_type2);
            panel1.Controls.Add(rb_type1);
            panel1.Location = new Point(8, 8);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(319, 39);
            panel1.TabIndex = 2;
            // 
            // rb_type4
            // 
            rb_type4.AutoSize = true;
            rb_type4.Location = new Point(246, 8);
            rb_type4.Margin = new Padding(2);
            rb_type4.Name = "rb_type4";
            rb_type4.Size = new Size(57, 21);
            rb_type4.TabIndex = 3;
            rb_type4.Text = "模式4";
            rb_type4.UseVisualStyleBackColor = true;
            // 
            // rb_type3
            // 
            rb_type3.AutoSize = true;
            rb_type3.Location = new Point(167, 8);
            rb_type3.Margin = new Padding(2);
            rb_type3.Name = "rb_type3";
            rb_type3.Size = new Size(57, 21);
            rb_type3.TabIndex = 2;
            rb_type3.Text = "模式3";
            rb_type3.UseVisualStyleBackColor = true;
            // 
            // rb_type2
            // 
            rb_type2.AutoSize = true;
            rb_type2.Location = new Point(88, 8);
            rb_type2.Margin = new Padding(2);
            rb_type2.Name = "rb_type2";
            rb_type2.Size = new Size(57, 21);
            rb_type2.TabIndex = 1;
            rb_type2.Text = "模式2";
            rb_type2.UseVisualStyleBackColor = true;
            // 
            // rb_type1
            // 
            rb_type1.AutoSize = true;
            rb_type1.Checked = true;
            rb_type1.Location = new Point(16, 8);
            rb_type1.Margin = new Padding(2);
            rb_type1.Name = "rb_type1";
            rb_type1.Size = new Size(57, 21);
            rb_type1.TabIndex = 0;
            rb_type1.TabStop = true;
            rb_type1.Text = "模式1";
            rb_type1.UseVisualStyleBackColor = true;
            // 
            // lbl_helper
            // 
            lbl_helper.AutoSize = true;
            lbl_helper.Location = new Point(1, 54);
            lbl_helper.Margin = new Padding(2, 0, 2, 0);
            lbl_helper.Name = "lbl_helper";
            lbl_helper.Size = new Size(259, 17);
            lbl_helper.TabIndex = 3;
            lbl_helper.Text = "小贴士：复制章节后，按下 ctrl+空格 生成结果";
            // 
            // lbl_help
            // 
            lbl_help.AutoSize = true;
            lbl_help.Cursor = Cursors.Hand;
            lbl_help.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Underline, GraphicsUnit.Point);
            lbl_help.ForeColor = Color.Blue;
            lbl_help.Location = new Point(294, 54);
            lbl_help.Name = "lbl_help";
            lbl_help.Size = new Size(32, 17);
            lbl_help.TabIndex = 4;
            lbl_help.Text = "帮助";
            lbl_help.Click += lbl_help_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(338, 99);
            Controls.Add(lbl_help);
            Controls.Add(lbl_helper);
            Controls.Add(panel1);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            MaximizeBox = false;
            Name = "MainForm";
            Text = "圣经章节查询（版本1.0.1）";
            FormClosing += MainForm_FormClosing;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lbl_msg;
        private Panel panel1;
        private RadioButton rb_type1;
        private RadioButton rb_type2;
        private Label lbl_helper;
        private RadioButton rb_type3;
        private RadioButton rb_type4;
        private Label lbl_help;
    }
}
