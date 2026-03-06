// =====================================================================
//  FILE: LobbyHandlers.cs  (Domino.Server/Handlers)
//
//  CHANGES vs original
//  -------------------
//  • Added Login handler so clients can register their display name.
//  • LobbyJoined now sends a full RoomSnapshot so the client can
//    render the available-rooms list immediately.
//  • Uses GameManager for player-name registry.
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

namespace Domino.Server.Handlers
{
    public class LobbyHandlers : IMessageHandler
    {
        private readonly GroupManager _groupManager;
        private readonly GameManager  _gameManager;

        // NOTE: The MessageRouter creates handler instances via Activator.CreateInstance.
        // For multi-dependency injection, extend the router or use a DI container.
        // Here we accept both via a shared static reference set in Program.cs.
        public LobbyHandlers(GroupManager groupManager, GameManager gameManager)
        {
            _groupManager = groupManager;
            _gameManager  = gameManager;
        }


        // ── Client first message after TCP connect ────────────────────
        [MessageRoute(GameConstants.ActionLogin)]
        public async Task HandleLoginAsync(PlayerConnection player, JsonElement payload)
        {
            var req = payload.Deserialize<LoginRequest>(JsonOpts.Default);
            string name = req?.PlayerName?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                await SendError(player, "Player name cannot be empty.");
                return;
            }

            _gameManager.RegisterPlayer(player.ConnectionId, name);
            Console.WriteLine($"[Lobby] Login: {name} ({player.ConnectionId})");

            // Confirm login
            await player.SendMessageAsync(Envelope(GameConstants.EventLoginOk,
                new LoginOkResponse { ConnectionId = player.ConnectionId, PlayerName = name }));

            // Immediately follow up with the lobby snapshot
            await SendLobbySnapshot(player);
        }

        // ── Player returns to lobby (e.g. after a game ends) ─────────
        [MessageRoute(GameConstants.ActionJoinLobby)]
        public async Task HandleJoinLobbyAsync(PlayerConnection player, JsonElement payload)
        {
            _groupManager.AddToGroup("Lobby", player);
            await SendLobbySnapshot(player);
        }

        // ── Helpers ───────────────────────────────────────────────────
        private async Task SendLobbySnapshot(PlayerConnection player)
        {
            var snapshot = new LobbySnapshotResponse
            {
                Rooms = _gameManager.GetAllRooms()
                    .Select(r => new RoomSummary
                    {
                        RoomId       = r.RoomId,
                        RoomName     = r.RoomName,
                        MaxPlayers   = r.MaxPlayers,
                        CurrentCount = r.Players.Count,
                        GameRunning  = r.GameRunning
                    }).ToList()
            };

            await player.SendMessageAsync(Envelope(GameConstants.EventLobbySnapshot, snapshot));
        }

        private static async Task SendError(PlayerConnection p, string msg)
            => await p.SendMessageAsync(Envelope(GameConstants.EventError, msg));

        private static string Envelope<T>(string action, T payload)
            => JsonSerializer.Serialize(new { Action = action, Payload = payload });
    }

    // ── Shared JSON options ───────────────────────────────────────────
    internal static class JsonOpts
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
