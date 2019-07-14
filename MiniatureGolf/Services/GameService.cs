using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MiniatureGolf.DAL;
using MiniatureGolf.DAL.Models;
using MiniatureGolf.Models;
using MiniatureGolf.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly AppSettings appSettings;
        #endregion Fields

        #region Properties
        public Dictionary<string, Gamestate> Games { get; private set; } = new Dictionary<string, Gamestate>();
        #endregion Properties

        #region ctor
        public GameService(IServiceProvider services, IHostApplicationLifetime applicationLifetime, IOptions<AppSettings> appSettings)
        {
            this.services = services;
            this.applicationLifetime = applicationLifetime;
            this.appSettings = appSettings.Value;

            var autoSaveTaskToken = new CancellationTokenSource();
            var autoCleanupTaskToken = new CancellationTokenSource();
            var autoIdleGamesCacheCleanerTaskToken = new CancellationTokenSource();
            this.applicationLifetime.ApplicationStopping.Register(() =>
            {
                autoSaveTaskToken.Cancel();
                autoCleanupTaskToken.Cancel();
                autoIdleGamesCacheCleanerTaskToken.Cancel();
                this.SaveAllOpenGames();
            });

            // don`t await on purpose so the started thread/task starts running in parallel
            _ = this.RunAutoSaveTaskAsync(autoSaveTaskToken.Token);
            _ = this.RunAutoCleanupTaskAsync(autoCleanupTaskToken.Token);
            _ = this.RunIdleGamesCacheCleanerTaskAsync(autoIdleGamesCacheCleanerTaskToken.Token);
        }
        #endregion ctor

        #region Methods
        #region Workers
        private async Task RunAutoSaveTaskAsync(CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                while (ct.IsCancellationRequested == false)
                {
                    await Task.Delay(TimeSpan.FromSeconds(this.appSettings.WorkerSettings.AutoSaveWorkerSettings.AutoSaveIntervalInSeconds), ct);
                    this.AutoSaveActivatedGames();
                }
            }, ct);
        }

        private async Task RunAutoCleanupTaskAsync(CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                while (ct.IsCancellationRequested == false)
                {
                    this.ClearUnstartedGames();
                    await Task.Delay(TimeSpan.FromMinutes(this.appSettings.WorkerSettings.UnstartedGamesCleanerSettings.WorkerIntervallInMinutes));
                }
            }, ct);
        }

        private async Task RunIdleGamesCacheCleanerTaskAsync(CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                while (ct.IsCancellationRequested == false)
                {
                    this.ClearIdleGamesFromCache();
                    await Task.Delay(TimeSpan.FromMinutes(this.appSettings.WorkerSettings.IdleGamesCacheCleanerSettings.WorkerIntervallInMinutes));
                }
            }, ct);
        }
        #endregion Workers

        #region Games
        public bool TryGetGame(string gameId, out Gamestate gamestate)
        {
            lock (this.Games)
            {
                if (this.Games.TryGetValue(gameId, out gamestate))
                {
                    return true;
                }
                else
                {
                    // lookup in db
                    gamestate = this.LoadGameFromDatabase(gameId);

                    if (gamestate != null)
                    {
                        this.Games.Add(gamestate.Game.GUID, gamestate);
                        return true;
                    }

                    return false;
                }
            }
        }

        /// <summary>
        /// Lädt das Game mit der angegebenen GameId aus der Datenbank und liefert ein ummantelndes Gamestate object.
        /// Wird unter der GameId kein Game gefunden, wird null zurückgegeben.
        /// Für den Ladevorgang wird eine neue Instanz des DbContext geöffnet und offenen gelassen, um Änderungen vornehmen zu können. Der DbContext ist über das Gamestate-Objekt verfügbar.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        private Gamestate LoadGameFromDatabase(string gameId)
        {
            var db = this.services.GetService<MiniatureGolfContext>();

            var g = db.Games
                .Include(a => a.Courses)
                    .ThenInclude(a => a.PlayerCourseHits)
                .Include(a => a.Teams)
                    .ThenInclude(a => a.TeamPlayers)
                        .ThenInclude(a => a.Player)
                .SingleOrDefault(a => a.GUID == gameId);

            if (g != null)
            {
                var gs = new Gamestate();
                gs.GameDbContext = db;
                gs.Game = g;

                return gs;
            }

            return default;
        }

        public string CreateNewGame()
        {
            lock (this.Games)
            {
                var db = this.services.GetService<MiniatureGolfContext>();
                var gs = new Gamestate();
                gs.GameDbContext = db;

                var g = new Game();
                g.GUID = Guid.NewGuid().ToString();
                g.CreationTime = DateTime.UtcNow;
                g.Teams.Add(new Team { IsDefaultTeam = true, Number = 0, Name = "all" });
                gs.Game = g;
                db.Games.Add(g);

                this.Games.Add(gs.Game.GUID, gs);

                return gs.Game.GUID; 
            }
        }

        public void DeleteGame(string gameId)
        {
            lock (this.Games)
            {
                if (this.TryGetGame(gameId, out var gs))
                {
                    this.Games.Remove(gs.Game.GUID);
                    gs.GameDbContext.Games.Remove(gs.Game);
                    gs.GameDbContext.SaveChanges();
                    gs.GameDbContext.Dispose();
                }
            }
        }

        public List<Gamestate> GetGames(Gamestatus? status, DateFilter dateFilter)
        {
            lock (this.Games)
            {
                var games = new List<Gamestate>();

                var compareDate = dateFilter switch
                {
                    DateFilter.Day => DateTime.Today.Date,
                    DateFilter.Week => DateTime.Today.AddDays(-((7 + (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
                    DateFilter.Month => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    DateFilter.Quarter => new DateTime(DateTime.Today.Year, (((((DateTime.Today.Month - 1) / 3) + 1) - 1) * 3) + 1, 1),
                    DateFilter.Year => new DateTime(DateTime.Today.Year, 1, 1),
                    _ => throw new NotImplementedException(),
                };
                compareDate = compareDate.ToUniversalTime();

                var gamesFromCache = this.Games
                    .Where(a => (status == null || a.Value.Game.StateId == (int)status)
                        && (a.Value.Game.StartTime >= compareDate || a.Value.Game.CreationTime >= compareDate))
                    .Select(a => a.Value)
                    .ToList();

                games.AddRange(gamesFromCache);

                // db lookup auf weitere games, welche noch nicht im dictionary geladen sind
                var gamesToFilterByInternalId = gamesFromCache.Where(a => a.Game.Id != 0).Select(a => a.Game.Id); // bereits in dictionary enthaltene müssen nicht neu geladen werden

                using (var db = this.services.GetService<MiniatureGolfContext>())
                {
                    var gamesFromDatabase = new List<Gamestate>();

                    var gameGuidsToLoad = db.Games
                        .Where(a => (status == null || a.StateId == (int)status)
                            && (a.StartTime >= compareDate || a.CreationTime >= compareDate)
                            && gamesToFilterByInternalId.Contains(a.Id) == false)
                        .Select(a => a.GUID)
                        .ToList();

                    foreach (var guid in gameGuidsToLoad)
                    {
                        var gs = this.LoadGameFromDatabase(guid);

                        if (gs != null)
                        {
                            this.Games.Add(gs.Game.GUID, gs);
                            gamesFromDatabase.Add(gs);
                        }
                    }

                    games.AddRange(gamesFromDatabase);
                }

                return games;
            }
        }

        public List<LightweightGamestate> GetGamesLightweight(Gamestatus? status, DateFilter dateFilter)
        {
            var games = new List<LightweightGamestate>();

            var compareDate = dateFilter switch
            {
                DateFilter.Day => DateTime.Today.Date,
                DateFilter.Week => DateTime.Today.AddDays(-((7 + (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
                DateFilter.Month => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                DateFilter.Quarter => new DateTime(DateTime.Today.Year, (((((DateTime.Today.Month - 1) / 3) + 1) - 1) * 3) + 1, 1),
                DateFilter.Year => new DateTime(DateTime.Today.Year, 1, 1),
                _ => throw new NotImplementedException(),
            };
            compareDate = compareDate.ToUniversalTime();

            using (var db = this.services.GetService<MiniatureGolfContext>())
            {
                var gamesFromDatabase = db.Games
                    .Include(a => a.Courses)
                        .ThenInclude(a => a.PlayerCourseHits)
                    .Include(a => a.Teams)
                        .ThenInclude(a => a.TeamPlayers)
                            .ThenInclude(a => a.Player)
                    .Where(a => (status == null || a.StateId == (int)status)
                        && (a.StartTime >= compareDate || a.CreationTime >= compareDate))
                    .ToList();

                foreach (var gameFromDatabase in gamesFromDatabase)
                {
                    var gs = new LightweightGamestate();
                    gs.Game = gameFromDatabase;

                    games.Add(gs);
                }

                return games;
            }
        }

        public void SaveToDatabase(Gamestate gs)
        {
            gs.GameDbContext.SaveChanges();
        }

        private void SaveAllOpenGames()
        {
            lock (this.Games)
            {
                var gamesToSave = this.Games.Where(a => a.Value.Game.StateId > (int)Gamestatus.Created).Select(a => a.Value).ToList();

                foreach (var gs in gamesToSave)
                {
                    this.SaveToDatabase(gs);
                }
            }
        }

        #region Worker Methods
        private void AutoSaveActivatedGames()
        {
            lock (this.Games)
            {
                var gamesToSave = this.Games.Where(a => a.Value.IsAutoSaveActive).Select(a => a.Value).ToList();

                foreach (var gs in gamesToSave)
                {
                    this.SaveToDatabase(gs);
                }
            }
        }

        /// <summary>
        /// Deletes games with status "Created" and "Configuring" which are older than specified (in appSettings) from the database and cache.
        /// </summary>
        private void ClearUnstartedGames()
        {
            lock (this.Games)
            {
                var compareDate = DateTime.UtcNow.AddHours(this.appSettings.WorkerSettings.UnstartedGamesCleanerSettings.IdleTimeInHours * -1);
                var gamesCurrentlyActive = this.Games.Where(a => (a.Value.MostRecentIsActivelyUsedHeartbeatTime ?? DateTime.MinValue) >= compareDate).Select(a => a.Value.Game.GUID).ToList();

                // clear from db
                using (var db = this.services.GetService<MiniatureGolfContext>())
                {
                    var gamesToDelete = db.Games
                        .Where(a => (a.StateId == (int)Gamestatus.Created || a.StateId == (int)Gamestatus.Configuring)
                            && a.CreationTime < compareDate
                            && !(gamesCurrentlyActive.Contains(a.GUID)))
                        .AsEnumerable();

                    if (gamesToDelete.Any())
                    {
                        db.Games.RemoveRange(gamesToDelete);

                        db.SaveChanges();
                    }
                }

                // clear from cache
                var gamesToRemoveFromCache = this.Games
                        .Where(a => (a.Value.Game.StateId == (int)Gamestatus.Created || a.Value.Game.StateId == (int)Gamestatus.Configuring)
                            && a.Value.Game.CreationTime < compareDate
                            && !(gamesCurrentlyActive.Contains(a.Value.Game.GUID)))
                        .AsEnumerable();

                foreach (var gameToRemoveFromCache in gamesToRemoveFromCache.ToList())
                {
                    this.Games.Remove(gameToRemoveFromCache.Value.Game.GUID);
                    gameToRemoveFromCache.Value.GameDbContext.Dispose();
                }
            }
        }

        /// <summary>
        /// Remove games, which arent actively used from the cache.
        /// </summary>
        private void ClearIdleGamesFromCache()
        {
            lock (this.Games)
            {
                var compareDate = DateTime.UtcNow.AddMinutes(this.appSettings.WorkerSettings.IdleGamesCacheCleanerSettings.IdleTimeInMinutes * -1);
                var gamesIdleForGivenTime = this.Games.Where(a => (a.Value.MostRecentIsActivelyUsedHeartbeatTime ?? DateTime.MinValue) < compareDate).Select(a => a.Value).ToList();

                foreach (var gameToRemoveFromCache in gamesIdleForGivenTime)
                {
                    this.Games.Remove(gameToRemoveFromCache.Game.GUID);
                    gameToRemoveFromCache.GameDbContext.Dispose();
                }
            }
        }
        #endregion Worker Methods
        #endregion Games 

        #region Courses
        public bool TryAddCourse(string gameId, int par)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                lock (gs)
                {
                    var c = new Course() { Number = gs.Game.Courses.Count + 1, Par = par };
                    gs.GameDbContext.Courses.Add(c);

                    c.Game = gs.Game;
                    gs.Game.Courses.Add(c);

                    gs.Game.Courses = gs.Game.Courses.ToList(); // hack!, damit eine Property-Änderung erkannt wird

                    return true;
                }
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
                lock (gs)
                {
                    if (gs.Game.Courses.Count > 0)
                    {
                        var c = gs.Game.Courses.LastOrDefault();
                        c.Game = null;
                        gs.Game.Courses.Remove(c);
                        gs.GameDbContext.Courses.Remove(c);

                        gs.Game.Courses = gs.Game.Courses.ToList(); // hack!, damit eine Property-Änderung erkannt wird
                    }

                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion Courses

        #region Players
        public bool TryAddPlayer(string gameId, string name)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                lock (gs)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = $"P {gs.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).Count() + 1:00}";
                    }

                    var p = new Player() { Name = name };
                    gs.GameDbContext.Players.Add(p);

                    var tp = new TeamPlayer();
                    gs.GameDbContext.TeamPlayers.Add(tp);

                    tp.Player = p;
                    p.TeamPlayers.Add(tp);

                    // add player to default-team 'all' (which has number '0')
                    var t = gs.Game.Teams.Single(a => a.IsDefaultTeam);
                    tp.Team = t;
                    t.TeamPlayers.Add(tp);

                    // falls ein anderes team als das default-team vorhanden ist, in das neueste team aufnehmen
                    if (gs.Game.Teams.LastOrDefault(a => a.IsDefaultTeam == false) is Team t2)
                    {
                        var tp2 = new TeamPlayer();
                        gs.GameDbContext.TeamPlayers.Add(tp2);

                        tp2.Player = p;
                        p.TeamPlayers.Add(tp2);
                        tp2.Team = t2;
                        t2.TeamPlayers.Add(tp2);
                    }

                    return true;
                }
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
                lock (gs)
                {
                    if (gs.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).LastOrDefault() is Player p)
                    {
                        // spieler aus allen teams entfernen
                        var teams = p.TeamPlayers.Select(a => a.Team).ToList();
                        foreach (var t in teams)
                        {
                            var tp = t.TeamPlayers.Single(a => a.Player == p);
                            tp.Team = null;
                            t.TeamPlayers.Remove(tp);
                            tp.Player = null;
                            p.TeamPlayers.Remove(tp);

                            gs.GameDbContext.TeamPlayers.Remove(tp);
                        }

                        this.RemoveEmptyTeams(gameId);
                    }

                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion Players

        #region Teams
        public bool TryAddTeam(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                lock (gs)
                {
                    if (gs.Game.Teams.LastOrDefault(a => a.IsDefaultTeam == false) == null)
                    { // erstes nicht-default-team, also alle bisherigen spieler aufnehmen
                        var t1 = new Team { Number = gs.Game.Teams.Count, Name = $"Team {gs.Game.Teams.Count:00}" };
                        gs.GameDbContext.Teams.Add(t1);
                        t1.Game = gs.Game;
                        gs.Game.Teams.Add(t1);

                        foreach (var p in gs.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers.Select(a => a.Player).ToList())
                        {
                            var tp = new TeamPlayer();
                            gs.GameDbContext.TeamPlayers.Add(tp);
                            tp.Player = p;
                            p.TeamPlayers.Add(tp);
                            tp.Team = t1;
                            t1.TeamPlayers.Add(tp);
                        }
                    }

                    // nur ein Team erstellen, wenn das letzte offene Team Spieler beinhaltet
                    if (gs.Game.Teams.LastOrDefault()?.TeamPlayers.Select(a => a.Player).Count() > 0)
                    {
                        // neues zweites team erstellen, in welches ab sofort die weiteren player-adds hineinlaufen
                        var t = new Team { Number = gs.Game.Teams.Count, Name = $"Team {gs.Game.Teams.Count:00}" };
                        gs.GameDbContext.Teams.Add(t);
                        t.Game = gs.Game;
                        gs.Game.Teams.Add(t);
                    }

                    return true;
                }
            }

            return false;
        }

        public void RemoveEmptyTeams(string gameId)
        {
            if (this.TryGetGame(gameId, out var gs))
            {
                lock (gs)
                {
                    // alle nicht-default-team teams, welche keiner spieler haben entfernen
                    var teamsToRemove = gs.Game.Teams.Where(a => a.IsDefaultTeam == false && a.TeamPlayers.Select(b => b.Player).Count() == 0).ToList();
                    foreach (var ttr in teamsToRemove)
                    {
                        ttr.Game = null;
                        gs.Game.Teams.Remove(ttr);

                        gs.GameDbContext.Teams.Remove(ttr);
                    }
                }
            }
        }
        #endregion Teams
        #endregion Methods
    }
}
