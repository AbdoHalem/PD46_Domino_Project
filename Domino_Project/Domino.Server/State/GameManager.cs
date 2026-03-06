// =====================================================================
//  FILE: GameManager.cs  (Domino.Server/State)
//
//  FIX: `using Connection` → `using Domino.Engine.Networking`
//       (Connection is the assembly folder name, not the namespace)
// =====================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Connection.Engine.Network;   // ← FIXED: was `using Connection` which doesn't exist
using Game_Engine;

namespace Domino.Server.State
{
    /// <summary>Lightweight DTO stored in the registry for a room that has not yet started.</summary>
    public class RoomRecord
    {
        public string RoomId      { get; } = Guid.NewGuid().ToString("N")[..8];
        public string RoomName    { get; set; }
        public int    MaxPlayers  { get; set; }
        public int    ScoreLimit  { get; set; }
        public string OwnerId     { get; set; }
        public ConcurrentDictionary<string, string> Players { get; }   // connectionId → playerName
            = new ConcurrentDictionary<string, string>();

        public bool IsFull    => Players.Count >= MaxPlayers;
        public bool IsReady   => Players.Count >= 2;
        public bool GameRunning { get; set; } = false;
        public string OwnerName { get; set; }

    }

    /// <summary>
    /// Central server-side state manager.
    /// Injected into all handler classes via their constructor.
    /// </summary>
    public class GameManager
    {
        // ── Rooms (lobby phase) ───────────────────────────────────────
        private readonly ConcurrentDictionary<string, RoomRecord> _rooms = new();

        // ── Active GameEngine instances (in-game phase) ───────────────
        private readonly ConcurrentDictionary<string, GameEngine> _engines = new();

        // ── Watchers  roomId → list of watcher ConnectionIds ──────────
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _watchers = new();

        // ── Player name lookup  connectionId → displayName ────────────
        private readonly ConcurrentDictionary<string, string> _playerNames = new();

        // =====================================================================
        //  PLAYER IDENTITY
        // =====================================================================

        public void RegisterPlayer(string connectionId, string playerName)
            => _playerNames[connectionId] = playerName;

        public void UnregisterPlayer(string connectionId)
            => _playerNames.TryRemove(connectionId, out _);

        public string GetPlayerName(string connectionId)
            => _playerNames.TryGetValue(connectionId, out var name) ? name : connectionId;

        // =====================================================================
        //  ROOM MANAGEMENT
        // =====================================================================

        public RoomRecord CreateRoom(string ownerConnectionId, string roomName, int maxPlayers, int scoreLimit)
        {
            var record = new RoomRecord
            {
                RoomName = roomName,
                MaxPlayers = maxPlayers,
                ScoreLimit = scoreLimit,
                OwnerId = ownerConnectionId
            };

            string ownerName = GetPlayerName(ownerConnectionId);

            // ADD THIS LINE to save the owner's name into your new state property
            record.OwnerName = ownerName;

            record.Players.TryAdd(ownerConnectionId, ownerName);

            _rooms[record.RoomId] = record;
            Console.WriteLine($"[GameManager] Room '{roomName}' created (id={record.RoomId}) by {ownerName}");
            return record;
        }

        /// <summary>Returns null if room doesn't exist or is full / game running.</summary>
        public RoomRecord JoinRoom(string connectionId, string roomId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return null;
            if (room.IsFull || room.GameRunning) return null;

            string name = GetPlayerName(connectionId);
            room.Players.TryAdd(connectionId, name);
            Console.WriteLine($"[GameManager] {name} joined room '{room.RoomName}'");
            return room;
        }

        public void AddWatcher(string connectionId, string roomId)
        {
            var bag = _watchers.GetOrAdd(roomId, _ => new ConcurrentBag<string>());
            bag.Add(connectionId);
        }

        public void RemovePlayer(string connectionId, out string leftRoomId)
        {
            leftRoomId = null;
            foreach (var kvp in _rooms)
            {
                if (kvp.Value.Players.TryRemove(connectionId, out _))
                {
                    leftRoomId = kvp.Key;
                    Console.WriteLine($"[GameManager] {connectionId} left room '{kvp.Value.RoomName}'");
                    break;
                }
            }
        }

        public IEnumerable<RoomRecord> GetAllRooms() => _rooms.Values;

        public RoomRecord GetRoom(string roomId)
            => _rooms.TryGetValue(roomId, out var r) ? r : null;

        // =====================================================================
        //  GAME ENGINE LIFECYCLE
        // =====================================================================

        /// <summary>
        /// Converts a RoomRecord into a live GameEngine.
        /// Call this when all players in the room have pressed Ready.
        /// </summary>
        public GameEngine StartGame(string roomId)
        {
            if (!_rooms.TryGetValue(roomId, out var record))
                throw new InvalidOperationException($"Room {roomId} not found.");
            if (!record.IsReady)
                throw new InvalidOperationException("Need at least 2 players.");

            // Build the Game_Engine Room object from our record
            var room = new Room
            {
                Name          = record.RoomName,
                ScoreLimit    = record.ScoreLimit,
                NumberOfPlayers = record.MaxPlayers,
                PlayerNames   = record.Players.Values.ToList()
            };

            var engine = new GameEngine(room);
            _engines[roomId] = engine;
            record.GameRunning = true;

            Console.WriteLine($"[GameManager] Game started in room '{record.RoomName}' with {room.PlayerNames.Count} players.");
            return engine;
        }

        public GameEngine GetEngine(string roomId)
            => _engines.TryGetValue(roomId, out var e) ? e : null;

        public void EndGame(string roomId)
        {
            _engines.TryRemove(roomId, out _);
            if (_rooms.TryGetValue(roomId, out var r))
                r.GameRunning = false;
        }

        // =====================================================================
        //  HELPERS
        // =====================================================================

        /// <summary>Maps a connectionId to the roomId it is currently in.</summary>
        public string FindRoomIdByConnection(string connectionId)
        {
            foreach (var kvp in _rooms)
                if (kvp.Value.Players.ContainsKey(connectionId))
                    return kvp.Key;
            return null;
        }

        /// <summary>Maps a connectionId to the PlayerState index in the engine.</summary>
        public int GetPlayerIndex(GameEngine engine, string connectionId)
        {
            string name = GetPlayerName(connectionId);
            for (int i = 0; i < engine.Players.Count; i++)
                if (engine.Players[i].PlayerName == name)
                    return i;
            return -1;
        }
    }
}
