using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Models
{
    public class Team
    {
        public int Number { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public string Name => (this.Number > 0 ? $"team {this.Number:00}" : "all");
    }
}
