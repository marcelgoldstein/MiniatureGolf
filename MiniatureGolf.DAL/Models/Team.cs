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

        public List<Player> Players { get; set; } = new List<Player>();

        public int? CurrentCourseNumber { get; set; }
    }
}
