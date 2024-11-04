namespace VideoFetchApp
{
    partial class YoutubeDownloader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(YoutubeDownloader));
            ss_tools = new StatusStrip();
            lbl_status = new ToolStripStatusLabel();
            bar_download = new ProgressBar();
            label1 = new Label();
            ss_tools.SuspendLayout();
            SuspendLayout();
            // 
            // ss_tools
            // 
            ss_tools.Items.AddRange(new ToolStripItem[] { lbl_status });
            ss_tools.Location = new Point(0, 70);
            ss_tools.Name = "ss_tools";
            ss_tools.Size = new Size(674, 22);
            ss_tools.TabIndex = 0;
            ss_tools.Text = "statusStrip1";
            // 
            // lbl_status
            // 
            lbl_status.Name = "lbl_status";
            lbl_status.Size = new Size(44, 17);
            lbl_status.Text = "状态栏";
            // 
            // bar_download
            // 
            bar_download.Location = new Point(12, 31);
            bar_download.Name = "bar_download";
            bar_download.Size = new Size(650, 23);
            bar_download.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 7);
            label1.Name = "label1";
            label1.Size = new Size(56, 17);
            label1.TabIndex = 2;
            label1.Text = "下载文件";
            // 
            // YoutubeDownloader
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(674, 92);
            Controls.Add(label1);
            Controls.Add(bar_download);
            Controls.Add(ss_tools);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "YoutubeDownloader";
            Text = "Youtube视频下载器";
            ss_tools.ResumeLayout(false);
            ss_tools.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip ss_tools;
        private ToolStripStatusLabel lbl_status;
        private ProgressBar bar_download;
        private Label label1;
    }
}