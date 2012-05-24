namespace RecoveryStar
{
    partial class PasswordForm
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
            this.password1Label = new System.Windows.Forms.Label();
            this.password2Label = new System.Windows.Forms.Label();
            this.CBCBlockSizeTextBox = new System.Windows.Forms.TextBox();
            this.maxCBCBlockSizeLabel = new System.Windows.Forms.Label();
            this.passwordTextBox2 = new System.Windows.Forms.TextBox();
            this.passwordTextBox1 = new System.Windows.Forms.TextBox();
            this.LanguageLabel = new System.Windows.Forms.Label();
            this.LanguageTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // password1Label
            // 
            this.password1Label.Location = new System.Drawing.Point(47, 13);
            this.password1Label.Name = "password1Label";
            this.password1Label.Size = new System.Drawing.Size(56, 20);
            this.password1Label.TabIndex = 0;
            this.password1Label.Text = "Password";
            this.password1Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // password2Label
            // 
            this.password2Label.Location = new System.Drawing.Point(47, 39);
            this.password2Label.Name = "password2Label";
            this.password2Label.Size = new System.Drawing.Size(56, 20);
            this.password2Label.TabIndex = 0;
            this.password2Label.Text = "Password";
            this.password2Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // CBCBlockSizeTextBox
            // 
            this.CBCBlockSizeTextBox.Location = new System.Drawing.Point(185, 66);
            this.CBCBlockSizeTextBox.MaxLength = 10;
            this.CBCBlockSizeTextBox.Name = "CBCBlockSizeTextBox";
            this.CBCBlockSizeTextBox.Size = new System.Drawing.Size(155, 20);
            this.CBCBlockSizeTextBox.TabIndex = 2;
            this.CBCBlockSizeTextBox.Text = "131072";
            this.CBCBlockSizeTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CBCBlockSizeTextBox_KeyDown);
            this.CBCBlockSizeTextBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.CBCBlockSizeTextBox_KeyUp);
            // 
            // maxCBCBlockSizeLabel
            // 
            this.maxCBCBlockSizeLabel.AutoSize = true;
            this.maxCBCBlockSizeLabel.Location = new System.Drawing.Point(9, 69);
            this.maxCBCBlockSizeLabel.Name = "maxCBCBlockSizeLabel";
            this.maxCBCBlockSizeLabel.Size = new System.Drawing.Size(170, 13);
            this.maxCBCBlockSizeLabel.TabIndex = 0;
            this.maxCBCBlockSizeLabel.Text = "Size of CBC-Block per volume (Kb)";
            // 
            // passwordTextBox2
            // 
            this.passwordTextBox2.Location = new System.Drawing.Point(109, 40);
            this.passwordTextBox2.MaxLength = 32;
            this.passwordTextBox2.Name = "passwordTextBox2";
            this.passwordTextBox2.PasswordChar = '*';
            this.passwordTextBox2.Size = new System.Drawing.Size(231, 20);
            this.passwordTextBox2.TabIndex = 1;
            this.passwordTextBox2.UseSystemPasswordChar = true;
            this.passwordTextBox2.TextChanged += new System.EventHandler(this.passwordTextBoxes_TextChanged);
            this.passwordTextBox2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.passwordTextBoxes_KeyDown);
            // 
            // passwordTextBox1
            // 
            this.passwordTextBox1.Location = new System.Drawing.Point(109, 14);
            this.passwordTextBox1.MaxLength = 32;
            this.passwordTextBox1.Name = "passwordTextBox1";
            this.passwordTextBox1.PasswordChar = '*';
            this.passwordTextBox1.Size = new System.Drawing.Size(231, 20);
            this.passwordTextBox1.TabIndex = 0;
            this.passwordTextBox1.UseSystemPasswordChar = true;
            this.passwordTextBox1.TextChanged += new System.EventHandler(this.passwordTextBoxes_TextChanged);
            this.passwordTextBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.passwordTextBoxes_KeyDown);
            // 
            // LanguageLabel
            // 
            this.LanguageLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.LanguageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.LanguageLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.LanguageLabel.Location = new System.Drawing.Point(13, 14);
            this.LanguageLabel.Name = "LanguageLabel";
            this.LanguageLabel.Size = new System.Drawing.Size(28, 46);
            this.LanguageLabel.TabIndex = 0;
            this.LanguageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // LanguageTimer
            // 
            this.LanguageTimer.Interval = 250;
            this.LanguageTimer.Tick += new System.EventHandler(this.LangTimer_Tick);
            // 
            // PasswordForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(352, 100);
            this.Controls.Add(this.LanguageLabel);
            this.Controls.Add(this.passwordTextBox1);
            this.Controls.Add(this.passwordTextBox2);
            this.Controls.Add(this.maxCBCBlockSizeLabel);
            this.Controls.Add(this.CBCBlockSizeTextBox);
            this.Controls.Add(this.password2Label);
            this.Controls.Add(this.password1Label);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PasswordForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = " Encryption parameters of AES-256";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.PasswordForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PasswordForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label password1Label;
        private System.Windows.Forms.Label password2Label;
        private System.Windows.Forms.TextBox CBCBlockSizeTextBox;
        private System.Windows.Forms.Label maxCBCBlockSizeLabel;
        private System.Windows.Forms.TextBox passwordTextBox2;
        private System.Windows.Forms.TextBox passwordTextBox1;
        private System.Windows.Forms.Label LanguageLabel;
        private System.Windows.Forms.Timer LanguageTimer;



    }
}