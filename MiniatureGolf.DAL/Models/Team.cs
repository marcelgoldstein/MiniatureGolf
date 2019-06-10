using System;
using System.Collections.Generic;
using System.Text;

namespace MiniatureGolf.DAL.Models
{
    public class Team
    {
        public int Id { get; set; }

        public int Number { get; set; }
        public string Name { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; }

        public List<TeamPlayer> TeamPlayers { get; set; } = new List<TeamPlayer>();

        public int? CurrentCourseNumber { get; set; }
        public bool IsDefaultTeam { get; set; }
    }
}
