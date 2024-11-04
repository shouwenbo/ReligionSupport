namespace ArticlePromptCrafter
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
            btn_create = new Button();
            statusStrip1 = new StatusStrip();
            lbl_status = new ToolStripStatusLabel();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btn_create
            // 
            btn_create.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            btn_create.Location = new Point(12, 12);
            btn_create.Name = "btn_create";
            btn_create.Size = new Size(539, 51);
            btn_create.TabIndex = 0;
            btn_create.Text = "点此生成";
            btn_create.UseVisualStyleBackColor = true;
            btn_create.Click += btn_create_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lbl_status });
            statusStrip1.Location = new Point(0, 71);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(563, 26);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lbl_status
            // 
            lbl_status.Name = "lbl_status";
            lbl_status.Size = new Size(54, 20);
            lbl_status.Text = "状态栏";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(563, 97);
            Controls.Add(statusStrip1);
            Controls.Add(btn_create);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "随机生成文章洗稿提示词";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_create;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lbl_status;
    }
}
