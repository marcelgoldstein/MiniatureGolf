using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiniatureGolf.DAL.Models
{
    public class Player
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }

        public List<TeamPlayer> TeamPlayers { get; set; } = new List<TeamPlayer>();

        public List<PlayerCourseHit> PlayerCourseHits { get; set; } = new List<PlayerCourseHit>();

        [NotMapped]
        public double? AverageHitCount => this.PlayerCourseHits.Select(b => b.HitCount).Where(b => b != null).Average();

        [NotMapped]
        public int? SumHitCount => this.PlayerCourseHits.Select(b => b.HitCount).Where(b => b != null).Sum();

        [NotMapped]
        public string NameForAvgRanking => (this.AverageHitCount != null ? $"{this.Name} ({this.AverageHitCount:N2})" : $"{this.Name}");

        [NotMapped]
        public string NameForSumRanking => (this.SumHitCount != 0 ? $"{this.Name} ({this.SumHitCount:N0})" : $"{this.Name}");
    }
}
