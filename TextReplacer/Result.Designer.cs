namespace TextReplacer
{
    partial class Result
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Result));
            btn_copy = new Button();
            list_replace = new ListBox();
            rich_result = new RichTextBox();
            SuspendLayout();
            // 
            // btn_copy
            // 
            btn_copy.Location = new Point(12, 12);
            btn_copy.Name = "btn_copy";
            btn_copy.Size = new Size(321, 23);
            btn_copy.TabIndex = 0;
            btn_copy.Text = "复制到剪贴板";
            btn_copy.UseVisualStyleBackColor = true;
            btn_copy.Click += btn_copy_Click;
            // 
            // list_replace
            // 
            list_replace.FormattingEnabled = true;
            list_replace.ItemHeight = 17;
            list_replace.Location = new Point(342, 12);
            list_replace.Name = "list_replace";
            list_replace.Size = new Size(171, 191);
            list_replace.TabIndex = 2;
            // 
            // rich_result
            // 
            rich_result.Location = new Point(12, 41);
            rich_result.Name = "rich_result";
            rich_result.Size = new Size(321, 162);
            rich_result.TabIndex = 3;
            rich_result.Text = "";
            // 
            // Result
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(522, 209);
            Controls.Add(rich_result);
            Controls.Add(list_replace);
            Controls.Add(btn_copy);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Result";
            Text = "结果";
            ResumeLayout(false);
        }

        #endregion

        private Button btn_copy;
        private ListBox list_replace;
        private RichTextBox rich_result;
    }
}