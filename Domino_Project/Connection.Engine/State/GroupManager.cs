using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Connection.Engine.Network; // Needed to reference PlayerConnection

namespace Domino.Engine.State
{
    public class GroupManager
    {
        // Outer dictionary: The Group Name (e.g., "Lobby", "Room_12")
        // Inner dictionary: The Players in that group (Key: ConnectionId, Value: PlayerConnection)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerConnection>> _groups = new();

        public void AddToGroup(string groupName, PlayerConnection player)
        {
            // Get the group if it exists, or create a new one if it doesn't
            var group = _groups.GetOrAdd(groupName, _ => new ConcurrentDictionary<string, PlayerConnection>());

            if (group.TryAdd(player.ConnectionId, player))
            {
                // Crucial: Update the player's own tracker so we can clean them up easily if they disconnect
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
                    // Remove from the player's personal tracker
                    player.CurrentGroups.Remove(groupName);
                    Console.WriteLine($"[GroupManager] {player.ConnectionId} left {groupName}");

                    // Memory Management: If the room is now empty, destroy the room dictionary to free RAM
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
                // Send the message to every single player currently in this specific group
                foreach (var player in group.Values)
                {
                    await player.SendMessageAsync(message);
                }
            }
        }
    }
}