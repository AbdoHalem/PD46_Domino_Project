// =====================================================================
//  FILE: Room.cs  (Game_Engine project)
//  BUG FIX: PlayerNames was never initialised → NullReferenceException
//           on IsReady() and anywhere the list is iterated.
// =====================================================================
using System.Collections.Generic;

namespace Game_Engine
{
    public class Room
    {
        public string       Name          { get; set; }
        public int          ScoreLimit    { get; set; }
        public int          NumberOfPlayers { get; set; }

        // FIX: initialise the list so callers don't need to guard against null.
        public List<string> PlayerNames   { get; set; } = new List<string>();

        public bool IsReady() => PlayerNames.Count >= 2;
        public bool IsFull()  => PlayerNames.Count >= NumberOfPlayers;
    }
}
