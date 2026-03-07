// =====================================================================
//  FILE: GameController.cs  (Client_UI)
//
//  PURPOSE
//  -------
//  Sits between the network (DominoClient) and the pure-rendering
//  GameForm. Translates server JSON events into GameForm method calls,
//  and translates GameForm user events into server messages.
//
//  GameForm stays 100% server-agnostic (good architecture).
//  GameController is the single wiring point.
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Client_UI.Network;
using Domino.Shared;
using Domino.Shared.Models.Responses;
using Game_Engine;
//using DominoGame;  // GameForm lives in this namespace

namespace Client_UI
{
    public class GameController : Form
    {
        private readonly DominoClient _client;
        private readonly string _playerName;
        private readonly GameForm _gameForm;
        private string _currentRoomId;

        // Local mirror of board state (for watcher / reconnect)
        private List<DominoTile> _board = new();
        private List<OtherPlayer> _others = new();
        private List<DominoTile> _myHand = new();
        private int _boneyard = 0;
        private bool _isMyTurn = false;
        private bool _isWatcher = false;

        public GameController(DominoClient client, string playerName,
                              PlayerHandResponse initialHand, bool isWatcher = false)
        {
            _client = client;
            _playerName = playerName;
            _isWatcher = isWatcher;

            // Host the GameForm inside this controller Form
            _gameForm = new GameForm();
            _gameForm.TopLevel = false;
            _gameForm.FormBorderStyle = FormBorderStyle.None;
            _gameForm.Dock = DockStyle.Fill;
            Controls.Add(_gameForm);
            _gameForm.Show();

            // Size this container to match GameForm
            ClientSize = _gameForm.Size;
            Text = $"Domino  –  {playerName}";
            FormBorderStyle = FormBorderStyle.Sizable;

            // ── Wire GameForm events → network calls ──────────────────
            _gameForm.TilePlaced += OnTilePlaced;
            _gameForm.DrawRequested += OnDrawRequested;
            _gameForm.PassRequested += OnPassRequested;
            _gameForm.LeaveGameRequested += OnLeaveGame;
            _gameForm.NewGameRequested += OnNewGame;

            // ── Wire network events → GameForm updates ────────────────
            _client.MessageReceived += OnMessageReceived;

            // ── Prime the UI with the hand we received at game start ──
            if (initialHand != null)
            {
                _myHand = initialHand.Hand
                    .Select(t => new DominoTile(t.Left, t.Right))
                    .ToList();
                _boneyard = initialHand.BoneyardCount;
                _isMyTurn = (initialHand.FirstTurn == playerName);

                _gameForm.InitGame(playerName, _myHand, _others, _boneyard, _isWatcher);
                UpdateTurnState();
            }
        }

        public GameController(DominoClient client, string playerName, WatcherJoinedResponse watcherData)
        {
            _client = client;
            _playerName = playerName;
            _isWatcher = true;

            // Host the GameForm inside this controller Form
            _gameForm = new GameForm();
            _gameForm.TopLevel = false;
            _gameForm.FormBorderStyle = FormBorderStyle.None;
            _gameForm.Dock = DockStyle.Fill;
            Controls.Add(_gameForm);
            _gameForm.Show();

            ClientSize = _gameForm.Size;
            Text = $"Domino (Spectator)  –  {playerName}";
            FormBorderStyle = FormBorderStyle.Sizable;

            // Allow spectators to use the Quit button
            _gameForm.LeaveGameRequested += OnLeaveGame;
            // ── Wire network events (We DO NOT wire outgoing actions for watchers)
            _client.MessageReceived += OnMessageReceived;

            // ── Prime the UI with the active board state ──
            if (watcherData != null)
            {
                // load the existing tiles on the board
                _board = watcherData.Board
                    .Select(t => new DominoTile(t.Left, t.Right))
                    .ToList();

                // set up dummy data for the hand since watchers don't have one
                _myHand = new List<DominoTile>();
                _boneyard = 0; // you can calculate this from the scores/board if needed
                _isMyTurn = false; // watchers never take turns

                // Initialize the UI in read-only mode
                _gameForm.InitGame(playerName, _myHand, _others, _boneyard, _isWatcher);
                UpdateTurnState();
            }
        }

        // ================================================================
        //  INCOMING SERVER EVENTS
        // ================================================================
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
                    // ── Opponent played a tile ────────────────────────────
                    case GameConstants.EventDominoPlayed:
                        HandleDominoPlayed(e.Payload);
                        break;

                    // ── Server confirms OUR tile was placed ───────────────
                    // (We already drew it locally; just sync booleans)

                    // ── Server sends us a tile we just drew ───────────────
                    case GameConstants.EventTileDrawn:
                        HandleTileDrawn(e.Payload);
                        break;

