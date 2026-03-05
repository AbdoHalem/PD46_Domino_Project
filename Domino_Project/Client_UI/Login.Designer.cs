namespace Client_UI
{
    partial class Login
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
            lblWelcome = new Label();
            lblUsername = new Label();
            textBox1 = new TextBox();
            btnLogin = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // lblWelcome
            // 
            lblWelcome.AutoSize = true;
            lblWelcome.BackColor = Color.Transparent;
            lblWelcome.Font = new Font("Arial Narrow", 45F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lblWelcome.ForeColor = Color.FromArgb(255, 192, 128);
            lblWelcome.Location = new Point(553, 15);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(196, 69);
            lblWelcome.TabIndex = 0;
            lblWelcome.Text = "اهلا بيك";
            lblWelcome.TextAlign = ContentAlignment.MiddleRight;
            lblWelcome.Click += lblWelcome_Click;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.BackColor = Color.Transparent;
            lblUsername.Font = new Font("Segoe UI", 14F);
            lblUsername.ForeColor = SystemColors.ButtonHighlight;
            lblUsername.Location = new Point(624, 215);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(125, 25);
            lblUsername.TabIndex = 1;
            lblUsername.Text = "أسم المستخدم";
            lblUsername.Click += lblUsername_Click;
            // 
            // textBox1
            // 
            textBox1.Font = new Font("Segoe UI", 12F);
            textBox1.Location = new Point(436, 215);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(182, 29);
            textBox1.TabIndex = 2;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.Transparent;
            btnLogin.BackgroundImage = Properties.Resources.button_background2;
            btnLogin.BackgroundImageLayout = ImageLayout.Stretch;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Arial", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 178);
            btnLogin.ForeColor = Color.DarkKhaki;
            btnLogin.Location = new Point(579, 313);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(126, 35);
            btnLogin.TabIndex = 3;
            btnLogin.Text = "تسجيل الدخول";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.MouseEnter += btnLogin_MouseEnter;
            btnLogin.MouseLeave += btnLogin_MouseLeave;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Arial Narrow", 45F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(255, 192, 128);
            label1.Location = new Point(302, 84);
            label1.Name = "label1";
            label1.Size = new Size(359, 69);
            label1.TabIndex = 4;
            label1.Text = "في ساعة دومينو";
            label1.TextAlign = ContentAlignment.MiddleRight;
            // 
            // Login
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.login_Backgroundflipped2;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(780, 438);
            Controls.Add(label1);
            Controls.Add(btnLogin);
            Controls.Add(textBox1);
            Controls.Add(lblUsername);
            Controls.Add(lblWelcome);
            DoubleBuffered = true;
            Name = "Login";
            Text = "Login";
            Load += Login_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblWelcome;
        private Label lblUsername;
        private TextBox textBox1;
        private Button btnLogin;
        private Label label1;
    }
}