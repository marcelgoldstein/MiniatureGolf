using MiniatureGolf.DAL;
using MiniatureGolf.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniatureGolf.Models
{
    public enum Gamestatus
    {
        Created = 1,
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
        #region Properties
        public Game Game { get; set; }
        public MiniatureGolfContext GameDbContext { get; set; }

        public Gamestatus Status { get { return (Gamestatus)this.Game.StateId; } set { this.Game.StateId = (int)value; } }
        public string StatusText => Enum.GetName(typeof(Gamestatus), this.Game.StateId).ToLower();
        public string PlayersText => $"{this.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).Count():#00}:    {string.Join(", ", this.GetPreparedPlayersForGame(this))}";
        public string Time => this.GetTimeText();
        public bool IsAutoSaveActive { get; set; }
        #endregion Properties

        #region Events
        public event StateChangedHandler StateChanged;
        public delegate void StateChangedHandler(object sender, object caller, StateChangedContext context);

        public void RaiseStateChanged(object caller, StateChangedContext context) => this.StateChanged?.Invoke(this, caller, context);
        #endregion Events

        #region Methods
        private List<string> GetPreparedPlayersForGame(Gamestate gs)
        {
            var players = gs.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player)
                    .OrderByDescending(a => a.PlayerCourseHits.Count(b => b.HitCount != null)) // absteigend nach anzahl gespielter kurse
                    .ThenBy(a => a.PlayerCourseHits.Sum(b => b.HitCount ?? 0)) // aufsteigend nach summe der benötigten schläge
                    .ToList();

            var playerStrings = players.Select(a => $"{a.Name} ({a.PlayerCourseHits.Sum(b => b.HitCount ?? 0)})").ToList();

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

    public class StateChangedContext
    {
        public string Key { get; set; }
        public object Payload { get; set; }
    }
}
