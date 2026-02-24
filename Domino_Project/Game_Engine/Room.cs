using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class Room
    {
        public string Name { get; set; }
        public int ScoreLimit { get; set; }
        public int NumberOfPlayers { get; set; }
        public List<string> PlayerNames { get; set; }

        public bool IsReady()
        {
            return PlayerNames.Count >= 2;
        }
    }
}
