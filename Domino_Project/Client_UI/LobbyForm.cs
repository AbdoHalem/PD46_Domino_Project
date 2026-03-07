using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Client_UI.Network;
using Domino.Shared;
using Domino.Shared.Models.Responses;

namespace Client_UI
{
    public class LobbyForm : Form
    {
        //  Networking
        private readonly DominoClient _client;
        private readonly string _playerName;

        // Pagination 
        private List<RoomSummary> _allRooms = new();
        private Dictionary<string, Panel> _cards = new();
        private int _currentPage = 0;
        private const int PageSize = 6;

        // UI controls 
        private FlowLayoutPanel flowRooms;
        private Button btnNext, btnPrev, btnCreateRoom;
        private TextBox txtRoomName, txtMaxPlayers, txtScoreLimit;
        private Label lblStatus;
        private Panel pnlCreate;

        // Waiting Room UI Controls
        private Panel pnlWaitingRoom;
        private ListBox lstPlayers;
        private Button btnStartGame;
        private Button btnLeaveRoom;
        private Label lblRoomTitle;

        public LobbyForm(DominoClient client, string playerName)
        {
            _client = client;
            _playerName = playerName;

            BuildUI();

            _client.MessageReceived += OnMessageReceived;
            _client.Disconnected += (s, e) =>
                MessageBox.Show("Disconnected from server.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            MaximizeBox = false;
        }

        private void BuildUI()
        {
            Text = $"Domino Lobby  –  {_playerName}";
            Size = new Size(1212, 674);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            BackColor = Color.FromArgb(34, 52, 34);
            DoubleBuffered = true;

            // Status label
            lblStatus = new Label
            {
                Text = "Connecting to lobby…",
                ForeColor = Color.LightYellow,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(370, 8)
            };
            Controls.Add(lblStatus);

            pnlCreate = new Panel
            {
                Location = new Point(10, 35),
                Size = new Size(345, 420),
                BackColor = Color.FromArgb(20, 40, 20)
            };
            Controls.Add(pnlCreate);

            // Left-aligned Labels and right-aligned TextBoxes for English LTR layout
            AddLabel(pnlCreate, "Create New Room", 10, 10, 22, Color.AntiqueWhite);
            AddLabel(pnlCreate, "Room Name:", 10, 65, 14, Color.AntiqueWhite);
            AddLabel(pnlCreate, "Max Players:", 10, 115, 14, Color.AntiqueWhite);
            AddLabel(pnlCreate, "Score Limit:", 10, 165, 14, Color.AntiqueWhite);

            txtRoomName = AddTextBox(pnlCreate, 140, 65, "My Room", 190);
            txtMaxPlayers = AddTextBox(pnlCreate, 140, 115, "2", 60);
            txtScoreLimit = AddTextBox(pnlCreate, 140, 165, "100", 80);

            btnCreateRoom = new Button
            {
                Text = "Create Room",
                Location = new Point(80, 220),
                Size = new Size(180, 48),
                BackColor = Color.FromArgb(30, 110, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13, FontStyle.Bold)
            };
            btnCreateRoom.Click += BtnCreateRoom_Click;
            pnlCreate.Controls.Add(btnCreateRoom);

            // ── Right panel: Room list ────────────────────────────────
            AddLabel(this, "Available Rooms", 366, 22, 22, Color.AntiqueWhite);

            flowRooms = new FlowLayoutPanel
            {
                Location = new Point(366, 40),
                Size = new Size(693, 592),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(5)
            };
            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, flowRooms, new object[] { true });
            Controls.Add(flowRooms);

            btnPrev = StyledBtn("Previous", new Point(253, 587));
            btnNext = StyledBtn("Next", new Point(1065, 587));

            btnNext.Click += (s, e) => { if ((_currentPage + 1) * PageSize < _allRooms.Count) { _currentPage++; RenderPage(); } };
            btnPrev.Click += (s, e) => { if (_currentPage > 0) { _currentPage--; RenderPage(); } };
            Controls.Add(btnNext);
            Controls.Add(btnPrev);

            // ── NEW: Waiting Room Panel (Hidden initially) ────────────
            pnlWaitingRoom = new Panel
            {
                Location = new Point(366, 40),
                Size = new Size(693, 592),
                BackColor = Color.FromArgb(20, 40, 20),
                Visible = false
            };
            Controls.Add(pnlWaitingRoom);

            lblRoomTitle = new Label
            {
                Text = "Waiting Room",
                ForeColor = Color.AntiqueWhite,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(250, 15) // Centered for LTR
            };
            pnlWaitingRoom.Controls.Add(lblRoomTitle);

            lstPlayers = new ListBox
            {
                Location = new Point(350, 70),
                Size = new Size(300, 300),
                Font = new Font("Segoe UI", 16),
                BackColor = Color.FromArgb(30, 50, 30),
                ForeColor = Color.White,
                RightToLeft = RightToLeft.No // Disabled RightToLeft
            };
            pnlWaitingRoom.Controls.Add(lstPlayers);

            btnStartGame = new Button
            {
                Text = "Start Game",
                Location = new Point(100, 70),
                Size = new Size(200, 60),
                BackColor = Color.FromArgb(30, 110, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 15, FontStyle.Bold)
            };
            btnStartGame.Click += BtnStartGame_Click;
            pnlWaitingRoom.Controls.Add(btnStartGame);

            btnLeaveRoom = new Button
            {
                Text = "Leave Room",
                Location = new Point(100, 150),
                Size = new Size(200, 60),
                BackColor = Color.FromArgb(140, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 15, FontStyle.Bold)
            };
            btnLeaveRoom.Click += BtnLeaveRoom_Click;
            pnlWaitingRoom.Controls.Add(btnLeaveRoom);

            // Request lobby data from server
            _ = _client.SendAsync(GameConstants.ActionJoinLobby);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnMessageReceived(sender, e)));
                return;
            }

            try
            {
                switch (e.Action)
                {
                    case GameConstants.EventLobbySnapshot:
                        var snapshot = e.Payload.Deserialize<LobbySnapshotResponse>(JsonOpts.Default);
                        if (snapshot != null) LoadRooms(snapshot.Rooms);
                        break;

                    case GameConstants.EventRoomSnapshot:
                        var room = e.Payload.Deserialize<RoomStateResponse>(JsonOpts.Default);
                        if (room != null)
                        {
                            lblStatus.Text = $"In room: {room.RoomName} ({room.ConnectedPlayers.Count}/{room.MaxPlayers})";
                            ShowWaitingRoom(room);
                        }
                        break;

                    case GameConstants.EventGameStarted:
                        var hand = e.Payload.Deserialize<PlayerHandResponse>(JsonOpts.Default);

                        // 2. THE NULL FIX: Make sure 'Hand' isn't null before passing it to GameController
                        if (hand != null && hand.Hand != null)
                        {
                            OpenGameForm(hand);
                        }
                        else
                        {
                            MessageBox.Show("The server started the game, but the domino hand data could not be mapped to the 'Hand' property in PlayerHandResponse.",
                                            "Data Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        break;

                    case GameConstants.EventError:
                        MessageBox.Show(e.Payload.GetString(), "Server Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnCreateRoom.Enabled = true;
                        break;

                    case GameConstants.EventWatcherJoined:
                        var watcherData = e.Payload.Deserialize<WatcherJoinedResponse>(JsonOpts.Default);
                        if (watcherData != null)
                        {
                            OpenSpectatorForm(watcherData); // You will need to create this method
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to process server event '{e.Action}':\n\n{ex.Message}",
                                "Crash Prevented", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRooms(List<RoomSummary> rooms)
        {
            _allRooms = rooms ?? new List<RoomSummary>();
            _currentPage = 0;
            _cards.Clear();
            RenderPage();
            lblStatus.Text = $"Lobby  –  {_allRooms.Count} room(s) available";
        }

        private void RenderPage()
        {
            flowRooms.SuspendLayout();
            flowRooms.Controls.Clear();

            foreach (var r in _allRooms.Skip(_currentPage * PageSize).Take(PageSize))
            {
                if (!_cards.TryGetValue(r.RoomId, out var card))
                {
                    card = CreateCard(r);
                    _cards[r.RoomId] = card;
                }
                flowRooms.Controls.Add(card);
            }

            flowRooms.ResumeLayout();
            flowRooms.Refresh();

            btnNext.Enabled = (_currentPage + 1) * PageSize < _allRooms.Count;
            btnPrev.Enabled = _currentPage > 0;
        }

        private Panel CreateCard(RoomSummary r)
        {
            bool isFull = r.CurrentCount >= r.MaxPlayers;
            bool running = r.GameRunning;

            var card = new Panel
            {
                Name = $"card_{r.RoomId}",
                Size = new Size(210, 275),
                Margin = new Padding(8),
                BackColor = Color.FromArgb(20, 45, 20),
                Tag = r.RoomId
            };

            card.Controls.Add(new Label
            {
                Text = $"Room: {r.RoomName}",
                AutoSize = true,
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(10, 20)
            });

            card.Controls.Add(new Label
            {
                Name = "lblCount",
                Text = $"Players: {r.CurrentCount}/{r.MaxPlayers}",
                AutoSize = true,
                Font = new Font("Tahoma", 10),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                Location = new Point(10, 60)
            });

            card.Controls.Add(new Label
            {
                Text = running ? "Game in Progress" : "Waiting",
                AutoSize = true,
                Font = new Font("Tahoma", 9, FontStyle.Italic),
                ForeColor = running ? Color.Gold : Color.LightGreen,
                BackColor = Color.Transparent,
                Location = new Point(10, 90)
            });

            var btnJoin = new Button
            {
                Name = "btnJoin",
                Text = "Join",
                Size = new Size(80, 35),
                Location = new Point(10, 220),
                Enabled = !isFull && !running,
                BackColor = Color.FromArgb(30, 80, 150),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnJoin.Click += (s, e) => JoinRoom(r.RoomId);
            card.Controls.Add(btnJoin);

            var btnWatch = new Button
            {
                Name = "btnWatch",
                Text = "Watch",
                Size = new Size(80, 35),
                Location = new Point(120, 220),
                Enabled = isFull || running,
                BackColor = Color.FromArgb(100, 60, 10),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnWatch.Click += (s, e) => WatchRoom(r.RoomId);
            card.Controls.Add(btnWatch);

            return card;
        }

        private void ShowWaitingRoom(RoomStateResponse room)
        {
            flowRooms.Visible = false;
            btnNext.Visible = false;
            btnPrev.Visible = false;
            pnlCreate.Visible = false;
            pnlWaitingRoom.Visible = true;

            lblRoomTitle.Text = $"Room: {room.RoomName}";
            lstPlayers.Items.Clear();

            bool isOwner = false;

            // Populate connected players
            if (room.ConnectedPlayers != null && room.ConnectedPlayers.Count > 0)
            {
                isOwner = (room.OwnerName == _playerName);

                foreach (var player in room.ConnectedPlayers)
                {
                    string displayTag = (player == room.OwnerName) ? " (Host)" : "";
                    lstPlayers.Items.Add(player + displayTag);
                }
            }

            if (isOwner)
            {
                btnStartGame.Visible = true;
                btnStartGame.Text = "Start Game";
                btnStartGame.Enabled = (room.ConnectedPlayers != null && room.ConnectedPlayers.Count >= GameConstants.MinPlayersToStart);
            }
            else
            {
                btnStartGame.Visible = true;
                btnStartGame.Text = "Waiting Host...";
                btnStartGame.Enabled = false;
            }
        }

        private void ReturnToLobbyView()
        {
            pnlWaitingRoom.Visible = false;
            flowRooms.Visible = true;
            btnNext.Visible = true;
            btnPrev.Visible = true;
            pnlCreate.Visible = true;
            btnCreateRoom.Enabled = true;
        }

        private async void BtnCreateRoom_Click(object sender, EventArgs e)
        {
            string name = txtRoomName.Text.Trim();
            if (string.IsNullOrEmpty(name)) { MessageBox.Show("Enter a room name."); return; }
            if (!int.TryParse(txtMaxPlayers.Text, out int max) || max < 2 || max > 4)
            { MessageBox.Show("Players must be 2–4."); return; }
            if (!int.TryParse(txtScoreLimit.Text, out int limit) || limit < 10)
            { MessageBox.Show("Score limit must be ≥ 10."); return; }

            btnCreateRoom.Enabled = false;
            await _client.SendAsync(GameConstants.ActionCreateRoom,
                new { RoomName = name, MaxPlayers = max, ScoreLimit = limit });
        }

        private async void JoinRoom(string roomId) =>
            await _client.SendAsync(GameConstants.ActionJoinRoom, new { RoomId = roomId });

        private async void WatchRoom(string roomId) =>
            await _client.SendAsync(GameConstants.ActionWatchRoom, new { RoomId = roomId });

        private async void BtnStartGame_Click(object sender, EventArgs e)
        {
            btnStartGame.Enabled = false;
            btnStartGame.Text = "Waiting others...";

            await _client.SendAsync(GameConstants.ActionReadyUp);
        }

        private async void BtnLeaveRoom_Click(object sender, EventArgs e)
        {
            await _client.SendAsync(GameConstants.ActionLeaveRoom);
            ReturnToLobbyView();
            // Ask server for fresh lobby data since we left the room
            await _client.SendAsync(GameConstants.ActionJoinLobby);
        }

        private void OpenGameForm(PlayerHandResponse hand)
        {
            try
            {
                var gf = new GameController(_client, _playerName, hand);

                gf.FormClosed += async (s, e) =>
                {
                    this.Show();
                    ReturnToLobbyView();
                    await _client.SendAsync(GameConstants.ActionJoinLobby);
                };

                this.Hide();
                gf.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load the Game UI:\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Show();
            }
        }
        private void OpenSpectatorForm(WatcherJoinedResponse watcherData)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OpenSpectatorForm(watcherData)));
                return;
            }

            try
            {
                var gf = new GameController(_client, _playerName, watcherData);

                gf.FormClosed += async (s, e) =>
                {
                    this.Show();
                    ReturnToLobbyView();
                    // Now properly awaited inside an async lambda
                    await _client.SendAsync(GameConstants.ActionJoinLobby);
                };

                this.Hide();
                gf.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load the Spectator UI:\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Show();
            }
        }

        private static void AddLabel(Control parent, string text, int x, int y, float size, Color color)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", size),
                ForeColor = color,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            });
        }

        private static TextBox AddTextBox(Control parent, int x, int y, string defaultText, int width)
        {
            var tb = new TextBox { Location = new Point(x, y), Size = new Size(width, 26), Text = defaultText, TextAlign = HorizontalAlignment.Center };
            parent.Controls.Add(tb);
            return tb;
        }

        private static Button StyledBtn(string text, Point loc) => new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(107, 40),
            BackColor = Color.FromArgb(30, 80, 30),
            ForeColor = Color.DarkKhaki,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12)
        };

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // LobbyForm
            // 
            ClientSize = new Size(282, 253);
            MaximizeBox = false;
            Name = "LobbyForm";
            ResumeLayout(false);

        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _client.MessageReceived -= OnMessageReceived;
            base.OnFormClosed(e);
        }
    }

    internal static class JsonOpts
    {
        public static readonly JsonSerializerOptions Default = new()
        { PropertyNameCaseInsensitive = true };
    }
}