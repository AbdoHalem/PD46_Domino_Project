using System;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Client_UI.Network;
using Domino.Shared;

namespace Client_UI
{
    public partial class Login : Form
    {
        private DominoClient _client;

        private const string ServerHost = "127.0.0.1";
        private const int    ServerPort = 5500;

        public Login()
        {
            InitializeComponent();

            btnLogin.Click += BtnLogin_ClickAsync;
        }

        private void Login_Load(object sender, EventArgs e)
        {
            btnLogin.FlatAppearance.BorderSize = 0;
            textBox1.KeyDown += (s, ke) =>
            {
                if (ke.KeyCode == Keys.Enter) BtnLogin_ClickAsync(s, ke);
            };
        }

        private async void BtnLogin_ClickAsync(object sender, EventArgs e)
        {
            string playerName = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                MessageBox.Show("Please enter a player name.", "Login",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text    = "Connecting…";

            _client = new DominoClient(this);

            _client.MessageReceived += (s, args) =>
            {
                if (args.Action == GameConstants.EventLoginOk)
                    OnLoginSuccess(playerName, args.Payload);
                else if (args.Action == GameConstants.EventError)
                    OnLoginError(args.Payload.GetString());
            };

            _client.Disconnected += (s, a) => OnLoginError("Disconnected from server.");

            bool connected = await _client.ConnectAsync(ServerHost, ServerPort);
            if (!connected)
            {
                OnLoginError($"Could not reach server at {ServerHost}:{ServerPort}");
                return;
            }

            await _client.SendAsync(GameConstants.ActionLogin, new { PlayerName = playerName });
        }

        private void OnLoginSuccess(string playerName, JsonElement payload)
        {
            var lobby = new LobbyForm(_client, playerName);
            lobby.FormClosed += (s, e) => this.Close();
            this.Hide();
            lobby.Show();
        }

        private void OnLoginError(string message)
        {
            btnLogin.Enabled = true;
            btnLogin.Text    = "تسجيل الدخول";
            MessageBox.Show($"Login failed:\n{message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _client?.Disconnect();
            _client = null;
        }

        private void lblWelcome_Click(object sender, EventArgs e)  { }
        private void lblUsername_Click(object sender, EventArgs e) { }

        private void btnLogin_MouseEnter(object sender, EventArgs e)
        {
            btnLogin.Width    += 10;
            btnLogin.Height   += 6;
            btnLogin.Location  = new Point(btnLogin.Location.X - 5, btnLogin.Location.Y - 3);
            btnLogin.Font      = new Font(btnLogin.Font.FontFamily, btnLogin.Font.Size + 1);
        }
        private void btnLogin_MouseLeave(object sender, EventArgs e)
        {
            btnLogin.Width    -= 10;
            btnLogin.Height   -= 6;
            btnLogin.Location  = new Point(btnLogin.Location.X + 5, btnLogin.Location.Y + 3);
            btnLogin.Font      = new Font(btnLogin.Font.FontFamily, btnLogin.Font.Size - 1);
        }
    }
}
