using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Connection.Engine.Network
{
    public class ConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, PlayerConnection> _connections = new();

        public void AddConnection(PlayerConnection player)
        {
            if (_connections.TryAdd(player.ConnectionId, player))
            {
                Console.WriteLine($"[ConnectionManager] Player connected: {player.ConnectionId}. Total Online: {_connections.Count}");
            }
        }

        public void RemoveConnection(string connectionId)
        {
            if (_connections.TryRemove(connectionId, out _))
            {
                Console.WriteLine($"[ConnectionManager] Player disconnected: {connectionId}. Total Online: {_connections.Count}");
            }
        }

        public PlayerConnection GetConnection(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var player);
            return player;
        }

        public IEnumerable<PlayerConnection> GetAllConnections()
        {
            return _connections.Values;
        }
    }
}