namespace Client_UI
{
    partial class Lobby
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            label1 = new Label();
            flowLayoutPanel1 = new FlowLayoutPanel();
            button1 = new Button();
            button2 = new Button();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            checkBox1 = new CheckBox();
            button3 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI", 30F);
            label1.ForeColor = SystemColors.ControlLight;
            label1.Location = new Point(1065, 18);
            label1.Name = "label1";
            label1.Size = new Size(123, 54);
            label1.TabIndex = 0;
            label1.Text = "الغرف";
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.BackColor = Color.Transparent;
            flowLayoutPanel1.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanel1.Location = new Point(366, 35);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Padding = new Padding(5);
            flowLayoutPanel1.Size = new Size(693, 592);
            flowLayoutPanel1.TabIndex = 1;
            // 
            // button1
            // 
            button1.BackgroundImage = Properties.Resources.button_background2;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 12F);
            button1.ForeColor = Color.DarkKhaki;
            button1.Location = new Point(1065, 587);
            button1.Name = "button1";
            button1.Size = new Size(107, 40);
            button1.TabIndex = 2;
            button1.Text = "التالي";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.BackgroundImage = Properties.Resources.button_background2;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI", 12F);
            button2.ForeColor = Color.DarkKhaki;
            button2.Location = new Point(253, 587);
            button2.Name = "button2";
            button2.Size = new Size(107, 40);
            button2.TabIndex = 3;
            button2.Text = "السابق";
            button2.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Segoe UI", 25F);
            label2.ForeColor = Color.AntiqueWhite;
            label2.Location = new Point(90, 35);
            label2.Name = "label2";
            label2.Size = new Size(165, 46);
            label2.TabIndex = 4;
            label2.Text = "انشاء غرفه";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Segoe UI", 18F);
            label3.ForeColor = Color.AntiqueWhite;
            label3.Location = new Point(231, 130);
            label3.Name = "label3";
            label3.Size = new Size(126, 32);
            label3.TabIndex = 5;
            label3.Text = ":اسم الغرفه";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.Font = new Font("Segoe UI", 18F);
            label4.ForeColor = Color.AntiqueWhite;
            label4.Location = new Point(219, 194);
            label4.Name = "label4";
            label4.Size = new Size(138, 32);
            label4.TabIndex = 6;
            label4.Text = ":عدد اللاعبين";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(22, 141);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(190, 23);
            textBox1.TabIndex = 8;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(22, 203);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(190, 23);
            textBox2.TabIndex = 9;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.BackColor = Color.Transparent;
            checkBox1.Font = new Font("Segoe UI", 18F);
            checkBox1.ForeColor = Color.AntiqueWhite;
            checkBox1.Location = new Point(88, 256);
            checkBox1.Name = "checkBox1";
            checkBox1.RightToLeft = RightToLeft.Yes;
            checkBox1.Size = new Size(269, 36);
            checkBox1.TabIndex = 10;
            checkBox1.Text = ":البدء بعد وجود شخصين";
            checkBox1.UseVisualStyleBackColor = false;
            // 
            // button3
            // 
            button3.BackgroundImage = Properties.Resources.button_background2;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Font = new Font("Segoe UI", 16F);
            button3.ForeColor = Color.DarkKhaki;
            button3.Location = new Point(74, 316);
            button3.Name = "button3";
            button3.Size = new Size(191, 48);
            button3.TabIndex = 11;
            button3.Text = "انشاء الغرفه";
            button3.UseVisualStyleBackColor = true;
            // 
            // Lobby
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.Gemini_Generated_Image_hmkq8fhmkq8fhmkq;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1212, 674);
            Controls.Add(button3);
            Controls.Add(checkBox1);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(flowLayoutPanel1);
            Controls.Add(label1);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Lobby";
            Text = "Lobby";
            Load += Lobby_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button button1;
        private Button button2;
        private Label label2;
        private Label label3;
        private Label label4;
        private TextBox textBox1;
        private TextBox textBox2;
        private CheckBox checkBox1;
        private Button button3;
    }
}