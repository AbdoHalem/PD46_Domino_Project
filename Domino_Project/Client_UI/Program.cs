// =====================================================================
//  FILE: Program.cs  (Client_UI)
//  FIX: Was launching Lobby directly (bypassing Login and server conn).
//       Now launches Login, which chains to LobbyForm after auth.
// =====================================================================
namespace Client_UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Login());   // FIX: was `new Lobby()`
        }
    }
}
