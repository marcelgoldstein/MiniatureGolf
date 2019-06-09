using System;
using System.Collections.Generic;
using System.Text;

namespace MiniatureGolf.DAL.Models
{
    public class Course
    {
        public int Id { get; set; }

        public int Number { get; set; }
        public int Par { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; }

        public List<PlayerCourseHit> PlayerCourseHits { get; set; } = new List<PlayerCourseHit>();
    }
}
