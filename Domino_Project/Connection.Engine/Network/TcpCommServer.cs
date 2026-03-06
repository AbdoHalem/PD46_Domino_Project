// =====================================================================
//  FILE: TcpCommServer.cs  (Connection.Engine project)
//
//  FIX: Removed `using Domino.Server` and GameManager constructor
//       parameter — those created a circular project reference.
//
//  Connection.Engine must stay a pure networking library.
//  It knows nothing about GameManager, rooms, or game logic.
//  Instead, it exposes SetHandlerFactory() so the application
//  layer (Domino.Server) can inject its own handler-creation
//  logic AFTER construction but BEFORE Start().
// =====================================================================
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Connection.Engine.Router;
using Domino.Engine.State;

namespace Connection.Engine.Network
{
    public class TcpCommServer
    {
        private readonly TcpListener        _listener;
        private readonly ConnectionRegistry _connectionRegistry;
        private readonly GroupManager       _groupManager;
        private readonly MessageRouter      _router;

        // ── Original single-arg constructor (no GameManager) ──────────
        public TcpCommServer(int port)
        {
            _listener           = new TcpListener(IPAddress.Any, port);
            _connectionRegistry = new ConnectionRegistry();
            _groupManager       = new GroupManager();
            _router             = new MessageRouter(_groupManager);
        }

        // ── Expose internals so Program.cs can build the factory ─────
        public ConnectionRegistry Registry     => _connectionRegistry;
        public GroupManager       GroupManager => _groupManager;

        // ── Let the application layer supply a custom handler factory ─
        // Call this BEFORE Start().
        // Example from Domino.Server/Program.cs:
        //   server.SetHandlerFactory(type =>
        //       Activator.CreateInstance(type, groupManager, gameManager, registry));
        public void SetHandlerFactory(Func<Type, object> factory)
        {
            _router.SetHandlerFactory(factory);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"[Server] Domino TCP Engine started on port {((IPEndPoint)_listener.LocalEndpoint).Port}...");

            _ = AcceptClientsAsync();
            // _ = StartHeartbeatMonitorAsync();  // re-enable for production
        }

        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                try
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                    PlayerConnection newPlayer = new PlayerConnection(tcpClient);

                    _connectionRegistry.AddConnection(newPlayer);
                    _groupManager.AddToGroup("Lobby", newPlayer);

                    _ = HandlePlayerCommunicationAsync(newPlayer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server Error] Failed to accept client: {ex.Message}");
                }
            }
        }

        private async Task HandlePlayerCommunicationAsync(PlayerConnection player)
        {
            NetworkStream stream = player.Client.GetStream();
            byte[] lengthPrefixBuffer = new byte[4];

            try
            {
                while (player.Client.Connected)
                {
                    int bytesRead = await ReadExactBytesAsync(stream, lengthPrefixBuffer, 4);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"[Network] {player.ConnectionId} disconnected gracefully.");
                        break;
                    }

                    int messageLength = BitConverter.ToInt32(lengthPrefixBuffer, 0);

                    if (messageLength <= 0 || messageLength > 1024 * 1024)
                    {
                        Console.WriteLine($"[Security] Invalid payload size from {player.ConnectionId}. Dropping.");
                        break;
                    }

                    byte[] messageBuffer = new byte[messageLength];
                    await ReadExactBytesAsync(stream, messageBuffer, messageLength);

                    player.LastMessageReceivedAt = DateTime.UtcNow;

                    string jsonPayload = Encoding.UTF8.GetString(messageBuffer);
                    await _router.RouteMessageAsync(player, jsonPayload);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[Network Drop] {player.ConnectionId} connection lost: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Unexpected error for {player.ConnectionId}: {ex.Message}");
            }
            finally
            {
                await HandlePlayerCleanupAsync(player);
            }
        }

        private async Task<int> ReadExactBytesAsync(NetworkStream stream, byte[] buffer, int bytesToRead)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < bytesToRead)
            {
                int read = await stream.ReadAsync(buffer, totalBytesRead, bytesToRead - totalBytesRead);
                if (read == 0) return 0;
                totalBytesRead += read;
            }
            return totalBytesRead;
        }

        private async Task HandlePlayerCleanupAsync(PlayerConnection player)
        {
            try
            {
                _connectionRegistry.RemoveConnection(player.ConnectionId);

                string[] groupsToLeave = System.Linq.Enumerable.ToArray(player.CurrentGroups);

                foreach (string groupName in groupsToLeave)
                {
                    _groupManager.RemoveFromGroup(groupName, player);

                    string disconnectNotice = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Action  = "PlayerDisconnected",
                        Payload = new { PlayerId = player.ConnectionId }
                    });

                    await _groupManager.BroadcastToGroupAsync(groupName, disconnectNotice);
                }

                player.Client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup Error] {player.ConnectionId}: {ex.Message}");
            }
        }

        private async Task StartHeartbeatMonitorAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                DateTime timeoutThreshold = DateTime.UtcNow.AddMinutes(-2);

                foreach (var player in _connectionRegistry.GetAllConnections())
                {
                    if (player.LastMessageReceivedAt < timeoutThreshold)
                    {
                        Console.WriteLine($"[Timeout] {player.ConnectionId} timed out. Forcing disconnect.");
                        player.Client.Close();
                    }
                }
            }
        }
    }
}
