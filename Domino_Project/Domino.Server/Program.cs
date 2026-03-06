// =====================================================================
//  FILE: Program.cs  (Domino.Server project)
//
//  FIX: Removed `using Connection` (wrong / non-existent namespace).
//       Correct namespace is `Domino.Engine.Networking`.
//
//  The circular dependency is broken because Connection.Engine never
//  references Domino.Server. Program.cs wires them at startup via
//  server.SetHandlerFactory().
// =====================================================================
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
            Console.WriteLine("║   Domino Game Server  v2.0       ║");
            Console.WriteLine("╚══════════════════════════════════╝");

            // ── Ensure GameResults directory exists ───────────────────
            string resultsDir = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                             "..", "..", "..", "GameResults"));
            Directory.CreateDirectory(resultsDir);

            // ── Create the server (pure networking, no game logic) ────
            int port   = 5500;
            var server = new TcpCommServer(port);

            // ── Create game-logic singletons ──────────────────────────
            var gameManager = new GameManager();

            // Grab the objects TcpCommServer owns internally
            GroupManager       groupManager = server.GroupManager;
            ConnectionRegistry registry     = server.Registry;

            // ── Inject dependencies into the handler factory ──────────
            // The factory is called once per handler class at startup.
            // It tries the richest constructor first, falls back gracefully.
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

            // ── Start listening ───────────────────────────────────────
            server.Start();
            Console.WriteLine($"Server live on port {port}. Press Ctrl+C to quit.");
            await Task.Delay(-1);
        }
    }
}
