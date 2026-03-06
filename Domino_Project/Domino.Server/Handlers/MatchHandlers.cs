// =====================================================================
//  FILE: MatchHandlers.cs  (Domino.Server/Handlers)
//
//  BEFORE: Giant "CRITICAL GAME LOGIC PLACEHOLDER" – the server trusted
//          the client completely and did zero validation.
//
//  AFTER:  Full server-authoritative validation:
//          1. Is it this player's turn?
//          2. Does the player actually hold the tile?
//          3. Is the placement mathematically legal?
//          4. Draw / Pass gating enforced server-side.
// =====================================================================
using System;
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
using Game_Engine;

namespace Domino.Server.Handlers
{
    public class MatchHandlers : IMessageHandler
    {
        private readonly GroupManager _groupManager;
        private readonly GameManager  _gameManager;

        public MatchHandlers(GroupManager groupManager, GameManager gameManager)
        {
            _groupManager = groupManager;
            _gameManager  = gameManager;
        }

        // ── Play a tile ───────────────────────────────────────────────
        [MessageRoute(GameConstants.ActionPlayDomino)]
        public async Task HandlePlayDominoAsync(PlayerConnection player, JsonElement payload)
        {
            var req = payload.Deserialize<PlayDominoRequest>(JsonOpts.Default);
            if (req == null) { await SendError(player, "Bad request."); return; }

            string roomId = _gameManager.FindRoomIdByConnection(player.ConnectionId);
            if (roomId == null) { await SendError(player, "You are not in a game."); return; }

            var engine = _gameManager.GetEngine(roomId);
            if (engine == null) { await SendError(player, "Game has not started yet."); return; }

            // ── Validation 1: Is it this player's turn? ───────────────
            string myName = _gameManager.GetPlayerName(player.ConnectionId);
            if (engine.CurrentPlayer.PlayerName != myName)
            {
                await SendError(player, "It is not your turn.");
                return;
            }

            // ── Validation 2: Does the player hold this tile? ─────────
            var tile = engine.CurrentPlayer.Cards
                .FirstOrDefault(t =>
                    (t.LeftSide  == req.TileValue1 && t.RightSide == req.TileValue2) ||
                    (t.LeftSide  == req.TileValue2 && t.RightSide == req.TileValue1));

            if (tile == null)
            {
                await SendError(player, $"You don't have tile [{req.TileValue1}|{req.TileValue2}].");
                return;
            }

            // ── Validation 3 + execute: GameEngine checks placement ───
            bool valid = engine.PlayCard(tile, req.TargetEdge);
            if (!valid)
            {
                await SendError(player,
                    $"Tile [{req.TileValue1}|{req.TileValue2}] cannot be placed on the {req.TargetEdge} end.");
                return;
            }

            // ── Broadcast the accepted move to everyone in the room ───
            string roomGroup = $"Room_{roomId}";
            var broadcast = new DominoPlayedResponse
            {
                PlayerId   = player.ConnectionId,
                PlayerName = myName,
                Tile       = new TileDto { Left = tile.LeftSide, Right = tile.RightSide },
                Edge       = req.TargetEdge,
                NextTurn   = engine.IsGameOver ? null : engine.CurrentPlayer.PlayerName,
                HandCount  = engine.Players
                                .FirstOrDefault(p => p.PlayerName == myName)?.Cards.Count ?? 0
            };

            await _groupManager.BroadcastToGroupAsync(roomGroup,
                Envelope(GameConstants.EventDominoPlayed, broadcast));

            Console.WriteLine($"[Match] {myName} played [{tile.LeftSide}|{tile.RightSide}] on {req.TargetEdge}");
        }

        // ── Draw a tile from the boneyard ─────────────────────────────
        [MessageRoute(GameConstants.ActionDrawTile)]
        public async Task HandleDrawTileAsync(PlayerConnection player, JsonElement payload)
        {
            var req = payload.Deserialize<DrawTileRequest>(JsonOpts.Default);
            if (req == null) { await SendError(player, "Bad request."); return; }

            string roomId = _gameManager.FindRoomIdByConnection(player.ConnectionId);
            if (roomId == null) { await SendError(player, "Not in a game."); return; }

            var engine = _gameManager.GetEngine(roomId);
            if (engine == null) { await SendError(player, "Game not started."); return; }

            string myName = _gameManager.GetPlayerName(player.ConnectionId);
            if (engine.CurrentPlayer.PlayerName != myName)
            {
                await SendError(player, "Not your turn.");
                return;
            }

            if (!engine.CanCurrentPlayerDraw())
            {
                await SendError(player, "Boneyard is empty.");
                return;
            }

            DominoTile drawn = engine.DrawFromBoneyard();
            string roomGroup = $"Room_{roomId}";

            // The drawn tile is PRIVATE – only send it to the drawing player
            await player.SendMessageAsync(Envelope(GameConstants.EventTileDrawn,
                new TileDrawnResponse
                {
                    DrawnTile     = new TileDto { Left = drawn.LeftSide, Right = drawn.RightSide },
                    BoneyardCount = engine.Boneyard.Count
                }));

            // Broadcast anonymised update to all others (hand count + boneyard size)
            await _groupManager.BroadcastToGroupAsync(roomGroup,
                Envelope("DrawBroadcast",
                    new DrawBroadcast { PlayerName = myName, BoneyardCount = engine.Boneyard.Count }));

            Console.WriteLine($"[Match] {myName} drew [{drawn.LeftSide}|{drawn.RightSide}]. Boneyard left: {engine.Boneyard.Count}");
        }

        // ── Pass turn ─────────────────────────────────────────────────
        [MessageRoute(GameConstants.ActionPass)]
        public async Task HandlePassAsync(PlayerConnection player, JsonElement payload)
        {
            string roomId = _gameManager.FindRoomIdByConnection(player.ConnectionId);
            if (roomId == null) { await SendError(player, "Not in a game."); return; }

            var engine = _gameManager.GetEngine(roomId);
            if (engine == null) { await SendError(player, "Game not started."); return; }

            string myName = _gameManager.GetPlayerName(player.ConnectionId);
            if (engine.CurrentPlayer.PlayerName != myName)
            {
                await SendError(player, "Not your turn.");
                return;
            }

            if (!engine.CanCurrentPlayerPass())
            {
                await SendError(player, "Cannot pass: draw from boneyard first, or you have a playable tile.");
                return;
            }

            engine.Pass();

            string roomGroup = $"Room_{roomId}";
            await _groupManager.BroadcastToGroupAsync(roomGroup,
                Envelope(GameConstants.EventPlayerPassed,
                    new PlayerPassedResponse
                    {
                        PlayerName = myName,
                        NextTurn   = engine.CurrentPlayer.PlayerName
                    }));

            Console.WriteLine($"[Match] {myName} passed. Next: {engine.CurrentPlayer.PlayerName}");
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static async Task SendError(PlayerConnection p, string msg) =>
            await p.SendMessageAsync(Envelope(GameConstants.EventError, msg));

        private static string Envelope<T>(string action, T payload) =>
            JsonSerializer.Serialize(new { Action = action, Payload = payload });
    }
}
