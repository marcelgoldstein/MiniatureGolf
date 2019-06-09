using System;
using System.Collections.Generic;
using System.Text;

namespace MiniatureGolf.DAL.Models
{
    public class Player
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }

        public int TeamId { get; set; }
        public Team Team { get; set; }

        public List<PlayerCourseHit> PlayerCourseHits { get; set; } = new List<PlayerCourseHit>();
    }
}
