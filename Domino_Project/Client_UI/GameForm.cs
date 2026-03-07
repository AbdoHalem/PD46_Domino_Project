using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
#nullable disable

namespace Client_UI
{
    public partial class GameForm : Form
    {
        // ── Tile drawing sizes ────────────────────────────────────────────
        private const int TILE_W = 90;
        private const int TILE_H = 45;
        private const int PIP_R = 6;
        private const int SPACING = 6;

        // ── Tile animation ────────────────────────────────────────────────
        private System.Windows.Forms.Timer _animTimer;
        private float _animProgress = 1f;
        private DominoTile _animTile = null;
        private PlacementSide _animSide;
        private PointF _animStart, _animEnd;

        // ── Current game state (set by server via public methods) ─────────
        private List<DominoTile> _myHand = new List<DominoTile>();
        private List<DominoTile> _boardTiles = new List<DominoTile>();
        private List<OtherPlayer> _otherPlayers = new List<OtherPlayer>();
        private int _sideCardsCount = 0;
        private bool _isMyTurn = false;
        private bool _isWatcher = false;
        private string _localPlayerName = "You";
        private int _selectedIndex = -1;
        private int _myPoints = 0;

        // ── UI Controls ───────────────────────────────────────────────────
        private PictureBox picBoard;
        private Panel pnlHand;
        private Panel pnlScore;
        private Button btnDraw, btnPass, btnQuit;
        private Label lblStatus, lblSideCards;
        private Panel pnlBoardContainer;

        // ================================================================
        //  PUBLIC EVENTS
        //  The server subscribes to these to know what the player did.
        // ================================================================
        public event EventHandler<TilePlacedEventArgs> TilePlaced;
        public event EventHandler DrawRequested;
        public event EventHandler PassRequested;
        public event EventHandler LeaveGameRequested;
        public event EventHandler NewGameRequested;

        // ================================================================
        //  PUBLIC METHODS
        //  The server calls these to push state into the GUI.
        // ================================================================

        // Call once when the game starts
        public void InitGame(string playerName, List<DominoTile> hand,
                             List<OtherPlayer> opponents, int sideCards, bool isWatcher)
        {
            _localPlayerName = playerName;
            _myHand = hand;
            _otherPlayers = opponents;
            _sideCardsCount = sideCards;
            _isWatcher = isWatcher;
            _boardTiles.Clear();
            _selectedIndex = -1;
            RefreshUI();
        }

        // Call every time something changes (tile placed, drawn, passed)
        public void UpdateGameState(List<DominoTile> myHand, List<DominoTile> board,
                                    List<OtherPlayer> opponents, int sideCards, bool isMyTurn)
        {
            _myHand = myHand;
            _boardTiles = board;
            _otherPlayers = opponents;
            _sideCardsCount = sideCards;
            _isMyTurn = isMyTurn;
            _selectedIndex = -1;
            RefreshUI();
        }

