using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MiniatureGolf.DAL.Models;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using MiniatureGolf.Settings;
using MiniatureGolf.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages
{
    public class GameScoreboardModel : ComponentBase, IDisposable
    {
        #region Const
        protected const string Context_HitCount = nameof(Context_HitCount);
        protected const string Context_Par = nameof(Context_Par);
        protected const string Context_GameStarted = nameof(Context_GameStarted);
        protected const string Context_GameFinished = nameof(Context_GameFinished);
        protected const string Context_CurrentCourse = nameof(Context_CurrentCourse);
        protected const string Context_Courses = nameof(Context_Courses);
        protected const string Context_Teams_Players = nameof(Context_Teams_Players);
        #endregion Const

        #region Enums
        public enum CourseState
        {
            Unstarted = 0,
            Started = 1,
            Finished = 2,
        }
        #endregion Enums

        #region Variables
        private readonly List<string> autoRefreshEmojis = new List<string> { "🐒", "🦍", "🐩", "🐕", "🐈", "🐅", "🐆", "🐎", "🦌", "🦏", "🦛", "🐂", "🐃", "🐄", "🐖", "🐏", "🐑", "🐐", "🐪", "🐫", "🦙", "🦘", "🦡", "🐘", "🐁", "🐀", "🦔", "🐇", "🐿", "🦎", "🐊", "🐢", "🐍", "🐉", "🦕", "🦖", "🦈", "🐬", "🐳", "🐋", "🐟", "🐠", "🐡", "🦐", "🦑", "🐙", "🦞", "🦀", "🐚", "🦆", "🐓", "🦃", "🦅", "🕊", "🦢", "🦜", "🦚", "🦉", "🐦", "🐧", "🐥", "🐤", "🐣", "🦇", "🦋", "🐌", "🐛", "🦟", "🦗", "🐜", "🐝", "🐞", "🦂", "🕷" };
        private CancellationTokenSource mostRecentTouchUpdaterTaskToken;
        #endregion Variables

        #region Properties
        [Inject] public GameService GameService { get; private set; }
        [Inject] protected IUriHelper UriHelper { get; private set; }
        [Inject] protected IOptions<AppSettings> AppSettings { get; private set; }
        [Inject] protected IHostApplicationLifetime applicationLifetime { get; private set; }

        [Parameter] protected string GameId { get; set; }
        [Parameter] protected string Mode { get; set; }
        [Parameter] protected string TeamNumber { get; set; }
        [Parameter] protected string RankingMode { get; set; }

        public Gamestate Gamestate { get; private set; }

        public string PlayerNameToAdd { get; set; }


        private int courseHitLimit = 7;
        public int CourseHitLimit { get => courseHitLimit; set { courseHitLimit = value; this.RecalibrateCoursePars(value); this.Gamestate.Game.CourseHitLimit = this.CourseHitLimit; } }

        public int CourseParNumberToAdd { get; set; } = 3;

        public List<Player> RankedPlayers { get; set; } = new List<Player>();
        public bool ShowColumns { get; set; } = true;
        public UserMode CurrentUserMode { get; set; }

        protected List<UserModeDropDownItem> ShareModes { get; set; }
        public int SelectedShareMode { get; set; } = (int)UserMode.SpectatorReadOnly;

        private int selectedTeamNumber;
        public int SelectedTeamNumber
        {
            get { return selectedTeamNumber; }
            set { selectedTeamNumber = value; this.OnSelectedTeamNumberChanged(); }
        }
        public Team SelectedTeam { get; set; }

        public bool IsNotificationWindowVisible { get; set; }

        /// <summary>
        /// Wird verwendet, um eine Verzögerung der Aktualisierung zu bewirken, wenn der Editierer Punkte geändert hat. 
        /// </summary>
        protected RedundantExecutionSuppressor AutoRefreshHelper { private set; get; }

        public string AutoRefreshEmoji { get; set; } = "😜";

        public bool ShowOuterViewEditOverlay { get; set; }
        public bool OuterViewEditOverlayAnimationTrigger { get; set; }
        protected RedundantExecutionSuppressor OuterViewEditOverlayHelper { private set; get; }

        protected RankingDisplayMode RankingDisplayMode { get; set; } = RankingDisplayMode.Average;
        #endregion Properties

        #region ctor
        public GameScoreboardModel()
        {
            this.AutoRefreshHelper = new RedundantExecutionSuppressor(async (t) => { this.RefreshPlayerRanking(); await this.Invoke(this.StateHasChanged); }, TimeSpan.FromSeconds(3));
            this.AutoRefreshHelper.ProgressChanged += (sender, e) =>
            {
                this.Invoke(this.StateHasChanged);
            };

            this.OuterViewEditOverlayHelper = new RedundantExecutionSuppressor(async (t) => { this.OuterViewEditOverlayAnimationTrigger = false; await this.Invoke(this.StateHasChanged); await Task.Delay(1000); t.ThrowIfCancellationRequested(); this.ShowOuterViewEditOverlay = false; await this.Invoke(this.StateHasChanged); }, TimeSpan.FromSeconds(3));
        }
        #endregion ctor

        #region Methods
        protected override Task OnInitAsync()
        {
            this.ShareModes = new List<UserModeDropDownItem>
            {
                new UserModeDropDownItem{ ModeId = (int)UserMode.Editor, Name = "editor" },
                new UserModeDropDownItem{ ModeId = (int)UserMode.Spectator, Name = "spectator" },
                new UserModeDropDownItem{ ModeId = (int)UserMode.SpectatorReadOnly, Name = "read only" },
            };

            return base.OnInitAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            UserMode desiredUserMode;
            if (string.IsNullOrWhiteSpace(this.GameId))
            { // sample game erstellen und dann per navigate als editor öffnen
                desiredUserMode = UserMode.Editor;
                this.CreateSampleGame();
                this.ChangeUserMode(desiredUserMode);
                this.SelectedShareMode = (int)UserMode.Editor;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(this.Mode))
                { // ohne parameter wird SpectatorReadOnly drauß
                    desiredUserMode = UserMode.SpectatorReadOnly;
                }
                else
                {
                    desiredUserMode = (UserMode)Convert.ToInt32(this.Mode);

                    if (desiredUserMode == UserMode.Editor)
                        this.SelectedShareMode = (int)UserMode.Editor;
                }

                if (this.GameService.TryGetGame(this.GameId, out var gs))
                {
                    this.SetCurrentGameState(gs);

                    this.ChangeUserMode(desiredUserMode);

                    if (!string.IsNullOrWhiteSpace(this.TeamNumber))
                    {
                        var t = this.Gamestate.Game.Teams.SingleOrDefault(a => a.Number == Convert.ToInt32(this.TeamNumber));
                        if (t != null)
                        { // ein gültiges team wurde per parameter angesteuert
                            this.SelectedTeamNumber = t.Number;
                        }
                    }

                    this.RefreshPlayerRanking();
                }
                else
                { // ungültie gameId übergeben
                    this.IsNotificationWindowVisible = true;
                    this.CreateSampleGame();
                    this.ChangeUserMode(UserMode.Editor);
                    this.SelectedShareMode = (int)UserMode.Editor;
                }
            }

            this.mostRecentTouchUpdaterTaskToken = new CancellationTokenSource();

            this.applicationLifetime.ApplicationStopping.Register(() =>
            {
                this.mostRecentTouchUpdaterTaskToken.Cancel();
            });

            _ = this.StartMostRecentTouchUpdater(this.mostRecentTouchUpdaterTaskToken.Token);

            if (this.RankingMode != null)
                this.RankingDisplayMode = Enum.Parse<RankingDisplayMode>(this.RankingMode); 

            return base.OnParametersSetAsync();
        }

        private async Task StartMostRecentTouchUpdater(CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                while (ct.IsCancellationRequested == false)
                {
                    await Task.Delay(TimeSpan.FromMinutes(this.AppSettings.Value.WorkerSettings.IdleGamesCacheCleanerSettings.WorkerIntervallInMinutes - (this.AppSettings.Value.WorkerSettings.IdleGamesCacheCleanerSettings.WorkerIntervallInMinutes / 10D)));

                    if (this.Gamestate != null)
                        this.Gamestate.MostRecentIsActivelyUsedHeartbeatTime = DateTime.UtcNow;
                }
            }, ct);
        }

        private void SetCurrentGameState(Gamestate gs)
        {
            if (this.Gamestate != null)
                this.Gamestate.StateChanged -= this.Gamestate_StateChanged;

            this.Gamestate = gs;
            this.CourseHitLimit = this.Gamestate.Game.CourseHitLimit;
            this.SelectedTeamNumber = 0;

            if (this.Gamestate != null)
                this.Gamestate.StateChanged += this.Gamestate_StateChanged;

            this.Gamestate.MostRecentIsActivelyUsedHeartbeatTime = DateTime.UtcNow;
        }

        private void Gamestate_StateChanged(object sender, object caller, StateChangedContext context)
        {
            if (caller == this)
                return; // nur events, welche von anderen  Page-Instanzen ausgelöst wurden sind relevant

            if ((sender as Gamestate)?.Game.GUID != this.Gamestate?.Game.GUID)
                return; // nicht das eigene 'game'

            if (context.Key == Context_HitCount)
            {
                if (this.SelectedTeam.TeamPlayers.Any(a => a.Player == (context.Payload as Player)))
                { // nur wenn der betroffenen Spieler im aktuell angezeigten Team ist
                    this.ShowOuterViewEditOverlay = true;
                    this.OuterViewEditOverlayAnimationTrigger = true;
                    this.RefreshPlayerRanking();
                    this.OuterViewEditOverlayHelper.Push();
                }
            }

            if (context.Key == Context_Teams_Players)
            {
                if (this.Gamestate.Game.Teams.Contains(this.SelectedTeam) == false)
                { // ist das ausgewählte team nicht mehr vorhande, das default-team (0) auswählen
                    this.SelectedTeamNumber = 0;
                }

                this.RefreshPlayerRanking();
            }

            this.Invoke(this.StateHasChanged);
        }

        private void CreateSampleGame(int recursiveCounter = 0)
        {
            this.CreateNewGame();

            if (this.Gamestate == null)
            {
                if (recursiveCounter <= 10)
                {
                    this.CreateSampleGame(++recursiveCounter);
                    return;
                }
            }

            this.CourseParNumberToAdd = 4;
            this.AddCourse();
            this.CourseParNumberToAdd = 3;
            this.AddCourse();
            this.CourseParNumberToAdd = 5;
            this.AddCourse();
            this.CourseParNumberToAdd = 2;
            this.AddCourse();
            this.CourseParNumberToAdd = 3;
            this.AddCourse();


            this.PlayerNameToAdd = "Peter";
            this.AddPlayer();
            this.PlayerNameToAdd = "Hugo";
            this.AddPlayer();
            this.PlayerNameToAdd = "Melissa";
            this.AddPlayer();
            this.PlayerNameToAdd = "Saul";
            this.AddPlayer();

            // Peter
            var p1 = this.Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[0].Player;
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[0], p1, 5);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[1], p1, 4);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[2], p1, 5);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[3], p1, 2);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[4], p1, 5);

            // Hugo
            var p2 = this.Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[1].Player;
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[0], p2, 3);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[1], p2, 2);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[2], p2, 3);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[3], p2, 3);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[4], p2, 4);

            // Melissa
            var p3 = this.Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[2].Player;
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[0], p3, 2);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[1], p3, 4);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[2], p3, 4);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[3], p3, 2);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[4], p3, 1);

            // Saul
            var p4 = this.Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[3].Player;
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[0], p4, 4);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[1], p4, 3);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[2], p4, 4);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[3], p4, 2);
            this.IncreaseHitCountInternal(this.Gamestate.Game.Courses[4], p4, 4);

            this.RefreshPlayerRanking();
        }

        protected void AddPlayer()
        {
            if (this.GameService.TryAddPlayer(this.Gamestate.Game.GUID, this.PlayerNameToAdd))
            {
                this.PlayerNameToAdd = null;

                this.RefreshPlayerRanking();

                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Teams_Players });
            }
        }

        protected void RemovePlayer()
        {
            if (this.GameService.TryRemovePlayer(this.Gamestate.Game.GUID))
            {
                if (this.Gamestate.Game.Teams.Contains(this.SelectedTeam) == false)
                { // ist das ausgewählte team nicht mehr vorhande, das default-team (0) auswählen
                    this.SelectedTeamNumber = 0;
                }

                this.RefreshPlayerRanking();

                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Teams_Players });
            }
        }

        protected void CreateNewTeam()
        {
            if (this.GameService.TryAddTeam(this.Gamestate.Game.GUID))
            {
                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Teams_Players });
            }
        }

        /// <summary>
        /// Removes all Columns and readds them so the order is correct.
        /// huge buggability here!!! only use with caution
        /// </summary>
        protected void ResetColumnsOrder()
        {
            this.ShowColumns = false;

            var _ = this.InvokeAsync(async () =>
            {
                await Task.Delay(1);
                this.ShowColumns = true;
                this.StateHasChanged();
            });
        }

        protected void AddCourse()
        {
            if (this.GameService.TryAddCourse(this.Gamestate.Game.GUID, this.CourseParNumberToAdd))
            {
                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Courses });
            }
        }

        protected void RemoveCourse()
        {
            if (this.GameService.TryRemoveCourse(this.Gamestate.Game.GUID))
            {
                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Courses });
            }
        }

        private void RefreshPlayerRanking()
        {
            if (this.SelectedTeam == null)
            {
                this.SelectedTeamNumber = 0;
            }

            var players = this.SelectedTeam.TeamPlayers.Select(a => a.Player);

            if (this.Gamestate.Status >= Gamestatus.Running)
            {
                players = this.Gamestate.Status switch
                {
                    Gamestatus.Running => players
                        .OrderBy(a => a.AverageHitCount ?? double.MaxValue) // aufsteigend nach dem durchschnitt der gebrauchten schläge
                        .ThenByDescending(a => a.PlayerCourseHits.Count(b => b.HitCount != null)), // absteigend nach anzahl gespielter kurse
                    Gamestatus.Finished => players
                        .OrderByDescending(a => a.PlayerCourseHits.Count(b => b.HitCount != null)) // absteigend nach anzahl gespielter kurse
                        .ThenBy(a => a.AverageHitCount ?? double.MaxValue), // aufsteigend nach dem durchschnitt der gebrauchten schläge
                    _ => throw new NotImplementedException()
                };
            }

            this.RankedPlayers = players.ToList();
        }

        protected void CreateNewGame()
        {
            if (this.GameService.TryGetGame(this.GameService.CreateNewGame(), out var gs))
            {
                this.SetCurrentGameState(gs);

                this.RefreshPlayerRanking();

                this.Gamestate.Game.StateId = (int)Gamestatus.Configuring;
            }
        }

        protected void FinishGame()
        {
            if (this.Gamestate != null)
            {
                this.Gamestate.Game.StateId = (int)Gamestatus.Finished;
                this.Gamestate.Game.FinishTime = DateTime.UtcNow;

                this.Gamestate.IsAutoSaveActive = false; // nach fertigstellen die autosave-modus wieder deaktivieren

                this.GameService.SaveToDatabase(this.Gamestate);

                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_GameFinished });
            }
        }

        protected void StartGame()
        {
            if (this.Gamestate != null)
            {
                // remove empty teams that remained from configuration
                this.GameService.RemoveEmptyTeams(this.Gamestate.Game.GUID);

                this.Gamestate.Game.StateId = (int)Gamestatus.Running;
                this.Gamestate.Game.StartTime = DateTime.UtcNow;

                this.RefreshPlayerRanking();
                this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_GameStarted });
            }
        }

        protected void ChangeUserMode(UserMode newMode)
        {
            this.CurrentUserMode = newMode;
            this.RefreshPlayerRanking();

            if (newMode == UserMode.Editor && this.Gamestate.Status <= Gamestatus.Running)
            { // gamestate als 'IsAutoSaveActive' markieren, da dort potenziell änderungen geschehen
                this.Gamestate.IsAutoSaveActive = true;
            }

            this.StateHasChanged();
        }

        private void OnSelectedTeamNumberChanged()
        {
            this.SelectedTeam = this.Gamestate.Game.Teams.SingleOrDefault(a => a.Number == selectedTeamNumber);
            this.RefreshPlayerRanking();
        }

        protected CourseState GetCurrentCourseStateForView(Course c)
        {
            var relevantPlayers = this.SelectedTeam.TeamPlayers.Select(a => a.Player);
            if (relevantPlayers.All(a => a.PlayerCourseHits.Any(b => b.Course == c && b.HitCount != null)))
                return CourseState.Finished; // alle spieler haben für diesen course einen hitcount != null
            else if (relevantPlayers.Any(a => a.PlayerCourseHits.Any(b => b.Course == c && b.HitCount != null)))
                return CourseState.Started; // mindestens ein spieler hat für diesen course einen hitcount != null
            else
                return CourseState.Unstarted; // ansonsten: keiner der spieler hat für diesen course einen hitcount != null
        }

        protected void IncreaseHitCount(Course c, Player p, int step = 1)
        {
            IncreaseHitCountInternal(c, p, step);

            this.RandoPickAutoRefreshEmoji();
            this.AutoRefreshHelper.Push();

            if (this.Gamestate.Status == Gamestatus.Running)
                this.SelectedTeam.CurrentCourseNumber = c.Number;

            this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_HitCount, Payload = p });
        }

        private void IncreaseHitCountInternal(Course c, Player p, int step = 1)
        {
            if (!(c.PlayerCourseHits.SingleOrDefault(a => a.Player == p) is PlayerCourseHit pch))
            {
                pch = new PlayerCourseHit();
                this.Gamestate.GameDbContext.PlayerCourseHits.Add(pch);
                pch.Course = c;
                c.PlayerCourseHits.Add(pch);
                pch.Player = p;
                p.PlayerCourseHits.Add(pch);
            }

            pch.HitCount = (pch.HitCount ?? 0) + step;

            // modulo 8, damit ein überrollen angewendet wird (8, da null/0 bis 7 gleich 8 elemente)
            // statt 7 in der formel gilt 'CourseHitLimit'
            pch.HitCount %= (this.CourseHitLimit + 1);
            // dann 0 durch null ersetzen
            pch.HitCount = pch.HitCount == 0 ? null : pch.HitCount;
        }

        protected void IncreasePar(Course c)
        {
            if (c.Par < this.CourseHitLimit)
            {
                c.Par++;
            }
            else
            {
                c.Par = 1;
            }

            this.Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Par });
        }

        protected bool RowShouldBeDisplayedTransparently(Course c)
        {
            if (this.Gamestate.Game.StateId != (int)Gamestatus.Running)
                return false;

            if (this.SelectedTeam.CurrentCourseNumber == null)
                return false;

            if (this.SelectedTeam.CurrentCourseNumber == c.Number)
                return false;

            return true;
        }

        private void RandoPickAutoRefreshEmoji()
        {
            var rnd = new Random();
            var emoji = this.autoRefreshEmojis[rnd.Next(0, this.autoRefreshEmojis.Count)];

            this.AutoRefreshEmoji = emoji;
        }

        protected void RecalibrateCoursePars(int upperLimit)
        {
            if (this.CourseParNumberToAdd > upperLimit)
                this.CourseParNumberToAdd = upperLimit;

            foreach (var course in this.Gamestate.Game.Courses)
            {
                if (course.Par > upperLimit)
                    course.Par = upperLimit;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.mostRecentTouchUpdaterTaskToken.Cancel();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GameScoreboardModel()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion IDisposable Support
        #endregion Methods

        protected class UserModeDropDownItem
        {
            public int ModeId { get; set; }
            public string Name { get; set; }
        }
    }
}