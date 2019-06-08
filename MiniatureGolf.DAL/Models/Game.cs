using System;
using System.Collections.Generic;
using System.Text;

namespace MiniatureGolf.DAL.Models
{
    public class Game
    {
        public string Id { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public State State { get; set; }


        public List<Team> Teams { get; set; }
        public List<Course> Courses { get; set; }
    }
}
