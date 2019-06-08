using System;
using System.Collections.Generic;
using System.Text;

namespace MiniatureGolf.DAL.Models
{
    public class PlayerCourseHit
    {
        public int Id { get; set; }
        public Player Player { get; set; }
        public Course Course { get; set; }
        public int? HitCount { get; set; }
    }
}
