using System;
using System.Collections.Generic;
using System.Text;

namespace MiniatureGolf.DAL.Models
{
    public class Game
    {
        public int Id { get; set; }

        public string GUID { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }

        public int StateId { get; set; }
        public State State { get; set; }

        public List<Team> Teams { get; set; } = new List<Team>();
        public List<Course> Courses { get; set; } = new List<Course>();
    }
}
