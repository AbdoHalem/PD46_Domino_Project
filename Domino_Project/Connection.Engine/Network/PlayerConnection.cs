using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Connection.Engine.Network
{
    public class PlayerConnection
    {
        public string ConnectionId { get; } = Guid.NewGuid().ToString();
        public TcpClient Client { get; }

        // Used by the Heartbeat Monitor to detect sudden Wi-Fi drops
        public DateTime LastMessageReceivedAt { get; set; } = DateTime.UtcNow;

        // Tracks which rooms/lobbies this player is in for O(1) cleanup on disconnect
        public HashSet<string> CurrentGroups { get; } = new HashSet<string>();

        public PlayerConnection(TcpClient client)
        {
            Client = client;
        }

        public async Task SendMessageAsync(string message)
        {
            // If the socket is already dead, don't try to write to it
            if (!Client.Connected) return;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            // Length-prefixing: Get the 4-byte size of the payload to solve TCP fragmentation
            byte[] lengthPrefix = BitConverter.GetBytes(messageBytes.Length);

            try
            {
                NetworkStream stream = Client.GetStream();
                // Send the 4-byte size first
                await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                // Then send the actual JSON payload
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception)
            {
                // If the player's connection drops the exact millisecond we try to write,
                // catch the exception and forcefully close the socket to trigger cleanup.
                Client.Close();
            }
        }
    }
}