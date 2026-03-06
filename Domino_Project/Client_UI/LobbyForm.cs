// =====================================================================
//  FILE: LobbyForm.cs  (Client_UI)
// =====================================================================
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
        // ── Networking ────────────────────────────────────────────────
        private readonly DominoClient _client;
        private readonly string _playerName;

        // ── Pagination ────────────────────────────────────────────────
        private List<RoomSummary> _allRooms = new();
        private Dictionary<string, Panel> _cards = new();
        private int _currentPage = 0;
        private const int PageSize = 6;

        // ── UI controls ───────────────────────────────────────────────
        private FlowLayoutPanel flowRooms;
        private Button btnNext, btnPrev, btnCreateRoom;
        private TextBox txtRoomName, txtMaxPlayers, txtScoreLimit;
        private Label lblStatus;
        private Panel pnlCreate;

        // ── NEW: Waiting Room UI Controls ─────────────────────────────
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
        }

        // ================================================================
        //  UI LAYOUT
        // ================================================================
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

            // ── Left panel: Create Room ───────────────────────────────
            pnlCreate = new Panel
            {
                Location = new Point(10, 35),
                Size = new Size(345, 420),
                BackColor = Color.FromArgb(20, 40, 20)
            };
            Controls.Add(pnlCreate);

            AddLabel(pnlCreate, "إنشاء غرفة جديدة", 8, 10, 22, Color.AntiqueWhite);
            AddLabel(pnlCreate, ":اسم الغرفة", 215, 60, 14, Color.AntiqueWhite);
            AddLabel(pnlCreate, ":عدد اللاعبين", 200, 110, 14, Color.AntiqueWhite);
            AddLabel(pnlCreate, ":حد النقاط", 210, 160, 14, Color.AntiqueWhite);

            txtRoomName = AddTextBox(pnlCreate, 10, 65, "My Room", 190);
            txtMaxPlayers = AddTextBox(pnlCreate, 10, 115, "2", 60);
            txtScoreLimit = AddTextBox(pnlCreate, 10, 165, "100", 80);

            btnCreateRoom = new Button
            {
                Text = "إنشاء الغرفة",
                Location = new Point(70, 220),
                Size = new Size(180, 48),
                BackColor = Color.FromArgb(30, 110, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13, FontStyle.Bold)
            };
            btnCreateRoom.Click += BtnCreateRoom_Click;
            pnlCreate.Controls.Add(btnCreateRoom);

            // ── Right panel: Room list ────────────────────────────────
            AddLabel(this, "الغرف المتاحة", 1060, 10, 22, Color.AntiqueWhite);

            flowRooms = new FlowLayoutPanel
            {
                Location = new Point(366, 35),
                Size = new Size(693, 592),
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(5)
            };
            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, flowRooms, new object[] { true });
            Controls.Add(flowRooms);

            btnNext = StyledBtn("التالي", new Point(1065, 587));
            btnPrev = StyledBtn("السابق", new Point(253, 587));
            btnNext.Click += (s, e) => { if ((_currentPage + 1) * PageSize < _allRooms.Count) { _currentPage++; RenderPage(); } };
            btnPrev.Click += (s, e) => { if (_currentPage > 0) { _currentPage--; RenderPage(); } };
            Controls.Add(btnNext);
            Controls.Add(btnPrev);

            // ── NEW: Waiting Room Panel (Hidden initially) ────────────
            pnlWaitingRoom = new Panel
            {
                Location = new Point(366, 35),
                Size = new Size(693, 592),
                BackColor = Color.FromArgb(20, 40, 20),
                Visible = false
            };
            Controls.Add(pnlWaitingRoom);

            lblRoomTitle = new Label
            {
                Text = "غرفة الانتظار",
                ForeColor = Color.AntiqueWhite,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(500, 15)
            };
            pnlWaitingRoom.Controls.Add(lblRoomTitle);

            lstPlayers = new ListBox
            {
                Location = new Point(350, 70),
                Size = new Size(300, 300),
                Font = new Font("Segoe UI", 16),
                BackColor = Color.FromArgb(30, 50, 30),
                ForeColor = Color.White,
                RightToLeft = RightToLeft.Yes
            };
            pnlWaitingRoom.Controls.Add(lstPlayers);

            btnStartGame = new Button
            {
                Text = "بدء اللعبة",
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
                Text = "مغادرة الغرفة",
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

        // ================================================================
        //  SERVER MESSAGE HANDLER
        // ================================================================
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            // 1. THE THREADING FIX: Force this entire method onto the main UI thread.
            // If you don't do this, opening GameForm will silently crash.
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

        // ================================================================
        //  ROOM CARDS & STATE TRANSITIONS
        // ================================================================
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
                Text = $"غرفة: {r.RoomName}",
                AutoSize = true,
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(10, 10)
            });

            card.Controls.Add(new Label
            {
                Name = "lblCount",
                Text = $"اللاعبين: {r.CurrentCount}/{r.MaxPlayers}",
                AutoSize = true,
                Font = new Font("Tahoma", 10),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                Location = new Point(10, 60)
            });

            card.Controls.Add(new Label
            {
                Text = running ? "اللعبة جارية" : "في الانتظار",
                AutoSize = true,
                Font = new Font("Tahoma", 9, FontStyle.Italic),
                ForeColor = running ? Color.Gold : Color.LightGreen,
                BackColor = Color.Transparent,
                Location = new Point(10, 90)
            });

            var btnJoin = new Button
            {
                Name = "btnJoin",
                Text = "انضمام",
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
                Text = "مشاهدة",
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

        // ── UPDATE: Show Waiting Room State ──────────────────────────────
        private void ShowWaitingRoom(RoomStateResponse room)
        {
            flowRooms.Visible = false;
            btnNext.Visible = false;
            btnPrev.Visible = false;
            pnlCreate.Visible = false;
            pnlWaitingRoom.Visible = true;

            lblRoomTitle.Text = $"غرفة: {room.RoomName}";
            lstPlayers.Items.Clear();

            bool isOwner = false;

            // Populate connected players
            if (room.ConnectedPlayers != null && room.ConnectedPlayers.Count > 0)
            {
                // THE FIX: Check against the explicit OwnerName from the server
                isOwner = (room.OwnerName == _playerName);

                foreach (var player in room.ConnectedPlayers)
                {
                    // Assign the Host tag using the true owner name
                    string displayTag = (player == room.OwnerName) ? " (المضيف)" : "";
                    lstPlayers.Items.Add(player + displayTag);
                }
            }

            // Configure the Start button based on whether the current client is the owner
            if (isOwner)
            {
                btnStartGame.Visible = true;
                btnStartGame.Text = "بدء اللعبة";
                btnStartGame.Enabled = (room.ConnectedPlayers != null && room.ConnectedPlayers.Count >= GameConstants.MinPlayersToStart);
            }
            else
            {
                btnStartGame.Visible = true;
                btnStartGame.Text = "في انتظار المضيف...";
                btnStartGame.Enabled = false;
            }
        }

        // ── NEW: Return to Main Lobby State ───────────────────────────
        private void ReturnToLobbyView()
        {
            pnlWaitingRoom.Visible = false;
            flowRooms.Visible = true;
            btnNext.Visible = true;
            btnPrev.Visible = true;
            pnlCreate.Visible = true;
            btnCreateRoom.Enabled = true;
        }

        // ================================================================
        //  ACTIONS
        // ================================================================
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

        // ── NEW: Start Game Request ───────────────────────────────────
        private async void BtnStartGame_Click(object sender, EventArgs e)
        {
            btnStartGame.Enabled = false;
            btnStartGame.Text = "Waiting for others..."; // Better UX since both must ready up

            // THE FIX: Use the shared constant so the server understands the command
            await _client.SendAsync(GameConstants.ActionReadyUp);
        }

        // ── NEW: Leave Room Request ───────────────────────────────────
        private async void BtnLeaveRoom_Click(object sender, EventArgs e)
        {
            await _client.SendAsync(GameConstants.ActionLeaveRoom);
            ReturnToLobbyView();
            // Ask server for fresh lobby data since we left the room
            await _client.SendAsync(GameConstants.ActionJoinLobby);
        }

        // ================================================================
        //  OPEN GAME FORM
        // ================================================================
        // ================================================================
        //  OPEN GAME FORM
        // ================================================================
        private void OpenGameForm(PlayerHandResponse hand)
        {
            try
            {
                var gf = new GameController(_client, _playerName, hand);

                gf.FormClosed += async (s, e) =>
                {
                    this.Show();
                    ReturnToLobbyView();
                    // Ask for fresh lobby data when returning
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
            // Ensure this runs on the main UI thread to prevent cross-thread operation exceptions
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OpenSpectatorForm(watcherData)));
                return;
            }

            try
            {
                // We will add this new constructor to GameController in the next step
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

        // ================================================================
        //  HELPERS
        // ================================================================
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
            var tb = new TextBox { Location = new Point(x, y), Size = new Size(width, 26), Text = defaultText };
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