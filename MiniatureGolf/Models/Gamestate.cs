using System;
using System.Collections.Generic;
using System.Linq;

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
        #region Properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreationTime { get; set; } = DateTime.UtcNow;
        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }

        public List<Team> Teams { get; set; } = new List<Team>() { new Team { Number = 0, Name = "all" } };
        public List<Course> Courses { get; set; } = new List<Course>();

        public Gamestatus Status { get; set; } = Gamestatus.Created;
        public string StatusText => Enum.GetName(typeof(Gamestatus), this.Status).ToLower();
        public string PlayersText => $"{this.Teams.Single(a => a.Number == 0).Players.Count:#00}:    {string.Join(", ", this.GetPreparedPlayersForGame(this))}";
        public string Time => this.GetTimeText();
        #endregion Properties

        #region Events
        public event StateChangedHandler StateChanged;
        public delegate void StateChangedHandler(object sender, object caller, StateChangedContext context);

        public void RaiseStateChanged(object caller, StateChangedContext context) => this.StateChanged?.Invoke(this, caller, context);
        #endregion Events

        #region Methods
        private List<string> GetPreparedPlayersForGame(Gamestate gs)
        {
            var players = gs.Teams.Single(a => a.Number == 0).Players
                    .OrderByDescending(a => gs.Courses.Count(b => b.PlayerHits[a.Id] != null)) // absteigend nach anzahl gespielter kurse
                    .ThenBy(a => gs.Courses.Sum(b => b.PlayerHits[a.Id])) // aufsteigend nach summe der benötigten schläge
                    .ToList();

            var playerStrings = players.Select(a => $"{a.Name} ({gs.Courses.Sum(b => b.PlayerHits[a.Id])})").ToList();

            return playerStrings;
        }

        private string GetTimeText()
        {
            if (this.StartTime == null)
                return string.Empty;

            if (this.StartTime?.Date != this.FinishTime?.Date)
            {
                return $"{this.StartTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")} - {this.FinishTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")}";
            }
            else
            { // start und ende haben den gleichen tag, dann beim ende das datum weglassen und nur die uhrzeit anzeigen
                return $"{this.StartTime?.ToLocalTime().ToString("dd.MM.yy HH:mm")} - {this.FinishTime?.ToLocalTime().ToString("HH:mm")}";
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
