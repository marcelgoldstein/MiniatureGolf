using System;
using System.Collections.Generic;

namespace MiniatureGolf.Models
{
    public enum Gamestatus
    {
        Created = 0,
        Configuring = 5,
        Running = 10,
        Finished = 15,
    }

    public enum UserMode
    {
        Editor = 0,
        Spectator = 1,
        SpectatorReadOnly = 2,
    }

    public class Gamestate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public List<Team> Teams { get; set; } = new List<Team>();
        public List<Course> Courses { get; set; } = new List<Course>();
        public Gamestatus Status { get; set; } = Gamestatus.Created;
        public int? CurrentCourseNumber { get; set; }
    }
}
