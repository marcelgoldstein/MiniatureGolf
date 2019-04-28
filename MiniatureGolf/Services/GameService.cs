using MiniatureGolf.Models;
using System.Collections.Generic;
using System.Linq;

namespace MiniatureGolf.Services
{
    public class GameService
    {
        #region Properties
        public Dictionary<string, Gamestate> Games { get; private set; } = new Dictionary<string, Gamestate>();
        #endregion Properties

        #region Methods
        #region Games
        public bool TryGetGame(string gameId, out Gamestate gamestate)
        {
            return this.Games.TryGetValue(gameId, out gamestate);
        }

        public string CreateNewGame()
        {
            var gs = new Gamestate();
            this.Games.Add(gs.Id, gs);
            return gs.Id;
        }

        public void DeleteGame(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                this.Games.Remove(gs.Id);
            }
        }


        #endregion Games 

        #region Courses


        public bool TryAddCourse(string gameId, int par)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                var t = new Course() { Number = gs.Courses.Count + 1, Par = par };
                foreach (var player in gs.Players)
                {
                    t.PlayerHits[player.Id] = null;
                }

                gs.Courses.Add(t);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryRemoveCourse(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                if (gs.Courses.Count > 0)
                {
                    gs.Courses.RemoveAt(gs.Courses.Count - 1);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion Courses

        #region Playsers
        public bool TryAddPlayer(string gameId, string name)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Player {gs.Players.Count + 1}";
                }

                var p = new Player() { Name = name };
                foreach (var track in gs.Courses)
                { // bei allen bestehenden tracks den neuen player hinzufügen
                    track.PlayerHits[p.Id] = null;
                }
                gs.Players.Add(p);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryRemovePlayer(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                if (gs.Players.Count > 0)
                {
                    var p = gs.Players.Last();
                    gs.Players.Remove(p);
                    foreach (var course in gs.Courses)
                    {
                        course.PlayerHits.Remove(p.Id);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion Playes
        #endregion Methods

    }
}
