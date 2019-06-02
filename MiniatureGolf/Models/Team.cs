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
        public int? CurrentCourseNumber { get; set; }

        private string name;
        public string Name
        {
            get
            {
                return (string.IsNullOrWhiteSpace(this.name) ? $"team {this.Number:00}" : this.name);
            }
            set
            {
                this.name = value;
            }
        }
    }
}
