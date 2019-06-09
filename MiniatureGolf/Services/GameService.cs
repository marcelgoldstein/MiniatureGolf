using Microsoft.Extensions.DependencyInjection;
using MiniatureGolf.DAL;
using MiniatureGolf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DB = MiniatureGolf.DAL.Models;

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
        #region Fields
        private readonly IServiceProvider services; 
        #endregion Fields

        #region Properties
        public Dictionary<string, Gamestate> Games { get; private set; } = new Dictionary<string, Gamestate>();
        #endregion Properties

        #region ctor
        public GameService(IServiceProvider services)
        {
            this.services = services;
        }
        #endregion ctor

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

            this.SaveToDatabase(null);

            return gs.Id;
        }

        public void DeleteGame(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                this.Games.Remove(gs.Id);
            }
        }

        public List<Gamestate> GetGames(Gamestatus? status, DateFilter dateFilter)
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
                .Where(a => (status == null || a.Value.Status == status)
                    && a.Value.CreationTime.ToLocalTime() >= compareDate)
                .Select(a => a.Value);

            return games.ToList();
        }

        public void SaveToDatabase(Gamestate gs)
        {
            using (var db = this.services.GetService<MiniatureGolfContext>())
            {
                var g = new DB.Game() { StateId = 1 };
                db.Games.Add(g);

                var t = new DB.Team();
                var p1 = new DB.Player();
                p1.Number = 1;
                p1.Name = "Miguel";
                t.Players.Add(p1);
                var p2 = new DB.Player();
                p2.Number = 2;
                p2.Name = "Sarah";
                t.Players.Add(p2);
                g.Teams.Add(t);
                var c1 = new DB.Course();
                c1.Number = 1;
                c1.Par = 3;
                g.Courses.Add(c1);
                var c2 = new DB.Course();
                c2.Number = 2;
                c2.Par = 4;
                g.Courses.Add(c2);

                var pch11 = new DB.PlayerCourseHit();
                pch11.HitCount = 1;
                c1.PlayerCourseHits.Add(pch11);
                p1.PlayerCourseHits.Add(pch11);
                var pch12 = new DB.PlayerCourseHit();
                pch12.HitCount = 2;
                c1.PlayerCourseHits.Add(pch12);
                p2.PlayerCourseHits.Add(pch12);
                var pch21 = new DB.PlayerCourseHit();
                pch21.HitCount = 3;
                c2.PlayerCourseHits.Add(pch21);
                p1.PlayerCourseHits.Add(pch21);
                var pch22 = new DB.PlayerCourseHit();
                pch22.HitCount = 4;
                c2.PlayerCourseHits.Add(pch22);
                p2.PlayerCourseHits.Add(pch22);


                //var g = new DB.Game();

                //if (gs.Teams.Where(a => (a.Number != 0 && a.Players.Any())).Any())
                //{ // sind teams vorhanden, welche spieler beinhalten und nicht das "all"-team sind -> dann diese persistieren
                //    foreach (var team in gs.Teams.Where(a => (a.Number != 0 && a.Players.Any())).ToList())
                //    {
                //        var t = new DB.Team();

                //        foreach (var player in team.Players)
                //        {
                //            var p = new DB.Player();

                //            p.Id = player.Id;
                //            p.Name = player.Name;

                //            t.Players.Add(p);
                //        }

                //        t.Id = team.Number

                //    }

                //}
                //else
                //{ // nur das "all"-team persistieren

                //}








                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
        }
        #endregion Games 

        #region Courses
        public bool TryAddCourse(string gameId, int par)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                var t = new Course() { Number = gs.Courses.Count + 1, Par = par };
                foreach (var player in gs.Teams.Single(a => a.Number == 0).Players)
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
                    name = $"Player {gs.Teams.Single(a => a.Number == 0).Players.Count + 1:00}";
                }

                var p = new Player() { Name = name };
                foreach (var track in gs.Courses)
                { // bei allen bestehenden tracks den neuen player hinzufügen
                    track.PlayerHits[p.Id] = null;
                }

                // add player to default-team 'all' (which has number '0')
                gs.Teams.Single(a => a.Number == 0).Players.Add(p);

                // falls ein anderes team als das default-team vorhanden ist, in das neueste team aufnehmen
                if (gs.Teams.LastOrDefault(a => a.Number != 0) is Team t)
                {
                    t.Players.Add(p);
                }

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
                if (gs.Teams.Single(a => a.Number == 0).Players.LastOrDefault() is Player p)
                {
                    // player stats aus allen courses entfernen
                    foreach (var course in gs.Courses)
                    {
                        course.PlayerHits.Remove(p.Id);
                    }

                    // spieler aus dem default-team entfernen
                    gs.Teams.Single(a => a.Number == 0).Players.Remove(p);

                    // team ermitteln, indem der player sein muss
                    if (gs.Teams.LastOrDefault(a => a.Number != 0 && a.Players.Contains(p)) is Team t)
                    {
                        // spieler aus diesem team entfernen
                        t.Players.Remove(p);
                    }

                    // alle nicht-default-team teams, welche keiner spieler haben entfernen
                    gs.Teams.RemoveAll(a => a.Number != 0 && a.Players.Count == 0);
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
                if (gs.Teams.LastOrDefault(a => a.Number != 0) == null)
                { // erstes nicht-default-team, also alle bisherigen spieler aufnehmen
                    var t1 = new Team { Number = gs.Teams.Count };
                    t1.Players.AddRange(gs.Teams.Single(a => a.Number == 0).Players);
                    gs.Teams.Add(t1);
                }

                var t = new Team { Number = gs.Teams.Count };
                gs.Teams.Add(t);

                return true;
            }

            return false;
        }
        #endregion Teams
        #endregion Methods
    }
}
