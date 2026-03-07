// =====================================================================
//  FILE: RoomHandlers.cs  (Domino.Server/Handlers)
//
//  CHANGES vs original
//  -------------------
//  • Added CreateRoom handler (was completely missing).
//  • Added WatchRoom handler (required by spec).
//  • JoinRoom now sends the full player list, not just the new player.
//  • Added ReadyUp handler – starts the game once all players ready.
//  • LeaveRoom handler to clean up gracefully.
//  • All responses use the shared GameConstants event strings.
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Connection.Engine.Network;
using Connection.Engine.Router;
using Domino.Engine.State;
using Domino.Server.State;
using Domino.Shared;
using Domino.Shared.Models.Requests;
using Domino.Shared.Models.Responses;

namespace Domino.Server.Handlers
{
    public class RoomHandlers : IMessageHandler
    {
        private readonly GroupManager _groupManager;
        private readonly GameManager _gameManager;
        private readonly ConnectionRegistry _connectionRegistry;

        public RoomHandlers(GroupManager groupManager, GameManager gameManager, ConnectionRegistry connectionRegistry)
        {
            _groupManager = groupManager;
            _gameManager = gameManager;
            _connectionRegistry = connectionRegistry; // Save the injected dependency
        }

        // ── Create a new room ─────────────────────────────────────────
        [MessageRoute(GameConstants.ActionCreateRoom)]
        public async Task HandleCreateRoomAsync(PlayerConnection player, JsonElement payload)
        {
            var req = payload.Deserialize<CreateRoomRequest>(JsonOpts.Default);

            if (req == null || string.IsNullOrWhiteSpace(req.RoomName))
            {
                await SendError(player, "Invalid room creation request.");
                return;
            }

            int maxPlayers = Math.Clamp(req.MaxPlayers, 2, GameConstants.MaxPlayersPerRoom);
            int scoreLimit = req.ScoreLimit > 0 ? req.ScoreLimit : 100;

            var record = _gameManager.CreateRoom(player.ConnectionId, req.RoomName, maxPlayers, scoreLimit);

            // Move creator out of global lobby and into the room's socket group
            _groupManager.RemoveFromGroup("Lobby", player);
            _groupManager.AddToGroup($"Room_{record.RoomId}", player);

            // Tell everyone in the lobby about the new room
            await _groupManager.BroadcastToGroupAsync("Lobby",
                Envelope(GameConstants.EventLobbySnapshot, BuildLobbySnapshot()));

            // Tell the creator their room details
            await player.SendMessageAsync(
                Envelope(GameConstants.EventRoomSnapshot, BuildRoomState(record)));

            Console.WriteLine($"[Rooms] {_gameManager.GetPlayerName(player.ConnectionId)} created '{req.RoomName}'");
        }

        // ── Join an existing room ─────────────────────────────────────
        [MessageRoute(GameConstants.ActionJoinRoom)]
        public async Task HandleJoinRoomAsync(PlayerConnection player, JsonElement payload)
        {
            var req = payload.Deserialize<JoinRoomRequest>(JsonOpts.Default);

            if (req == null || string.IsNullOrWhiteSpace(req.RoomId))
            {
                await SendError(player, "Invalid room ID.");
                return;
            }

            var record = _gameManager.JoinRoom(player.ConnectionId, req.RoomId);
            if (record == null)
            {
                await SendError(player, "Room not found, is full, or game already in progress.");
                return;
            }

            string roomGroup = $"Room_{record.RoomId}";
            _groupManager.RemoveFromGroup("Lobby", player);
            _groupManager.AddToGroup(roomGroup, player);

            // BUG FIX (original code): broadcast the FULL player list, not just the new joiner
            var snapshot = BuildRoomState(record);
            await _groupManager.BroadcastToGroupAsync(roomGroup,
                Envelope(GameConstants.EventRoomSnapshot, snapshot));

            // Update lobby so the room card shows correct count
            await _groupManager.BroadcastToGroupAsync("Lobby",
                Envelope(GameConstants.EventLobbySnapshot, BuildLobbySnapshot()));

            Console.WriteLine($"[Rooms] {_gameManager.GetPlayerName(player.ConnectionId)} joined '{record.RoomName}'");
        }

