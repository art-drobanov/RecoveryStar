namespace RecoveryStar
{
    partial class AboutForm
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
            this.components = new System.ComponentModel.Container();
            this.imitLinkLabel = new System.Windows.Forms.LinkLabel();
            this.imitLabel = new System.Windows.Forms.Label();
            this.RecoveryStarLabel = new System.Windows.Forms.Label();
            this.copyrightListBox = new System.Windows.Forms.ListBox();
            this.developersListBoxdevelopersListBox = new System.Windows.Forms.ListBox();
            this.RSIconTimer = new System.Windows.Forms.Timer(this.components);
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.splitterPictureBox = new System.Windows.Forms.PictureBox();
            this.okButtonXP = new PinkieControls.ButtonXP();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitterPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // imitLinkLabel
            // 
            this.imitLinkLabel.AutoSize = true;
            this.imitLinkLabel.Location = new System.Drawing.Point(10, 44);
            this.imitLinkLabel.Name = "imitLinkLabel";
            this.imitLinkLabel.Size = new System.Drawing.Size(92, 13);
            this.imitLinkLabel.TabIndex = 1;
            this.imitLinkLabel.TabStop = true;
            this.imitLinkLabel.Tag = "";
            this.imitLinkLabel.Text = "http://www.imit.ru";
            this.imitLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.imitLinkLabel_LinkClicked);
            // 
            // imitLabel
            // 
            this.imitLabel.AutoSize = true;
            this.imitLabel.Location = new System.Drawing.Point(9, 25);
            this.imitLabel.Name = "imitLabel";
            this.imitLabel.Size = new System.Drawing.Size(259, 13);
            this.imitLabel.TabIndex = 0;
            this.imitLabel.Text = "© 2006 — 2013  Russia, IMIT SPbSPU, Cherepovets.";
            // 
            // RecoveryStarLabel
            // 
            this.RecoveryStarLabel.AutoSize = true;
            this.RecoveryStarLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.RecoveryStarLabel.Location = new System.Drawing.Point(9, 9);
            this.RecoveryStarLabel.Name = "RecoveryStarLabel";
            this.RecoveryStarLabel.Size = new System.Drawing.Size(117, 13);
            this.RecoveryStarLabel.TabIndex = 0;
            this.RecoveryStarLabel.Text = "Recovery Star 2.22";
            // 
            // copyrightListBox
            // 
            this.copyrightListBox.BackColor = System.Drawing.SystemColors.Control;
            this.copyrightListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.copyrightListBox.FormattingEnabled = true;
            this.copyrightListBox.Items.AddRange(new object[] {
            "This program is absolutely free and may be used",
            " in part or in its entirety for any purposes."});
            this.copyrightListBox.Location = new System.Drawing.Point(11, 145);
            this.copyrightListBox.Name = "copyrightListBox";
            this.copyrightListBox.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.copyrightListBox.Size = new System.Drawing.Size(257, 26);
            this.copyrightListBox.TabIndex = 0;
            this.copyrightListBox.TabStop = false;
            // 
            // developersListBoxdevelopersListBox
            // 
            this.developersListBoxdevelopersListBox.BackColor = System.Drawing.SystemColors.Control;
            this.developersListBoxdevelopersListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.developersListBoxdevelopersListBox.FormattingEnabled = true;
            this.developersListBoxdevelopersListBox.Items.AddRange(new object[] {
            "Authors:       Drobanov Artem Fedorovich (DrAF),",
            "                    RUSpectrum (Orenburg city).",
            "Mathematical support: persicum@front.ru.",
            "Translation by Leonid Korolkov (412)."});
            this.developersListBoxdevelopersListBox.Location = new System.Drawing.Point(11, 67);
            this.developersListBoxdevelopersListBox.Name = "developersListBoxdevelopersListBox";
            this.developersListBoxdevelopersListBox.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.developersListBoxdevelopersListBox.Size = new System.Drawing.Size(257, 52);
            this.developersListBoxdevelopersListBox.TabIndex = 0;
            this.developersListBoxdevelopersListBox.TabStop = false;
            // 
            // RSIconTimer
            // 
            this.RSIconTimer.Interval = 40;
            this.RSIconTimer.Tick += new System.EventHandler(this.RSIconTimer_Tick);
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Location = new System.Drawing.Point(296, 11);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(96, 96);
            this.logoPictureBox.TabIndex = 40;
            this.logoPictureBox.TabStop = false;
            // 
            // splitterPictureBox
            // 
            this.splitterPictureBox.Image = global::RecoveryStar.Properties.Resources.AboutSplitter;
            this.splitterPictureBox.Location = new System.Drawing.Point(0, 132);
            this.splitterPictureBox.Name = "splitterPictureBox";
            this.splitterPictureBox.Size = new System.Drawing.Size(394, 2);
            this.splitterPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.splitterPictureBox.TabIndex = 32;
            this.splitterPictureBox.TabStop = false;
            // 
            // okButtonXP
            // 
            this.okButtonXP.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.okButtonXP.DefaultScheme = true;
            this.okButtonXP.DialogResult = System.Windows.Forms.DialogResult.None;
            this.okButtonXP.Hint = "";
            this.okButtonXP.Location = new System.Drawing.Point(307, 150);
            this.okButtonXP.Name = "okButtonXP";
            this.okButtonXP.Scheme = PinkieControls.ButtonXP.Schemes.Blue;
            this.okButtonXP.Size = new System.Drawing.Size(75, 23);
            this.okButtonXP.TabIndex = 0;
            this.okButtonXP.Text = "OK";
            this.okButtonXP.Click += new System.EventHandler(this.okButtonXP_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 185);
            this.Controls.Add(this.okButtonXP);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.splitterPictureBox);
            this.Controls.Add(this.imitLinkLabel);
            this.Controls.Add(this.imitLabel);
            this.Controls.Add(this.RecoveryStarLabel);
            this.Controls.Add(this.copyrightListBox);
            this.Controls.Add(this.developersListBoxdevelopersListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = " About Recovery Star 2.22 (02.04.2013)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AboutForm_FormClosing);
            this.Load += new System.EventHandler(this.AboutForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitterPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox splitterPictureBox;
        private System.Windows.Forms.LinkLabel imitLinkLabel;
        private System.Windows.Forms.Label imitLabel;
        private System.Windows.Forms.Label RecoveryStarLabel;
        private System.Windows.Forms.ListBox copyrightListBox;
        private System.Windows.Forms.ListBox developersListBoxdevelopersListBox;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Timer RSIconTimer;
        private PinkieControls.ButtonXP okButtonXP;

    }
}