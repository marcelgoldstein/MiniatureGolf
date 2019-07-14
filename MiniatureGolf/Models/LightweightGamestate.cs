using MiniatureGolf.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniatureGolf.Models
{


    public class LightweightGamestate
    {
        #region Properties
        public Game Game { get; set; }
        public Gamestatus Status { get { return (Gamestatus)this.Game.StateId; } set { this.Game.StateId = (int)value; } }
        public string StatusText => Enum.GetName(typeof(Gamestatus), this.Game.StateId).ToLower();
        public string PlayersTextForAvgRanking => $"{this.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).Count():#00}:    {string.Join(", ", this.GetPreparedPlayersForGame(this, RankingDisplayMode.Average))}";
        public string PlayersTextForSumRanking => $"{this.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).Count():#00}:    {string.Join(", ", this.GetPreparedPlayersForGame(this, RankingDisplayMode.Sum))}";
        public string Time => this.GetTimeText();
        #endregion Properties

        #region Methods
        private IEnumerable<string> GetPreparedPlayersForGame(LightweightGamestate gs, RankingDisplayMode rankingDisplayMode)
        {
            var players = gs.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player);

            players = this.Status switch
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
            if (this.Game.StartTime == null)
                return string.Empty;

            if (this.Game.StartTime?.Date != this.Game.FinishTime?.Date)
            {
                return $"{this.Game.StartTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")} - {this.Game.FinishTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")}";
            }
            else
            { // start und ende haben den gleichen tag, dann beim ende das datum weglassen und nur die uhrzeit anzeigen
                return $"{this.Game.StartTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")} - {this.Game.FinishTime?.ToLocalTime().ToString("HH:mm")}";
            }
        }
        #endregion Methods
    }
}