        // ── Watch (spectate) a room ───────────────────────────────────
        [MessageRoute(GameConstants.ActionWatchRoom)]
        public async Task HandleWatchRoomAsync(PlayerConnection player, JsonElement payload)
        {
            var req = payload.Deserialize<WatchRoomRequest>(JsonOpts.Default);
            if (req == null) { await SendError(player, "Bad request."); return; }

            var record = _gameManager.GetRoom(req.RoomId);
            if (record == null) { await SendError(player, "Room not found."); return; }

            _gameManager.AddWatcher(player.ConnectionId, req.RoomId);

            string roomGroup = $"Room_{record.RoomId}";
            _groupManager.RemoveFromGroup("Lobby", player);
            _groupManager.AddToGroup(roomGroup, player);

            var engine = _gameManager.GetEngine(req.RoomId);

            // If game is running, send current board state to the watcher
            if (engine != null)
            {
                var watcherPayload = new WatcherJoinedResponse
                {
                    RoomName = record.RoomName,
                    Board = engine.Board.PlayedCards
                                    .Select(t => new TileDto { Left = t.LeftSide, Right = t.RightSide })
                                    .ToList(),
                    CurrentTurn = engine.CurrentPlayer.PlayerName,
                    Scores = engine.Players
                                    .Select(p => new PlayerScoreDto
                                    {
                                        PlayerName = p.PlayerName,
                                        TotalScore = p.TotalScore
                                    }).ToList()
                };
                await player.SendMessageAsync(Envelope(GameConstants.EventWatcherJoined, watcherPayload));
            }
            else
            {
                // Game hasn't started yet; just send the room roster
                await player.SendMessageAsync(
                    Envelope(GameConstants.EventRoomSnapshot, BuildRoomState(record)));
            }
        }

