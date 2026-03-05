using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client_UI
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            btnLogin.FlatAppearance.BorderSize = 0;
        }

        private void lblWelcome_Click(object sender, EventArgs e)
        {

        }

        private void lblUsername_Click(object sender, EventArgs e)
        {

        }

        private void btnLogin_MouseEnter(object sender, EventArgs e)
        {
            btnLogin.Width += 10;
            btnLogin.Height += 6;
            btnLogin.Location = new Point(btnLogin.Location.X - 5, btnLogin.Location.Y - 3);
            btnLogin.Font = new Font(btnLogin.Font.FontFamily, btnLogin.Font.Size + 1);
        }
        private void btnLogin_MouseLeave(object sender, EventArgs e)
        {
            btnLogin.Width -= 10;
            btnLogin.Height -= 6;
            btnLogin.Location = new Point(btnLogin.Location.X + 5, btnLogin.Location.Y + 3);
            btnLogin.Font = new Font(btnLogin.Font.FontFamily, btnLogin.Font.Size - 1);
        }
    }
}
