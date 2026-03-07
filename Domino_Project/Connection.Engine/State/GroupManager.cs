using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Connection.Engine.Network;

namespace Domino.Engine.State
{
    public class GroupManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerConnection>> _groups = new();

        public void AddToGroup(string groupName, PlayerConnection player)
        {
            var group = _groups.GetOrAdd(groupName, _ => new ConcurrentDictionary<string, PlayerConnection>());

            if (group.TryAdd(player.ConnectionId, player))
            {
                player.CurrentGroups.Add(groupName);
                Console.WriteLine($"[GroupManager] {player.ConnectionId} joined {groupName}");
            }
        }

        public void RemoveFromGroup(string groupName, PlayerConnection player)
        {
            if (_groups.TryGetValue(groupName, out var group))
            {
                if (group.TryRemove(player.ConnectionId, out _))
                {
                    player.CurrentGroups.Remove(groupName);
                    Console.WriteLine($"[GroupManager] {player.ConnectionId} left {groupName}");

                    if (group.IsEmpty)
                    {
                        _groups.TryRemove(groupName, out _);
                        Console.WriteLine($"[GroupManager] {groupName} is empty and was destroyed.");
                    }
                }
            }
        }

        public async Task BroadcastToGroupAsync(string groupName, string message)
        {
            if (_groups.TryGetValue(groupName, out var group))
            {
                foreach (var player in group.Values)
                {
                    await player.SendMessageAsync(message);
                }
            }
        }
    }
}