        // ── Player signals ready to play ──────────────────────────────
        // ── Player signals ready to play ──────────────────────────────
        [MessageRoute(GameConstants.ActionReadyUp)]
        public async Task HandleReadyUpAsync(PlayerConnection player, JsonElement payload)
        {
            string roomId = _gameManager.FindRoomIdByConnection(player.ConnectionId);
            if (roomId == null) { await SendError(player, "You are not in a room."); return; }

            var record = _gameManager.GetRoom(roomId);
            if (record == null || !record.IsReady) return;

            // Only the room owner can trigger game start (or you can auto-start when full)
            bool ownerReadied = player.ConnectionId == record.OwnerId;
            bool roomFull = record.IsFull;

            if (!ownerReadied && !roomFull) return;

            // Start the engine
            var engine = _gameManager.StartGame(roomId);
            string roomGroup = $"Room_{roomId}";

            // Wire server-side game events
            engine.OnTurnChanged += async (nextPlayerName) =>
            {
                await _groupManager.BroadcastToGroupAsync(roomGroup,
                    Envelope(GameConstants.EventTurnChanged, new { NextTurn = nextPlayerName }));
            };

            engine.OnRoundEnded += async (winnerName) =>
            {
                // ── THE FIX: Add a 250ms delay to prevent race conditions ──
                // This gives MatchHandlers time to broadcast the final DominoPlayed event 
                // BEFORE we wipe the board and distribute the new cards.
                await Task.Delay(250);

                var roundResp = new RoundEndedResponse
                {
                    RoundWinner = winnerName,
                    Scores = engine.Players.Select(p => new PlayerScoreDto
                    {
                        PlayerName = p.PlayerName,
                        TotalScore = p.TotalScore
                    }).ToList(),
                    GameOver = engine.IsGameOver
                };
                await _groupManager.BroadcastToGroupAsync(roomGroup,
                    Envelope(GameConstants.EventRoundEnded, roundResp));

                // ── Deal new hands if the match continues ──────────────
                if (!engine.IsGameOver)
                {
                    // Trigger the engine to clear old cards, shuffle the bank, and deal!
                    engine.StartNewRound();

                    foreach (var kvp in record.Players)
                    {
                        string connId = kvp.Key;
                        string pName = kvp.Value;

                        var pState = engine.Players.FirstOrDefault(p => p.PlayerName == pName);
                        if (pState == null) continue;

                        var conn = GetConnectionFromGroup(roomGroup, connId);
                        if (conn == null) continue;

                        var hand = pState.Cards
                            .Select(t => new TileDto { Left = t.LeftSide, Right = t.RightSide })
                            .ToList();

                        // Send the EventTileDealt message to update their UI
                        await conn.SendMessageAsync(Envelope(GameConstants.EventTileDealt,
                            new PlayerHandResponse
                            {
                                PlayerName = pName,
                                Hand = hand,
                                BoneyardCount = engine.Boneyard.Count,
                                FirstTurn = engine.CurrentPlayer.PlayerName
                            }));
                    }
                }
            };

            engine.OnGameOver += async (winnerName) =>
            {
                engine.SaveResultToFile();
                var gameOverResp = new GameOverResponse
                {
                    GameWinner = winnerName,
                    FinalScores = engine.Players.Select(p => new PlayerScoreDto
                    {
                        PlayerName = p.PlayerName,
                        TotalScore = p.TotalScore
                    }).ToList()
                };
                await _groupManager.BroadcastToGroupAsync(roomGroup,
                    Envelope(GameConstants.EventGameOver, gameOverResp));
                _gameManager.EndGame(roomId);
            };

            // Send each player their private hand for the FIRST round
            foreach (var kvp in record.Players)
            {
                string connId = kvp.Key;
                string pName = kvp.Value;
                var pState = engine.Players.FirstOrDefault(p => p.PlayerName == pName);
                if (pState == null) continue;

                var conn = GetConnectionFromGroup(roomGroup, connId);
                if (conn == null) continue;

                var hand = pState.Cards
                    .Select(t => new TileDto { Left = t.LeftSide, Right = t.RightSide })
                    .ToList();

                await conn.SendMessageAsync(Envelope(GameConstants.EventGameStarted,
                    new PlayerHandResponse
                    {
                        PlayerName = pName,
                        Hand = hand,
                        BoneyardCount = engine.Boneyard.Count,
                        FirstTurn = engine.CurrentPlayer.PlayerName
                    }));
            }

            Console.WriteLine($"[Rooms] Game started in '{record.RoomName}'. First turn: {engine.CurrentPlayer.PlayerName}");
        }

