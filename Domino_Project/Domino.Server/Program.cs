using System;
using System.IO;
using System.Threading.Tasks;
using Connection.Engine.Network;   // TcpCommServer, ConnectionRegistry
using Domino.Engine.State;
using Domino.Server.State;     // GroupManager
using Domino.Server.State;        // GameManager

namespace Domino.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════╗");
            Console.WriteLine("║   Domino Game Server  v1.0       ║");
            Console.WriteLine("╚══════════════════════════════════╝");

            string resultsDir = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                             "..", "..", "..", "GameResults"));
            Directory.CreateDirectory(resultsDir);

            // ── Create the server (pure networking, no game logic) ────
            int port   = 5500;
            var server = new TcpCommServer(port);

            var gameManager = new GameManager();

            // Grab the objects TcpCommServer owns internally
            GroupManager       groupManager = server.GroupManager;
            ConnectionRegistry registry     = server.Registry;

            server.SetHandlerFactory(handlerType =>
            {
                // 3-arg: (GroupManager, GameManager, ConnectionRegistry)
                try
                {
                    return Activator.CreateInstance(
                        handlerType, groupManager, gameManager, registry);
                }
                catch (MissingMethodException) { }

                // 2-arg: (GroupManager, GameManager)
                try
                {
                    return Activator.CreateInstance(
                        handlerType, groupManager, gameManager);
                }
                catch (MissingMethodException) { }

                // Original 1-arg fallback: (GroupManager)
                return Activator.CreateInstance(handlerType, groupManager);
            });

            server.Start();
            Console.WriteLine($"Server live on port {port}. Press Ctrl+C to quit.");
            await Task.Delay(-1);
        }
    }
}