        // Call when a round ends
        public void ShowRoundResult(Dictionary<string, int> points)
        {
            string msg = "Round Over!\n\n";
            foreach (var kv in points) msg += $"  {kv.Key}: {kv.Value} pts\n";
            MessageBox.Show(msg, "Round Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Call when the whole game ends
        public void ShowGameResult(string winner, Dictionary<string, int> finalPoints)
        {
            string msg = $"Winner:  {winner}\n\nFinal Scores:\n";
            foreach (var kv in finalPoints) msg += $"  {kv.Key}: {kv.Value} pts\n";
            var r = MessageBox.Show(msg + "\nPlay again?", "Game Over",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes) NewGameRequested?.Invoke(this, EventArgs.Empty);
            else LeaveGameRequested?.Invoke(this, EventArgs.Empty);
        }

        // ================================================================
        //  CONSTRUCTOR
        // ================================================================

        public GameForm()
        {
            InitializeComponent2();
            BuildLayout();
            SetupAnimation();
        }

        // ================================================================
        //  LAYOUT
        // ================================================================

        private void BuildLayout()
        {
            Text = "Domino";
            Size = new Size(1200, 780);
            MinimumSize = new Size(1000, 680);
            BackColor = Color.FromArgb(22, 68, 22);
            DoubleBuffered = true;

            lblStatus = new Label
            {
                Text = "Waiting game...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            Controls.Add(lblStatus);

            lblSideCards = new Label
            {
                Text = "Side cards: 0",
                ForeColor = Color.LightYellow,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(12, 42)
            };
            Controls.Add(lblSideCards);

            pnlBoardContainer = new Panel
            {
                Location = new Point(0, 68),
                Size = new Size(940, 220),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            picBoard = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(940, 200),
                BackColor = Color.Transparent
            };
            picBoard.Paint += PicBoard_Paint;

            pnlBoardContainer.Controls.Add(picBoard);
            Controls.Add(pnlBoardContainer);

            pnlScore = new Panel
            {
                Location = new Point(950, 68),
                Size = new Size(235, 370),
                BackColor = Color.FromArgb(15, 50, 15)
            };
            pnlScore.Paint += PnlScore_Paint;
            Controls.Add(pnlScore);

            pnlHand = new Panel
            {
                Location = new Point(0, 560),
                Size = new Size(940, 180),
                BackColor = Color.Transparent
            };
            pnlHand.Paint += PnlHand_Paint;
            pnlHand.MouseClick += PnlHand_Click;
            Controls.Add(pnlHand);

            btnDraw = Btn("Draw", new Point(960, 450), Color.FromArgb(30, 100, 200));
            btnPass = Btn("Pass", new Point(960, 510), Color.FromArgb(180, 90, 20));
            btnQuit = Btn("Quit", new Point(960, 580), Color.FromArgb(140, 20, 20));

            btnDraw.Click += (s, e) => DrawRequested?.Invoke(this, EventArgs.Empty);
            btnPass.Click += (s, e) => PassRequested?.Invoke(this, EventArgs.Empty);
            btnQuit.Click += OnQuit;

            Controls.Add(btnDraw);
            Controls.Add(btnPass);
            Controls.Add(btnQuit);

            Resize += (s, e) => {
                int w = Math.Max(700, ClientSize.Width - 260);
                pnlBoardContainer.Width = w;
                pnlHand.Width = w;
                pnlScore.Left = ClientSize.Width - 245;
                btnDraw.Left = btnPass.Left = btnQuit.Left = ClientSize.Width - 235;
                RefreshUI();
            };
        }

        private Button Btn(string text, Point loc, Color c) => new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(220, 48),
            BackColor = c,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };

        // ================================================================
        //  ANIMATION  –  tile flies from hand to board when placed
        // ================================================================

        private void SetupAnimation()
        {
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) => {
                _animProgress += 0.08f;
                if (_animProgress >= 1f) { _animProgress = 1f; _animTimer.Stop(); _animTile = null; }
                picBoard.Invalidate();
            };
        }

        private void StartAnimation(DominoTile tile, PlacementSide side)
        {
            _animTile = tile;
            _animSide = side;
            _animProgress = 0f;
            _animStart = new PointF(400, pnlHand.Top + 50);

            int boardY = picBoard.Height / 2 - TILE_H / 2;
            int totalW = _boardTiles.Count * (TILE_W + 4);
            int sx = Math.Max(10, (picBoard.Width - totalW) / 2);

            _animEnd = side == PlacementSide.Right
                ? new PointF(sx + totalW - TILE_W - 4, boardY)
                : new PointF(sx, boardY);

            _animTimer.Start();
        }

        // ================================================================
        //  REFRESH UI  –  updates all labels, button states, repaints
        // ================================================================

        private void RefreshUI()
        {
            if (InvokeRequired) { Invoke((Action)RefreshUI); return; }

            if (_isWatcher) lblStatus.Text = "Watching";
            else if (_isMyTurn) lblStatus.Text = $"Your turn,  {_localPlayerName}!";
            else lblStatus.Text = "Opponent's turn...";

            lblSideCards.Text = $"Side cards: {_sideCardsCount}";

            bool myTurn = _isMyTurn && !_isWatcher;

            bool hasValidMove = false;
            if (_boardTiles.Count == 0) hasValidMove = true;
            else
            {
                int le = _boardTiles[0].Left;
                int re = _boardTiles[_boardTiles.Count - 1].Right;
                foreach (var tile in _myHand)
                {
                    if (tile.CanPlayOn(le) || tile.CanPlayOn(re))
                    {
                        hasValidMove = true;
                        break;
                    }
                }
            }

            btnDraw.Enabled = myTurn && !hasValidMove && _sideCardsCount > 0;
            btnPass.Enabled = myTurn && !hasValidMove && _sideCardsCount == 0;
            btnDraw.BackColor = btnDraw.Enabled ? Color.FromArgb(30, 100, 200) : Color.FromArgb(70, 70, 70);
            btnPass.BackColor = btnPass.Enabled ? Color.FromArgb(180, 90, 20) : Color.FromArgb(70, 70, 70);

            picBoard.Invalidate();
            pnlHand.Invalidate();
            pnlScore.Invalidate();
            RebuildOpponentPanels();

            int requiredWidth = Math.Max(pnlBoardContainer.Width, _boardTiles.Count * (TILE_W + 4) + 40);
            if (picBoard.Width != requiredWidth)
            {
                picBoard.Width = requiredWidth;
            }
        }

        // ================================================================
        //  PAINT – Board (placed tile chain)
        // ================================================================

        private void PicBoard_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_boardTiles.Count == 0)
            {
                using var p = new Pen(Color.FromArgb(80, 255, 255, 255), 2);
                p.DashStyle = DashStyle.Dash;
                int cx = picBoard.Width / 2, cy = picBoard.Height / 2;
                g.DrawRectangle(p, cx - 120, cy - 28, 240, 56);
                g.DrawString("Place your first tile here",
                    new Font("Segoe UI", 10), Brushes.White, cx - 100, cy - 10);
                return;
            }