        // ── Player voluntarily leaves room ────────────────────────────
        // ── Player voluntarily leaves room or disconnects ────────────────────────────
        [MessageRoute(GameConstants.ActionLeaveRoom)]
        public async Task HandleLeaveRoomAsync(PlayerConnection player, JsonElement payload)
        {
            string connId = player.ConnectionId;
            string name = _gameManager.GetPlayerName(connId);

            // RemovePlayer now handles Host Migration internally if the host leaves!
            _gameManager.RemovePlayer(connId, out string roomId);
            if (roomId == null) return;

            string roomGroup = $"Room_{roomId}";
            _groupManager.RemoveFromGroup(roomGroup, player);
            _groupManager.AddToGroup("Lobby", player);

            var engine = _gameManager.GetEngine(roomId);
            var roomRecord = _gameManager.GetRoom(roomId);

            // If the game is currently active, we need special handling
            if (engine != null && roomRecord != null && roomRecord.GameRunning)
            {
                if (roomRecord.Players.Count <= 1)
                {
                    // Only 1 player left. Abort the game entirely.
                    await _groupManager.BroadcastToGroupAsync(roomGroup,
                        Envelope(GameConstants.EventPlayerLeft, new PlayerLeftResponse
                        {
                            PlayerName = name,
                            Message = $"{name} left. Not enough players to continue.",
                            GameAborted = true
                        }));

                    _gameManager.EndGame(roomId);
                    _gameManager.RemoveRoom(roomId);
                }
                else
                {
                    // 2+ players remain. Restart the round but keep scores.
                    engine.RemovePlayerKeepScore(name);

                    await _groupManager.BroadcastToGroupAsync(roomGroup,
                        Envelope(GameConstants.EventPlayerLeft, new PlayerLeftResponse
                        {
                            PlayerName = name,
                            Message = $"{name} has left the game. The round will restart.",
                            GameAborted = false
                        }));

                    // Wipe the board and deal fresh hands
                    engine.StartNewRound();

                    // Distribute the new private hands to the remaining players
                    foreach (var kvp in roomRecord.Players)
                    {
                        string pConnId = kvp.Key;
                        string pName = kvp.Value;

                        var pState = engine.Players.FirstOrDefault(p => p.PlayerName == pName);
                        if (pState == null) continue;

                        var conn = GetConnectionFromGroup(roomGroup, pConnId);
                        if (conn == null) continue;

                        var hand = pState.Cards
                            .Select(t => new TileDto { Left = t.LeftSide, Right = t.RightSide })
                            .ToList();

                        await conn.SendMessageAsync(Envelope(GameConstants.EventTileDealt,
                            new PlayerHandResponse
                            {
                                PlayerName = pName,
                                Hand = hand,
                                BoneyardCount = engine.Boneyard.Count,
                                FirstTurn = engine.CurrentPlayer.PlayerName
                            }));
                    }
                }
            }
            else
            {
                // The game wasn't running yet (just hanging out in the waiting room)
                if (roomRecord != null && roomRecord.Players.Count == 0)
                {
                    // The last person (or the host alone) left before starting. Destroy the room!
                    _gameManager.RemoveRoom(roomId);
                }
                else if (roomRecord != null)
                {
                    // Broadcast a fresh RoomSnapshot
                    // This forces the LobbyForm to instantly redraw the waiting room list.
                    // If the host migrated, the new host will instantly get the "Start Game" button!
                    await _groupManager.BroadcastToGroupAsync(roomGroup,
                        Envelope(GameConstants.EventRoomSnapshot, BuildRoomState(roomRecord)));
                }
            }

            // Update the lobby so the player count drops accurately on the UI cards,
            // or removes the card completely if the room was just destroyed!
            await _groupManager.BroadcastToGroupAsync("Lobby",
                Envelope(GameConstants.EventLobbySnapshot, BuildLobbySnapshot()));
        }

        // ── Private helpers ───────────────────────────────────────────
        private RoomStateResponse BuildRoomState(Domino.Server.State.RoomRecord record) =>
            new RoomStateResponse
            {
                RoomId = record.RoomId,
                RoomName = record.RoomName,
                MaxPlayers = record.MaxPlayers,
                ScoreLimit = record.ScoreLimit,
                ConnectedPlayers = record.Players.Values.ToList(),
                IsGameStarting = record.IsFull,
                OwnerName = record.OwnerName // Simply map the property you added to the state
            };

        private LobbySnapshotResponse BuildLobbySnapshot() =>
            new LobbySnapshotResponse
            {
                Rooms = _gameManager.GetAllRooms()
                    .Select(r => new RoomSummary
                    {
                        RoomId = r.RoomId,
                        RoomName = r.RoomName,
                        MaxPlayers = r.MaxPlayers,
                        CurrentCount = r.Players.Count,
                        GameRunning = r.GameRunning
                    }).ToList()
            };

        private async Task SendLobbySnapshot(PlayerConnection player) =>
            await player.SendMessageAsync(
                Envelope(GameConstants.EventLobbySnapshot, BuildLobbySnapshot()));

        private static async Task SendError(PlayerConnection p, string msg) =>
            await p.SendMessageAsync(Envelope(GameConstants.EventError, msg));

        private static string Envelope<T>(string action, T payload) =>
            JsonSerializer.Serialize(new { Action = action, Payload = payload });

        private PlayerConnection GetConnectionFromGroup(string group, string connId)
        {
            // Retrieve the actual connection so the server can send the hand
            return _connectionRegistry.GetConnection(connId);
        }
    }
}
