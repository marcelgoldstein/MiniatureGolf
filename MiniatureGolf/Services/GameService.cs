﻿using MiniatureGolf.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniatureGolf.Services
{
    #region Enums
    public enum DateFilter
    {
        Day,
        Week,
        Month,
        Quarter,
        Year,
    }
    #endregion Enums

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

        public List<Gamestate> GetGames(Gamestatus status, DateFilter dateFilter)
        {
            var compareDate = dateFilter switch
            {
                DateFilter.Day => DateTime.Today.Date,
                DateFilter.Week => DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday),
                DateFilter.Month => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                DateFilter.Quarter => new DateTime(DateTime.Today.Year, (((((DateTime.Today.Month - 1) / 3) + 1) - 1) * 3) + 1, 1),
                DateFilter.Year => new DateTime(DateTime.Today.Year, 1, 1),
                _ => throw new NotImplementedException(),
            };

            var games = this.Games
                .Where(a => a.Value.Status == status
                    && a.Value.CreationTime.ToLocalTime() >= compareDate)
                .Select(a => a.Value);

            return games.ToList();
        }
        #endregion Games 

        #region Courses
        public bool TryAddCourse(string gameId, int par)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                var t = new Course() { Number = gs.Courses.Count + 1, Par = par };
                foreach (var player in gs.Teams.SelectMany(a => a.Players))
                {
                    t.PlayerHits[player.Id] = null;
                }

                gs.Courses.Add(t);

                gs.Courses = gs.Courses.ToList(); // hack!, damit eine Property-Änderung erkannt wird

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

                    gs.Courses = gs.Courses.ToList(); // hack!, damit eine Property-Änderung erkannt wird
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
                    name = $"Player {gs.Teams.SelectMany(a => a.Players).Count() + 1:00}";
                }

                var p = new Player() { Name = name };
                foreach (var track in gs.Courses)
                { // bei allen bestehenden tracks den neuen player hinzufügen
                    track.PlayerHits[p.Id] = null;
                }

                if (gs.Teams.Count == 0)
                { // falls noch kein team vorhanden ist, eins erstellen
                    this.TryAddTeam(gameId);
                }

                gs.Teams.Last().Players.Add(p);

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
                if (gs.Teams.SelectMany(a => a.Players).Count() > 0)
                {
                    var t = gs.Teams.LastOrDefault();

                    if (t?.Players.Count == 0)
                    {
                        gs.Teams.Remove(t);
                    }

                    t = gs.Teams.LastOrDefault();

                    var p = t.Players.Last();
                    t.Players.Remove(p);
                    if (t.Players.Count == 0)
                    { // letzter spieler aus team entfernt, dann auch das team entfernen
                        gs.Teams.Remove(t);
                    }

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

        #region Teams
        public bool TryAddTeam(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                var t = gs.Teams.LastOrDefault();
                if (t != null && t.Players.Count == 0)
                { // bereits ein team vorhanden und dieses hat aber keine player -> kein neues team erstellen
                    return false;
                }

                t = new Team { Number = gs.Teams.Count + 1 };

                gs.Teams.Add(t);

                return true;
            }

            return false;
        }
        #endregion Teams
        #endregion Methods

    }
}