            int totalW = _boardTiles.Count * (TILE_W + 4);
            int startX = Math.Max(10, (picBoard.Width - totalW) / 2);
            int y = picBoard.Height / 2 - TILE_H / 2;

            for (int i = 0; i < _boardTiles.Count; i++)
                DrawTile(g, _boardTiles[i], startX + i * (TILE_W + 4), y,
                         faceUp: true, selected: false, active: true, glow: false);

            if (_animTile != null && _animProgress < 1f)
            {
                float ax = _animStart.X + (_animEnd.X - _animStart.X) * _animProgress;
                float ay = _animStart.Y + (_animEnd.Y - _animStart.Y) * _animProgress;
                DrawTile(g, _animTile, (int)ax, (int)ay,
                         faceUp: true, selected: true, active: true, glow: false);
            }
        }

        // ================================================================
        //  PAINT – Player Hand (bottom area)
        // ================================================================

        private void PnlHand_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.FillRectangle(new SolidBrush(Color.FromArgb(60, 0, 0, 0)), 0, 0, pnlHand.Width, 30);
            g.DrawString(_isWatcher ? "Watching" : $"Your Hand  –  {_localPlayerName}",
                new Font("Segoe UI", 10, FontStyle.Bold), Brushes.LightYellow, 8, 7);

            int sx = 10, ty = 44;

            // Highlight tiles that are valid moves
            var validSet = new HashSet<int>();
            if (_isMyTurn && !_isWatcher && _boardTiles.Count > 0)
            {
                int le = _boardTiles[0].Left;
                int re = _boardTiles[_boardTiles.Count - 1].Right;
                for (int i = 0; i < _myHand.Count; i++)
                    if (_myHand[i].CanPlayOn(le) || _myHand[i].CanPlayOn(re))
                        validSet.Add(i);
            }

