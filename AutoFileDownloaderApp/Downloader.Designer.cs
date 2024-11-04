namespace AutoFileDownloaderApp
{
    partial class Downloader
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Downloader));
            lbl_progress = new Label();
            statusStrip1 = new StatusStrip();
            lbl_status = new ToolStripStatusLabel();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lbl_progress
            // 
            lbl_progress.AutoSize = true;
            lbl_progress.Location = new Point(12, 9);
            lbl_progress.Name = "lbl_progress";
            lbl_progress.Size = new Size(69, 20);
            lbl_progress.TabIndex = 0;
            lbl_progress.Text = "下载进度";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lbl_status });
            statusStrip1.Location = new Point(0, 36);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(493, 26);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lbl_status
            // 
            lbl_status.Name = "lbl_status";
            lbl_status.Size = new Size(54, 20);
            lbl_status.Text = "状态栏";
            // 
            // Downloader
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(493, 62);
            Controls.Add(statusStrip1);
            Controls.Add(lbl_progress);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Downloader";
            Text = "Downloader";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbl_progress;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lbl_status;
    }
}