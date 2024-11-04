namespace LifeQuotesRecorder
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
            txt_date = new TextBox();
            label1 = new Label();
            label3 = new Label();
            label2 = new Label();
            txt_content = new TextBox();
            btn_ok = new Button();
            statusStrip1 = new StatusStrip();
            lbl_status = new ToolStripStatusLabel();
            btn_clear = new Button();
            label4 = new Label();
            label5 = new Label();
            pl_type = new Panel();
            radio_wz = new RadioButton();
            radio_jwfx = new RadioButton();
            radio_smyl = new RadioButton();
            pl_direction = new Panel();
            radio_zonghe = new RadioButton();
            radio_xingwei = new RadioButton();
            radio_shiming = new RadioButton();
            radio_xinyangguanli = new RadioButton();
            radio_shouyin = new RadioButton();
            radio_xinxin = new RadioButton();
            radio_chuandao = new RadioButton();
            radio_daogao = new RadioButton();
            btn_random = new Button();
            statusStrip1.SuspendLayout();
            pl_type.SuspendLayout();
            pl_direction.SuspendLayout();
            SuspendLayout();
            // 
            // txt_date
            // 
            txt_date.Location = new Point(62, 6);
            txt_date.Name = "txt_date";
            txt_date.Size = new Size(100, 23);
            txt_date.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 9);
            label1.Name = "label1";
            label1.Size = new Size(44, 17);
            label1.TabIndex = 1;
            label1.Text = "日期：";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(168, 9);
            label3.Name = "label3";
            label3.Size = new Size(128, 17);
            label3.TabIndex = 3;
            label3.Text = "（不填则默认为今天）";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(13, 117);
            label2.Name = "label2";
            label2.Size = new Size(44, 17);
            label2.TabIndex = 4;
            label2.Text = "内容：";
            // 
            // txt_content
            // 
            txt_content.Location = new Point(62, 115);
            txt_content.Multiline = true;
            txt_content.Name = "txt_content";
            txt_content.Size = new Size(441, 140);
            txt_content.TabIndex = 5;
            // 
            // btn_ok
            // 
            btn_ok.Location = new Point(428, 261);
            btn_ok.Name = "btn_ok";
            btn_ok.Size = new Size(75, 26);
            btn_ok.TabIndex = 6;
            btn_ok.Text = "确认";
            btn_ok.UseVisualStyleBackColor = true;
            btn_ok.Click += btn_ok_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lbl_status });
            statusStrip1.Location = new Point(0, 294);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(515, 22);
            statusStrip1.TabIndex = 7;
            statusStrip1.Text = "statusStrip1";
            // 
            // lbl_status
            // 
            lbl_status.Name = "lbl_status";
            lbl_status.Size = new Size(92, 17);
            lbl_status.Text = "生命语录记录器";
            // 
            // btn_clear
            // 
            btn_clear.Location = new Point(334, 261);
            btn_clear.Name = "btn_clear";
            btn_clear.Size = new Size(75, 26);
            btn_clear.TabIndex = 8;
            btn_clear.Text = "清空";
            btn_clear.UseVisualStyleBackColor = true;
            btn_clear.Click += btn_clear_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(13, 45);
            label4.Name = "label4";
            label4.Size = new Size(44, 17);
            label4.TabIndex = 9;
            label4.Text = "类型：";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(13, 81);
            label5.Name = "label5";
            label5.Size = new Size(44, 17);
            label5.TabIndex = 10;
            label5.Text = "方向：";
            // 
            // pl_type
            // 
            pl_type.Controls.Add(radio_wz);
            pl_type.Controls.Add(radio_jwfx);
            pl_type.Controls.Add(radio_smyl);
            pl_type.Location = new Point(62, 35);
            pl_type.Name = "pl_type";
            pl_type.Size = new Size(442, 38);
            pl_type.TabIndex = 11;
            // 
            // radio_wz
            // 
            radio_wz.AutoSize = true;
            radio_wz.Location = new Point(163, 8);
            radio_wz.Name = "radio_wz";
            radio_wz.Size = new Size(50, 21);
            radio_wz.TabIndex = 2;
            radio_wz.Text = "文章";
            radio_wz.UseVisualStyleBackColor = true;
            // 
            // radio_jwfx
            // 
            radio_jwfx.AutoSize = true;
            radio_jwfx.Location = new Point(83, 8);
            radio_jwfx.Name = "radio_jwfx";
            radio_jwfx.Size = new Size(74, 21);
            radio_jwfx.TabIndex = 1;
            radio_jwfx.Text = "经文分享";
            radio_jwfx.UseVisualStyleBackColor = true;
            // 
            // radio_smyl
            // 
            radio_smyl.AutoSize = true;
            radio_smyl.Location = new Point(3, 8);
            radio_smyl.Name = "radio_smyl";
            radio_smyl.Size = new Size(74, 21);
            radio_smyl.TabIndex = 0;
            radio_smyl.Text = "生命语录";
            radio_smyl.UseVisualStyleBackColor = true;
            // 
            // pl_direction
            // 
            pl_direction.Controls.Add(radio_zonghe);
            pl_direction.Controls.Add(radio_xingwei);
            pl_direction.Controls.Add(radio_shiming);
            pl_direction.Controls.Add(radio_xinyangguanli);
            pl_direction.Controls.Add(radio_shouyin);
            pl_direction.Controls.Add(radio_xinxin);
            pl_direction.Controls.Add(radio_chuandao);
            pl_direction.Controls.Add(radio_daogao);
            pl_direction.Location = new Point(62, 71);
            pl_direction.Name = "pl_direction";
            pl_direction.Size = new Size(442, 38);
            pl_direction.TabIndex = 12;
            // 
            // radio_zonghe
            // 
            radio_zonghe.AutoSize = true;
            radio_zonghe.Location = new Point(391, 8);
            radio_zonghe.Name = "radio_zonghe";
            radio_zonghe.Size = new Size(50, 21);
            radio_zonghe.TabIndex = 7;
            radio_zonghe.Text = "综合";
            radio_zonghe.UseVisualStyleBackColor = true;
            // 
            // radio_xingwei
            // 
            radio_xingwei.AutoSize = true;
            radio_xingwei.Location = new Point(339, 8);
            radio_xingwei.Name = "radio_xingwei";
            radio_xingwei.Size = new Size(50, 21);
            radio_xingwei.TabIndex = 6;
            radio_xingwei.Text = "行为";
            radio_xingwei.UseVisualStyleBackColor = true;
            // 
            // radio_shiming
            // 
            radio_shiming.AutoSize = true;
            radio_shiming.Location = new Point(287, 8);
            radio_shiming.Name = "radio_shiming";
            radio_shiming.Size = new Size(50, 21);
            radio_shiming.TabIndex = 5;
            radio_shiming.Text = "使命";
            radio_shiming.UseVisualStyleBackColor = true;
            // 
            // radio_xinyangguanli
            // 
            radio_xinyangguanli.AutoSize = true;
            radio_xinyangguanli.Location = new Point(211, 8);
            radio_xinyangguanli.Name = "radio_xinyangguanli";
            radio_xinyangguanli.Size = new Size(74, 21);
            radio_xinyangguanli.TabIndex = 4;
            radio_xinyangguanli.Text = "信仰管理";
            radio_xinyangguanli.UseVisualStyleBackColor = true;
            // 
            // radio_shouyin
            // 
            radio_shouyin.AutoSize = true;
            radio_shouyin.Location = new Point(159, 8);
            radio_shouyin.Name = "radio_shouyin";
            radio_shouyin.Size = new Size(50, 21);
            radio_shouyin.TabIndex = 3;
            radio_shouyin.Text = "受印";
            radio_shouyin.UseVisualStyleBackColor = true;
            // 
            // radio_xinxin
            // 
            radio_xinxin.AutoSize = true;
            radio_xinxin.Location = new Point(107, 8);
            radio_xinxin.Name = "radio_xinxin";
            radio_xinxin.Size = new Size(50, 21);
            radio_xinxin.TabIndex = 2;
            radio_xinxin.Text = "信心";
            radio_xinxin.UseVisualStyleBackColor = true;
            // 
            // radio_chuandao
            // 
            radio_chuandao.AutoSize = true;
            radio_chuandao.Location = new Point(55, 8);
            radio_chuandao.Name = "radio_chuandao";
            radio_chuandao.Size = new Size(50, 21);
            radio_chuandao.TabIndex = 1;
            radio_chuandao.Text = "传道";
            radio_chuandao.UseVisualStyleBackColor = true;
            // 
            // radio_daogao
            // 
            radio_daogao.AutoSize = true;
            radio_daogao.Location = new Point(3, 8);
            radio_daogao.Name = "radio_daogao";
            radio_daogao.Size = new Size(50, 21);
            radio_daogao.TabIndex = 0;
            radio_daogao.Text = "祷告";
            radio_daogao.UseVisualStyleBackColor = true;
            // 
            // btn_random
            // 
            btn_random.Location = new Point(62, 262);
            btn_random.Name = "btn_random";
            btn_random.Size = new Size(123, 26);
            btn_random.TabIndex = 13;
            btn_random.Text = "随机挑选生命语录";
            btn_random.UseVisualStyleBackColor = true;
            btn_random.Click += btn_random_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(515, 316);
            Controls.Add(btn_random);
            Controls.Add(pl_direction);
            Controls.Add(pl_type);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(btn_clear);
            Controls.Add(statusStrip1);
            Controls.Add(btn_ok);
            Controls.Add(txt_content);
            Controls.Add(label2);
            Controls.Add(label3);
            Controls.Add(label1);
            Controls.Add(txt_date);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            Text = "生命语录记录器";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            pl_type.ResumeLayout(false);
            pl_type.PerformLayout();
            pl_direction.ResumeLayout(false);
            pl_direction.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txt_date;
        private Label label1;
        private Label label3;
        private Label label2;
        private TextBox txt_content;
        private Button btn_ok;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lbl_status;
        private Button btn_clear;
        private Label label4;
        private Label label5;
        private Panel pl_type;
        private RadioButton radio_smyl;
        private RadioButton radio_jwfx;
        private Panel pl_direction;
        private RadioButton radio_chuandao;
        private RadioButton radio_daogao;
        private RadioButton radio_wz;
        private RadioButton radio_xinxin;
        private RadioButton radio_shouyin;
        private RadioButton radio_xinyangguanli;
        private RadioButton radio_shiming;
        private RadioButton radio_xingwei;
        private RadioButton radio_zonghe;
        private Button btn_random;
    }
}