            for (int i = 0; i < _myHand.Count; i++)
            {
                bool sel = (i == _selectedIndex);
                DrawTile(g, _myHand[i], sx + i * (TILE_W + SPACING),
                         sel ? ty - 12 : ty,
                         faceUp: !_isWatcher, selected: sel,
                         active: _isMyTurn && !_isWatcher,
                         glow: validSet.Contains(i) && !sel);
            }

            // Boneyard pile
            if (_sideCardsCount > 0 && !_isWatcher)
            {
                int bx = sx + _myHand.Count * (TILE_W + SPACING) + 20;
                int stack = Math.Min(_sideCardsCount, 6);
                for (int i = stack - 1; i >= 0; i--)
                    DrawTile(g, null, bx + i * 4, ty - i * 2,
                             faceUp: false, selected: false, active: false, glow: false);
                g.DrawString($"x{_sideCardsCount}",
                    new Font("Segoe UI", 9, FontStyle.Bold), Brushes.LightYellow,
                    bx + 2, ty + TILE_H + 6);
            }
        }

        // ================================================================
        //  PAINT – Score Panel (right side)
        // ================================================================

        private void PnlScore_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawString("Scoreboard", new Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, 10, 10);
            g.DrawLine(new Pen(Color.FromArgb(80, 255, 255, 255)), 10, 36, 220, 36);

            DrawScoreRow(g, _localPlayerName + " (You)", _myPoints, _myHand.Count, _isMyTurn, 50);
            for (int i = 0; i < _otherPlayers.Count; i++)
                DrawScoreRow(g, _otherPlayers[i].Name, _otherPlayers[i].Points,
                             _otherPlayers[i].CardCount, _otherPlayers[i].IsActive, 50 + (i + 1) * 62);
        }

        private void DrawScoreRow(Graphics g, string name, int pts, int cards, bool active, int y)
        {
            if (active)
            {
                using var hl = new SolidBrush(Color.FromArgb(45, 255, 220, 0));
                g.FillRectangle(hl, 4, y - 2, 226, 54);
            }
            DrawAvatar(g, name, 8, y + 7, 34);
            g.DrawString(name, new Font("Segoe UI", 9, FontStyle.Bold),
                new SolidBrush(active ? Color.Yellow : Color.White), 50, y + 5);
            g.DrawString($"{pts} pts   {cards} cards",
                new Font("Segoe UI", 8), Brushes.LightGray, 50, y + 23);
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 255, 255, 255)), 50, y + 42, 160, 6);
            int fill = Math.Min(160, pts * 2);
            if (fill > 0)
                g.FillRectangle(new SolidBrush(active ? Color.Gold : Color.SteelBlue),
                                50, y + 42, fill, 6);
        }

        private void DrawAvatar(Graphics g, string name, int x, int y, int size)
        {
            Color[] colours = {
                Color.FromArgb(70,130,180), Color.FromArgb(180,70,100),
                Color.FromArgb(60,160,60),  Color.FromArgb(160,120,40),
                Color.FromArgb(130,70,180)
            };
            int idx = name.Length > 0 ? Math.Abs(char.ToUpper(name[0]) - 'A') % colours.Length : 0;
            g.FillEllipse(new SolidBrush(colours[idx]), x, y, size, size);
            g.DrawEllipse(Pens.White, x, y, size, size);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(name.Length > 0 ? name[0].ToString().ToUpper() : "?",
                new Font("Segoe UI", size * 0.38f, FontStyle.Bold),
                Brushes.White, new RectangleF(x, y, size, size), sf);
        }

        // ================================================================
        //  OPPONENT PANELS  (vertical face-down tiles, top area)
        // ================================================================

        private void RebuildOpponentPanels()
        {
            foreach (Control c in Controls.OfType<Panel>().ToList())
                if (c.Tag is string t && t == "opp") Controls.Remove(c);

            if (_otherPlayers.Count == 0) return;

            int availW = Math.Max(700, ClientSize.Width - 260);
            int panW = availW / _otherPlayers.Count;

            for (int i = 0; i < _otherPlayers.Count; i++)
            {
                var opp = _otherPlayers[i];
                int captW = panW - 6;
                var pnl = new Panel
                {
                    Location = new Point(i * panW, 295),
                    Size = new Size(captW, 255),
                    BackColor = Color.Transparent,
                    Tag = "opp"
                };
                var captOpp = opp;
                pnl.Paint += (s, ev) => DrawOpponent(ev.Graphics, captOpp, captW);
                Controls.Add(pnl);
                pnl.BringToFront();
            }
        }

        private void DrawOpponent(Graphics g, OtherPlayer opp, int w)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float a = opp.IsActive ? 1f : 0.5f;

            DrawAvatar(g, opp.Name, 6, 4, 34);
            g.DrawString(opp.Name, new Font("Segoe UI", 9, FontStyle.Bold),
                new SolidBrush(opp.IsActive ? Color.Yellow
                    : Color.FromArgb((int)(a * 255), Color.White)), 46, 6);
            g.DrawString($"{opp.CardCount} cards  |  {opp.Points} pts",
                new Font("Segoe UI", 8),
                new SolidBrush(Color.FromArgb(200, Color.White)), 46, 24);

            int tw = 28, th = 56, gap = 5;
            int totalW = opp.CardCount * (tw + gap);
            int sx = Math.Max(4, (w - totalW) / 2);

            for (int i = 0; i < opp.CardCount; i++)
            {
                int tx = sx + i * (tw + gap);
                g.FillRectangle(new SolidBrush(Color.FromArgb((int)(a * 50), 0, 0, 0)),
                                tx + 3, 47, tw, th);
                using var cp = RoundedRect(tx, 44, tw, th, 4);
                g.FillPath(new SolidBrush(Color.FromArgb((int)(a * 220), 35, 55, 140)), cp);
                g.DrawPath(new Pen(opp.IsActive
                    ? Color.FromArgb((int)(a * 255), Color.Gold)
                    : Color.FromArgb((int)(a * 160), Color.White),
                    opp.IsActive ? 2f : 1f), cp);
                g.DrawLine(new Pen(Color.FromArgb(60, 255, 255, 255), 1),
                           tx + 3, 44 + th / 2, tx + tw - 3, 44 + th / 2);
            }
        }

        // ================================================================
        //  TILE DRAWING
        // ================================================================

        private void DrawTile(Graphics g, DominoTile tile, int x, int y,
                               bool faceUp, bool selected, bool active, bool glow)
        {
            if (glow)
                for (int s = 8; s >= 2; s -= 3)
                {
                    using var gp = new Pen(Color.FromArgb(35, 0, 255, 90), s * 2);
                    gp.LineJoin = LineJoin.Round;
                    using var rp = RoundedRect(x - s, y - s, TILE_W + s * 2, TILE_H + s * 2, 7);
                    g.DrawPath(gp, rp);
                }

            using var shPath = RoundedRect(x + 3, y + 4, TILE_W, TILE_H, 6);
            g.FillPath(new SolidBrush(Color.FromArgb(70, 0, 0, 0)), shPath);

            Color bg = !faceUp ? Color.FromArgb(45, 65, 155)
                     : selected ? Color.LightGoldenrodYellow
                     : !active ? Color.FromArgb(175, 175, 160)
                     : Color.FromArgb(248, 245, 230);

            using var tPath = RoundedRect(x, y, TILE_W, TILE_H, 6);
            g.FillPath(new SolidBrush(bg), tPath);

            Color bc = selected ? Color.Gold : glow ? Color.FromArgb(80, 210, 80) : Color.FromArgb(70, 50, 30);
            float bw = selected ? 3f : glow ? 2f : 1.5f;
            g.DrawPath(new Pen(bc, bw), tPath);

            if (!faceUp || tile == null) return;

            g.DrawLine(new Pen(Color.FromArgb(90, 70, 50), 1.5f),
                       x + TILE_W / 2, y + 5, x + TILE_W / 2, y + TILE_H - 5);

            DrawPips(g, tile.Left, x + 2, y + 2, TILE_W / 2 - 4, TILE_H - 4, active);
            DrawPips(g, tile.Right, x + TILE_W / 2 + 2, y + 2, TILE_W / 2 - 4, TILE_H - 4, active);
        }

        private void DrawPips(Graphics g, int value, int rx, int ry, int rw, int rh, bool active)
        {
            Color pc = active ? Color.FromArgb(25, 15, 5) : Color.FromArgb(130, 120, 110);
            foreach (var pt in GetPipPositions(value, rx, ry, rw, rh))
                g.FillEllipse(new SolidBrush(pc), pt.X - PIP_R, pt.Y - PIP_R, PIP_R * 2, PIP_R * 2);
        }

        private List<Point> GetPipPositions(int v, int rx, int ry, int rw, int rh)
        {
            var list = new List<Point>();
            int cx = rx + rw / 2, cy = ry + rh / 2, dx = rw / 3, dy = rh / 3;
            switch (v)
            {
                case 1: list.Add(P(cx, cy)); break;
                case 2: list.Add(P(cx - dx, cy - dy)); list.Add(P(cx + dx, cy + dy)); break;
                case 3: list.Add(P(cx - dx, cy - dy)); list.Add(P(cx, cy)); list.Add(P(cx + dx, cy + dy)); break;
                case 4:
                    list.Add(P(cx - dx, cy - dy)); list.Add(P(cx + dx, cy - dy));
                    list.Add(P(cx - dx, cy + dy)); list.Add(P(cx + dx, cy + dy)); break;
                case 5:
                    list.Add(P(cx - dx, cy - dy)); list.Add(P(cx + dx, cy - dy)); list.Add(P(cx, cy));
                    list.Add(P(cx - dx, cy + dy)); list.Add(P(cx + dx, cy + dy)); break;
                case 6:
                    list.Add(P(cx - dx, cy - dy)); list.Add(P(cx + dx, cy - dy));
                    list.Add(P(cx - dx, cy)); list.Add(P(cx + dx, cy));
                    list.Add(P(cx - dx, cy + dy)); list.Add(P(cx + dx, cy + dy)); break;
            }
            return list;
        }

        private Point P(int x, int y) => new Point(x, y);

        private GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(x, y, r * 2, r * 2, 180, 90);
            p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ================================================================
        //  MOUSE INPUT
        //  Click once to select a tile, click again to place it.
        // ================================================================

        private void PnlHand_Click(object sender, MouseEventArgs e)
        {
            if (!_isMyTurn || _isWatcher) return;

            int sx = 10, ty = 44;
            for (int i = 0; i < _myHand.Count; i++)
            {
                var rect = new Rectangle(sx + i * (TILE_W + SPACING), ty - 16, TILE_W, TILE_H + 22);
                if (rect.Contains(e.Location))
                {
                    if (_selectedIndex == i) TryPlaceTile(i);
                    else { _selectedIndex = i; pnlHand.Invalidate(); }
                    return;
                }
            }
            _selectedIndex = -1;
            pnlHand.Invalidate();
        }

        private void TryPlaceTile(int handIndex)
        {
            var tile = _myHand[handIndex];

            if (_boardTiles.Count == 0)
            {
                DoPlace(handIndex, PlacementSide.Right);
                return;
            }

            int le = _boardTiles[0].Left;
            int re = _boardTiles[_boardTiles.Count - 1].Right;
            bool canL = tile.CanPlayOn(le);
            bool canR = tile.CanPlayOn(re);

            if (!canL && !canR)
            {
                MessageBox.Show($"Tile {tile} doesn't fit either end.\nDraw a tile or Pass.",
                    "Can't Place", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _selectedIndex = -1; pnlHand.Invalidate(); return;
            }

            if (canL && !canR) { DoPlace(handIndex, PlacementSide.Left); return; }
            if (canR && !canL) { DoPlace(handIndex, PlacementSide.Right); return; }

            using var dlg = new PlacementDialog(tile, _boardTiles);
            if (dlg.ShowDialog(this) == DialogResult.OK) DoPlace(handIndex, dlg.ChosenSide);
            else { _selectedIndex = -1; pnlHand.Invalidate(); }
        }

        private void DoPlace(int handIndex, PlacementSide side)
        {
            var tile = _myHand[handIndex];

            if (side == PlacementSide.Left && _boardTiles.Count > 0)
            {
                int le = _boardTiles[0].Left;
                if (tile.Left == le) tile = new DominoTile(tile.Right, tile.Left);
                _boardTiles.Insert(0, tile);
            }
            else
            {
                if (_boardTiles.Count > 0)
                {
                    int re = _boardTiles[_boardTiles.Count - 1].Right;
                    if (tile.Right == re) tile = new DominoTile(tile.Right, tile.Left);
                }
                _boardTiles.Add(tile);
            }

            StartAnimation(tile, side);
            _myHand.RemoveAt(handIndex);
            _selectedIndex = -1;

            TilePlaced?.Invoke(this, new TilePlacedEventArgs(tile, side));
            RefreshUI();
        }

        private void OnQuit(object sender, EventArgs e)
        {
            if (MessageBox.Show("Leave the game?", "Quit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                LeaveGameRequested?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateLocalScore(int pts)
        {
            _myPoints = pts;
            pnlScore.Invalidate();
        }

        private void InitializeComponent2() { }
    }

    // ================================================================
    //  SHARED DATA CLASSES
    //  Used by all members. Must not be renamed or restructured.
    // ================================================================

    public class DominoTile
    {
        public int Left { get; set; }
        public int Right { get; set; }
        public DominoTile(int l, int r) { Left = l; Right = r; }
        public bool CanPlayOn(int end) => Left == end || Right == end;
        public override string ToString() => $"[{Left}|{Right}]";
    }

    public class OtherPlayer
    {
        public string Name { get; set; }
        public int CardCount { get; set; }
        public int Points { get; set; }
        public bool IsActive { get; set; }
    }

    public enum PlacementSide { Left, Right }

    public class TilePlacedEventArgs : EventArgs
    {
        public DominoTile Tile { get; }
        public PlacementSide Side { get; }
        public TilePlacedEventArgs(DominoTile t, PlacementSide s) { Tile = t; Side = s; }
    }

    // ================================================================
    //  PLACEMENT DIALOG
    //  Shown when a tile fits BOTH ends of the board.
    // ================================================================

    public class PlacementDialog : Form
    {
        public PlacementSide ChosenSide { get; private set; } = PlacementSide.Right;

        public PlacementDialog(DominoTile tile, List<DominoTile> board)
        {
            Text = "Where to place?"; Size = new Size(360, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Color.FromArgb(22, 68, 22);

            int le = board[0].Left, re = board[board.Count - 1].Right;

            Controls.Add(new Label
            {
                Text = $"Tile: {tile}\n\nLeft end: [{le}]         Right end: [{re}]",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(15, 12)
            });

            var bL = DlgBtn($"<-- Left  (end {le})", new Point(15, 100), tile.CanPlayOn(le));
            var bR = DlgBtn($"Right (end {re}) -->", new Point(190, 100), tile.CanPlayOn(re));
            bL.Click += (s, e) => { ChosenSide = PlacementSide.Left; DialogResult = DialogResult.OK; Close(); };
            bR.Click += (s, e) => { ChosenSide = PlacementSide.Right; DialogResult = DialogResult.OK; Close(); };
            Controls.Add(bL); Controls.Add(bR);

            Controls.Add(new Button
            {
                Text = "Cancel",
                Location = new Point(135, 155),
                Size = new Size(80, 28),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(120, 30, 30)
            });
        }

        private Button DlgBtn(string text, Point loc, bool on) => new Button
        {
            Text = text,
            Location = loc,
            Size = new Size(150, 44),
            Enabled = on,
            BackColor = on ? Color.FromArgb(30, 100, 200) : Color.FromArgb(70, 70, 70),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
    }
}
