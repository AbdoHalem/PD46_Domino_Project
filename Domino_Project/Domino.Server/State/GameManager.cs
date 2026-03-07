using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Connection.Engine.Network;
using Game_Engine;

namespace Domino.Server.State
{
    public class RoomRecord
    {
        public string RoomId      { get; } = Guid.NewGuid().ToString("N")[..8];
        public string RoomName    { get; set; }
        public int    MaxPlayers  { get; set; }
        public int    ScoreLimit  { get; set; }
        public string OwnerId     { get; set; }
        public ConcurrentDictionary<string, string> Players { get; } = new ConcurrentDictionary<string, string>();

        public bool IsFull    => Players.Count >= MaxPlayers;
        public bool IsReady   => Players.Count >= 2;
        public bool GameRunning { get; set; } = false;
        public string OwnerName { get; set; }

    }

    public class GameManager
    {
        private readonly ConcurrentDictionary<string, RoomRecord> _rooms = new();
        private readonly ConcurrentDictionary<string, GameEngine> _engines = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _watchers = new();
        private readonly ConcurrentDictionary<string, string> _playerNames = new();

        public void RegisterPlayer(string connectionId, string playerName)
            => _playerNames[connectionId] = playerName;

        public void UnregisterPlayer(string connectionId)
            => _playerNames.TryRemove(connectionId, out _);

        public string GetPlayerName(string connectionId)
            => _playerNames.TryGetValue(connectionId, out var name) ? name : connectionId;

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
            record.OwnerName = ownerName;
            record.Players.TryAdd(ownerConnectionId, ownerName);

            _rooms[record.RoomId] = record;
            Console.WriteLine($"[GameManager] Room '{roomName}' created (id={record.RoomId}) by {ownerName}");
            return record;
        }

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

        public GameEngine StartGame(string roomId)
        {
            if (!_rooms.TryGetValue(roomId, out var record))
                throw new InvalidOperationException($"Room {roomId} not found.");
            if (!record.IsReady)
                throw new InvalidOperationException("Need at least 2 players.");

            var room = new Room
            {
                Name = record.RoomName,
                ScoreLimit = record.ScoreLimit,
                NumberOfPlayers = record.MaxPlayers,
                PlayerNames = record.Players.Values.ToList()
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

        public string FindRoomIdByConnection(string connectionId)
        {
            foreach (var kvp in _rooms)
                if (kvp.Value.Players.ContainsKey(connectionId))
                    return kvp.Key;
            return null;
        }

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