                    // ── Somebody drew (public boneyard count update) ──────
                    case "DrawBroadcast":
                        HandleDrawBroadcast(e.Payload);
                        break;

                    // ── A player passed ───────────────────────────────────
                    case GameConstants.EventPlayerPassed:
                        HandlePlayerPassed(e.Payload);
                        break;

                    // ── Turn changed (without a tile placement) ───────────
                    case GameConstants.EventTurnChanged:
                        HandleTurnChanged(e.Payload);
                        break;

                    // ── Round ended ───────────────────────────────────────
                    case GameConstants.EventRoundEnded:
                        HandleRoundEnded(e.Payload);
                        break;
                    // ── Server deals fresh hands for a new round ──────────
                    case GameConstants.EventTileDealt:
                        HandleNewRoundDealt(e.Payload);
                        break;
                    // ── Game over ─────────────────────────────────────────
                    case GameConstants.EventGameOver:
                        HandleGameOver(e.Payload);
                        break;

                    // ── Player disconnected mid-game ──────────────────────
                    case GameConstants.EventPlayerLeft:
                        var left = e.Payload.Deserialize<PlayerLeftResponse>(JsonOpts.Default);
                        if (left != null)
                        {
                            if (left.GameAborted)
                            {
                                MessageBox.Show("All other players left the game. The game has ended.",
                                    "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                Close(); // Close the game form and return to lobby
                            }
                            else
                            {
                                MessageBox.Show($"{left.Message}\n\nThe round will now restart.",
                                    "Player Left", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Remove the player from the local UI state
                                var opp = _others.FirstOrDefault(o => o.Name == left.PlayerName);
                                if (opp != null)
                                {
                                    _others.Remove(opp);
                                }
                                // The UI will update automatically when the server sends EventTileDealt for the new round
                            }
                        }
                        break;

                    case GameConstants.EventError:
                        MessageBox.Show(e.Payload.GetString(), "Game Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Fail gracefully if a payload is malformed
                Console.WriteLine($"[Spectator] Error parsing event {e.Action}: {ex.Message}");
            }
        }


        // ── Tile played (by any player) ───────────────────────────────
        private void HandleDominoPlayed(JsonElement p)
        {
            var resp = p.Deserialize<DominoPlayedResponse>(JsonOpts.Default);
            if (resp == null) return;

            // 1. If WE played this tile, GameForm already updated the board optimistically.
            // We just need to update whose turn it is and stop here.
            if (resp.PlayerName == _playerName)
            {
                _isMyTurn = resp.NextTurn == _playerName;
                _gameForm.UpdateGameState(_myHand, _board, _others, _boneyard, _isMyTurn);
                return;
            }

            // 2. If an OPPONENT played it, we must add it to the board AND flip it if necessary
            var tile = new DominoTile(resp.Tile.Left, resp.Tile.Right);

            if (resp.Edge.Equals("left", StringComparison.OrdinalIgnoreCase))
            {
                // Flip if the left side of the new tile matches the left side of the board
                if (_board.Count > 0 && tile.Left == _board[0].Left)
                {
                    tile = new DominoTile(tile.Right, tile.Left);
                }
                _board.Insert(0, tile);
            }
            else
            {
                // Flip if the right side of the new tile matches the right side of the board
                if (_board.Count > 0 && tile.Right == _board[_board.Count - 1].Right)
                {
                    tile = new DominoTile(tile.Right, tile.Left);
                }
                _board.Add(tile);
            }

            // 3. Update the opponent's card count so their UI hand shrinks
            var opp = _others.FirstOrDefault(o => o.Name == resp.PlayerName);
            if (opp != null)
            {
                opp.CardCount = resp.HandCount;
            }
            else
            {
                _others.Add(new OtherPlayer { Name = resp.PlayerName, CardCount = resp.HandCount });
            }

            _isMyTurn = resp.NextTurn == _playerName;
            _gameForm.UpdateGameState(_myHand, _board, _others, _boneyard, _isMyTurn);
        }

        // ── Tile drawn (private – only sent to the drawing player) ───
        private void HandleTileDrawn(JsonElement p)
        {
            var resp = p.Deserialize<TileDrawnResponse>(JsonOpts.Default);
            if (resp == null) return;

            _myHand.Add(new DominoTile(resp.DrawnTile.Left, resp.DrawnTile.Right));
            _boneyard = resp.BoneyardCount;
            _gameForm.UpdateGameState(_myHand, _board, _others, _boneyard, _isMyTurn);
        }

        private void HandleDrawBroadcast(JsonElement p)
        {
            var resp = p.Deserialize<DrawBroadcast>(JsonOpts.Default);
            if (resp == null) return;

            _boneyard = resp.BoneyardCount;

            var opp = _others.FirstOrDefault(o => o.Name == resp.PlayerName);
            if (opp != null) opp.CardCount++;

            _gameForm.UpdateGameState(_myHand, _board, _others, _boneyard, _isMyTurn);
        }

        private void HandlePlayerPassed(JsonElement p)
        {
            var resp = p.Deserialize<PlayerPassedResponse>(JsonOpts.Default);
            if (resp == null) return;
            _isMyTurn = resp.NextTurn == _playerName;
            UpdateTurnState();
        }

        private void HandleTurnChanged(JsonElement p)
        {
            if (p.TryGetProperty("NextTurn", out var nt))
                _isMyTurn = nt.GetString() == _playerName;
            UpdateTurnState();
        }

        private void HandleRoundEnded(JsonElement p)
        {
            var resp = p.Deserialize<RoundEndedResponse>(JsonOpts.Default);
            if (resp == null) return;

            // 1. Update the actual UI score state before showing the popup
            foreach (var score in resp.Scores)
            {
                if (score.PlayerName == _playerName)
                {
                    _gameForm.UpdateLocalScore(score.TotalScore); // Update your score
                }
                else
                {
                    var opp = _others.FirstOrDefault(o => o.Name == score.PlayerName);
                    if (opp != null) opp.Points = score.TotalScore; // Update opponent scores
                }
            }

            // 2. Show the popup (We DO NOT clear the board here anymore)
            var pts = resp.Scores.ToDictionary(s => s.PlayerName, s => s.TotalScore);
            _gameForm.ShowRoundResult(pts);
        }
        private void HandleNewRoundDealt(JsonElement p)
        {
            var resp = p.Deserialize<PlayerHandResponse>(JsonOpts.Default);
            if (resp == null) return;

            _myHand = resp.Hand
                .Select(t => new DominoTile(t.Left, t.Right))
                .ToList();

            _boneyard = resp.BoneyardCount;
            _isMyTurn = (resp.FirstTurn == _playerName);

            foreach (var o in _others)
            {
                o.CardCount = 7;
            }

            // THE FIX: Wipe the board here, exactly when the new round begins!
            _board.Clear();

            UpdateTurnState();
        }

        private void HandleGameOver(JsonElement p)
        {
            var resp = p.Deserialize<GameOverResponse>(JsonOpts.Default);
            if (resp == null) return;

            var pts = resp.FinalScores.ToDictionary(s => s.PlayerName, s => s.TotalScore);
            _gameForm.ShowGameResult(resp.GameWinner, pts);
        }

        // ================================================================
        //  OUTGOING PLAYER ACTIONS
        // ================================================================
        private async void OnTilePlaced(object sender, TilePlacedEventArgs e) =>
            await _client.SendAsync(GameConstants.ActionPlayDomino, new
            {
                GameId = _currentRoomId,
                TileValue1 = e.Tile.Left,
                TileValue2 = e.Tile.Right,
                TargetEdge = e.Side.ToString()
            });

        private async void OnDrawRequested(object sender, EventArgs e) =>
            await _client.SendAsync(GameConstants.ActionDrawTile,
                new { GameId = _currentRoomId });

        private void InitializeComponent()
        {

        }

        private async void OnPassRequested(object sender, EventArgs e) =>
            await _client.SendAsync(GameConstants.ActionPass,
                new { GameId = _currentRoomId });

        private bool _isLeaving = false; // Prevents sending the leave command twice

        private async void OnLeaveGame(object sender, EventArgs e)
        {
            if (_isLeaving) return;
            _isLeaving = true;

            await _client.SendAsync(GameConstants.ActionLeaveRoom);
            await Task.Delay(150); // Flush the network stream

            Close();
        }

        // ── NEW: Catches the 'X' button on the window ─────────────────
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // If the user clicked the 'X' button instead of 'Quit', tell the server!
            if (!_isLeaving)
            {
                _isLeaving = true;

                // Fire-and-forget the leave command. The TCP socket belongs to the 
                // main app, so it will successfully send even as the form closes.
                _ = _client.SendAsync(GameConstants.ActionLeaveRoom);
            }
            base.OnFormClosing(e);
        }

        private async void OnNewGame(object sender, EventArgs e) =>
            await _client.SendAsync(GameConstants.ActionReadyUp);

        // ── Refresh the GameForm with current local state ─────────────
        private void UpdateTurnState() =>
            _gameForm.UpdateGameState(_myHand, _board, _others, _boneyard, _isMyTurn);

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _client.MessageReceived -= OnMessageReceived;
            base.OnFormClosed(e);
        }
    }
}
