using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniatureGolf.DAL.Models;

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
    public double? AverageHitCount => TeamPlayers.Select(a => a.Player.AverageHitCount).Where(a => a != null).Average(a => a);

    [NotMapped]
    public double? AverageOfPlayerSumHitCount => TeamPlayers.Select(a => a.Player.SumHitCount).Where(a => a != 0).Average(a => a);

    [NotMapped]
    public string NameForAvgRanking => (AverageHitCount != null ? $"{Name} ({AverageHitCount:N2})" : $"{Name}");

    [NotMapped]
    public string NameForAvgOfPlayerSumRanking => (AverageOfPlayerSumHitCount != null ? $"{Name} ({AverageOfPlayerSumHitCount:N2})" : $"{Name}");
}
