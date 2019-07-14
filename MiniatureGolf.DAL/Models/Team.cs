using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

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

        [NotMapped]
        public double? AverageHitCount => this.TeamPlayers.Select(a => a.Player.AverageHitCount).Where(a => a != null).Average(a => a);

        [NotMapped]
        public double? AverageOfPlayerSumHitCount => this.TeamPlayers.Select(a => a.Player.SumHitCount).Where(a => a != 0).Average(a => a);

        [NotMapped]
        public string NameForAvgRanking => (this.AverageHitCount != null ? $"{this.Name} ({this.AverageHitCount:N2})" : $"{this.Name}");

        [NotMapped]
        public string NameForAvgOfPlayerSumRanking => (this.AverageOfPlayerSumHitCount != null ? $"{this.Name} ({this.AverageOfPlayerSumHitCount:N2})" : $"{this.Name}");
    }
}
