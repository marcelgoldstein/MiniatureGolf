using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MiniatureGolf.DAL.Models;

public class Player
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Name { get; set; }

    public List<TeamPlayer> TeamPlayers { get; set; } = new List<TeamPlayer>();

    public List<PlayerCourseHit> PlayerCourseHits { get; set; } = new List<PlayerCourseHit>();

    [NotMapped]
    public double? AverageHitCount => PlayerCourseHits.Select(b => b.HitCount).Where(b => b != null).Average();

    [NotMapped]
    public int? SumHitCount => PlayerCourseHits.Select(b => b.HitCount).Where(b => b != null).Sum();

    [NotMapped]
    public string NameForAvgRanking => (AverageHitCount != null ? $"{Name} ({AverageHitCount:N2})" : $"{Name}");

    [NotMapped]
    public string NameForSumRanking => (SumHitCount != 0 ? $"{Name} ({SumHitCount:N0})" : $"{Name}");
}
