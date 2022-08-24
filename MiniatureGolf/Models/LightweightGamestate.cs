using MiniatureGolf.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniatureGolf.Models;


public class LightweightGamestate
{
    #region Properties
    public Game Game { get; set; }
    public Gamestatus Status { get { return (Gamestatus)Game.State; } set { Game.State = (int)value; } }
    public string StatusText => Enum.GetName(typeof(Gamestatus), Game.State).ToLower();
    public string PlayersTextForAvgRanking => $"{Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).Count():#00}:    {string.Join(", ", GetPreparedPlayersForGame(this, RankingDisplayMode.Average))}";
    public string PlayersTextForSumRanking => $"{Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).Count():#00}:    {string.Join(", ", GetPreparedPlayersForGame(this, RankingDisplayMode.Sum))}";
    public string Time => GetTimeText();
    #endregion Properties

    #region Methods
    private IEnumerable<string> GetPreparedPlayersForGame(LightweightGamestate gs, RankingDisplayMode rankingDisplayMode)
    {
        var players = gs.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player);

        players = Status switch
        {
            Gamestatus.Running => players
                .OrderBy(a => a.AverageHitCount ?? double.MaxValue) // aufsteigend nach dem durchschnitt der gebrauchten schläge
                .ThenByDescending(a => a.PlayerCourseHits.Count(b => b.HitCount != null)), // absteigend nach anzahl gespielter kurse

            _ => players
                .OrderByDescending(a => a.PlayerCourseHits.Count(b => b.HitCount != null)) // absteigend nach anzahl gespielter kurse
                .ThenBy(a => a.AverageHitCount ?? double.MaxValue), // aufsteigend nach dem durchschnitt der gebrauchten schläge
        };

        var playerStrings = rankingDisplayMode switch
        {
            RankingDisplayMode.Average => players.Select(a => a.NameForAvgRanking),
            RankingDisplayMode.Sum => players.Select(a => a.NameForSumRanking),
            _ => throw new NotImplementedException()
        };

        return playerStrings;
    }

    private string GetTimeText()
    {
        if (Game.StartTime == null)
            return string.Empty;

        if (Game.StartTime?.Date != Game.FinishTime?.Date)
        {
            return $"{Game.StartTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")} - {Game.FinishTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")}";
        }
        else
        { // start und ende haben den gleichen tag, dann beim ende das datum weglassen und nur die uhrzeit anzeigen
            return $"{Game.StartTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")} - {Game.FinishTime?.ToLocalTime().ToString("HH:mm")}";
        }
    }
    #endregion Methods
}
