using Microsoft.AspNetCore.Components;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using MiniatureGolf.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages
{
    public class GameScoreboardModel : ComponentBase
    {
        #region Const
        protected const string Context_HitCount = nameof(Context_HitCount);
        protected const string Context_Par = nameof(Context_Par);
        protected const string Context_GameStarted = nameof(Context_GameStarted);
        protected const string Context_GameFinished = nameof(Context_GameFinished);
        protected const string Context_CurrentCourse = nameof(Context_CurrentCourse);
        #endregion Const

        #region Variables
        private readonly List<string> autoRefreshEmojis = new List<string> { "🐒", "🦍", "🐩", "🐕", "🐈", "🐅", "🐆", "🐎", "🦌", "🦏", "🦛", "🐂", "🐃", "🐄", "🐖", "🐏", "🐑", "🐐", "🐪", "🐫", "🦙", "🦘", "🦡", "🐘", "🐁", "🐀", "🦔", "🐇", "🐿", "🦎", "🐊", "🐢", "🐍", "🐉", "🦕", "🦖", "🦈", "🐬", "🐳", "🐋", "🐟", "🐠", "🐡", "🦐", "🦑", "🐙", "🦞", "🦀", "🐚", "🦆", "🐓", "🦃", "🦅", "🕊", "🦢", "🦜", "🦚", "🦉", "🐦", "🐧", "🐥", "🐤", "🐣", "🦇", "🦋", "🐌", "🐛", "🦟", "🦗", "🐜", "🐝", "🐞", "🦂", "🕷" };
        #endregion Variables

        #region Properties
        [Inject] public GameService GameService { get; private set; }
        [Inject] protected IUriHelper UriHelper { get; private set; }

        [Parameter] protected string GameId { get; set; }
        [Parameter] protected string Mode { get; set; }
        [Parameter] protected string TeamNumber { get; set; }

        public Gamestate Gamestate { get; private set; }

        public string PlayerNameToAdd { get; set; }
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
        public Team NullTeamSelection { get; set; } = new Team { Number = 0 };

        public bool IsNotificationWindowVisible { get; set; }

        /// <summary>
        /// Wird verwendet, um eine Verzögerung der Aktualisierung zu bewirken, wenn der Editierer Punkte geändert hat. 
        /// </summary>
        protected RedundantExecutionSuppressor AutoRefreshHelper { private set; get; }

        public string AutoRefreshEmoji { get; set; } = "😜";

        public bool ShowOuterViewEditOverlay { get; set; }
        public bool OuterViewEditOverlayAnimationTrigger { get; set; }
        protected RedundantExecutionSuppressor OuterViewEditOverlayHelper { private set; get; }
        #endregion Properties

        #region ctor
        public GameScoreboardModel()
        {
            this.AutoRefreshHelper = new RedundantExecutionSuppressor(() => { this.RefreshPlayerRanking(); this.Invoke(this.StateHasChanged); }, TimeSpan.FromSeconds(3));
            this.AutoRefreshHelper.ProgressChanged += (sender, e) =>
            {
                this.Invoke(this.StateHasChanged);
            };

            this.OuterViewEditOverlayHelper = new RedundantExecutionSuppressor(async () => { this.OuterViewEditOverlayAnimationTrigger = false; await this.Invoke(this.StateHasChanged); await Task.Delay(1000); this.ShowOuterViewEditOverlay = false; await this.Invoke(this.StateHasChanged); }, TimeSpan.FromSeconds(3));
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
                this.NavigateToGame(this.Gamestate.Id, this.CurrentUserMode, this.SelectedTeamNumber);
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
                }

                if (this.GameService.TryGetGame(this.GameId, out var gs))
                {
                    this.SetCurrentGameState(gs);

                    this.ChangeUserMode(desiredUserMode);

                    if (!string.IsNullOrWhiteSpace(this.TeamNumber))
                    {
                        var t = this.Gamestate.Teams.SingleOrDefault(a => a.Number == Convert.ToInt32(this.TeamNumber));
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
                }
            }

            return base.OnParametersSetAsync();
        }

        private void SetCurrentGameState(Gamestate gs)
        {
            if (this.Gamestate != null)
                this.Gamestate.StateChanged -= this.Gamestate_StateChanged;

            this.Gamestate = gs;

            if (this.Gamestate != null)
                this.Gamestate.StateChanged += this.Gamestate_StateChanged;
        }

        private void Gamestate_StateChanged(object sender, object caller, string context)
        {
            if (caller == this)
                return; // nur events, welche von anderen  Page-Instanzen ausgelöst wurden sind relevant

            if ((sender as Gamestate)?.Id != this.Gamestate?.Id)
                return; // nicht das eigene 'game'

            if (context == Context_HitCount)
            {
                this.ShowOuterViewEditOverlay = true;
                this.OuterViewEditOverlayAnimationTrigger = true;
                this.RefreshPlayerRanking();
                this.OuterViewEditOverlayHelper.Push();
            }

            this.Invoke(this.StateHasChanged);
        }

        private void CreateSampleGame()
        {
            this.CreateNewGame();

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
            var p1 = this.Gamestate.Teams.SelectMany(a => a.Players).ToArray()[0];
            this.Gamestate.Courses[0].PlayerHits[p1.Id] = 5;
            this.Gamestate.Courses[1].PlayerHits[p1.Id] = 4;
            this.Gamestate.Courses[2].PlayerHits[p1.Id] = 5;
            this.Gamestate.Courses[3].PlayerHits[p1.Id] = 2;
            this.Gamestate.Courses[4].PlayerHits[p1.Id] = 5;

            // Hugo
            var p2 = this.Gamestate.Teams.SelectMany(a => a.Players).ToArray()[1];
            this.Gamestate.Courses[0].PlayerHits[p2.Id] = 3;
            this.Gamestate.Courses[1].PlayerHits[p2.Id] = 2;
            this.Gamestate.Courses[2].PlayerHits[p2.Id] = 3;
            this.Gamestate.Courses[3].PlayerHits[p2.Id] = 3;
            this.Gamestate.Courses[4].PlayerHits[p2.Id] = 4;

            // Melissa
            var p3 = this.Gamestate.Teams.SelectMany(a => a.Players).ToArray()[2];
            this.Gamestate.Courses[0].PlayerHits[p3.Id] = 2;
            this.Gamestate.Courses[1].PlayerHits[p3.Id] = 4;
            this.Gamestate.Courses[2].PlayerHits[p3.Id] = 4;
            this.Gamestate.Courses[3].PlayerHits[p3.Id] = 2;
            this.Gamestate.Courses[4].PlayerHits[p3.Id] = 1;

            // Saul
            var p4 = this.Gamestate.Teams.SelectMany(a => a.Players).ToArray()[3];
            this.Gamestate.Courses[0].PlayerHits[p4.Id] = 4;
            this.Gamestate.Courses[1].PlayerHits[p4.Id] = 3;
            this.Gamestate.Courses[2].PlayerHits[p4.Id] = 4;
            this.Gamestate.Courses[3].PlayerHits[p4.Id] = 2;
            this.Gamestate.Courses[4].PlayerHits[p4.Id] = 4;

            this.RefreshPlayerRanking();
        }

        protected void AddPlayer()
        {
            if (this.GameService.TryAddPlayer(this.Gamestate.Id, this.PlayerNameToAdd))
            {
                this.PlayerNameToAdd = null;

                this.RefreshPlayerRanking();
            }
        }

        protected void RemovePlayer()
        {
            if (this.GameService.TryRemovePlayer(this.Gamestate.Id))
            {
                this.RefreshPlayerRanking();
            }
        }

        protected void CreateNewTeam()
        {
            this.GameService.TryAddTeam(this.Gamestate.Id);
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
            this.GameService.TryAddCourse(this.Gamestate.Id, this.CourseParNumberToAdd);
        }

        protected void RemoveCourse()
        {
            this.GameService.TryRemoveCourse(this.Gamestate.Id);
        }

        private void RefreshPlayerRanking()
        {
            List<Player> players = null;
            if (this.SelectedTeam == null)
            {
                players = this.Gamestate.Teams.SelectMany(a => a.Players).ToList();
            }
            else
            {
                players = this.SelectedTeam.Players;
            }

            if (this.Gamestate.Status >= Gamestatus.Running)
            {
                this.RankedPlayers = players
                    .OrderByDescending(a => this.Gamestate.Courses.Count(b => b.PlayerHits[a.Id] != null)) // absteigend nach anzahl gespielter kurse
                    .ThenBy(a => this.Gamestate.Courses.Sum(b => b.PlayerHits[a.Id])) // aufsteigend nach summe der benötigten schläge
                    .ToList();
            }
            else
            {
                this.RankedPlayers = players.ToList();
            }
        }

        protected void CreateNewGame()
        {
            if (this.GameService.TryGetGame(this.GameService.CreateNewGame(), out var gs))
            {
                this.SetCurrentGameState(gs);

                this.RefreshPlayerRanking();

                this.Gamestate.Status = Gamestatus.Configuring;
            }
        }

        protected void FinishGame()
        {
            if (this.Gamestate != null)
            {
                this.Gamestate.Status = Gamestatus.Finished;
                this.Gamestate.FinishTime = DateTime.UtcNow;

                this.Gamestate.RaiseStateChanged(this, Context_GameFinished);
            }
        }

        private void NavigateToGame(string gameid, UserMode mode, int teamNumber)
        {
            // böser hack!:
            // damit die uri im browser mit den parametern befüllt wird, direkt mal nen page-refresh aufrufen
            // kurioserweise muss dieser ca. 1000 ms verzögert werden, da sonst nichts? passiert.
            // komplett ohne verzögern geht nicht, da man sich hier gerade im pre-rendering befindet.
            // sollte diese component nicht als annavigierbare page verwendet werden, darf dieser auf ruf nicht erfolgen!
            var _ = this.InvokeAsync(async () =>
            {
                await Task.Delay(1000);
                this.UriHelper.NavigateTo($"/MiniatureGolf/{gameid}/{(int)mode}/{teamNumber}", true);
            });
        }

        protected void StartGame()
        {
            if (this.Gamestate != null)
            {
                this.Gamestate.Status = Gamestatus.Running;
                this.Gamestate.StartTime = DateTime.UtcNow;

                this.RefreshPlayerRanking();
                this.Gamestate.RaiseStateChanged(this, Context_GameStarted);
            }
        }

        protected void ChangeUserMode(UserMode newMode)
        {
            this.CurrentUserMode = newMode;
            this.RefreshPlayerRanking();

            this.StateHasChanged();
        }

        protected void ToggleUserMode()
        {
            switch (this.CurrentUserMode)
            {
                case UserMode.Editor:
                    this.ChangeUserMode(UserMode.Spectator);
                    break;
                case UserMode.Spectator:
                    this.ChangeUserMode(UserMode.Editor);
                    break;
                default:
                    throw new InvalidOperationException("Invalid Source-UserMode for toggling!");
            }
        }

        private void OnSelectedTeamNumberChanged()
        {
            this.SelectedTeam = this.Gamestate.Teams.SingleOrDefault(a => a.Number == selectedTeamNumber);
            this.RefreshPlayerRanking();
        }

        protected List<Player> GetPlayersInView()
        {
            if (this.SelectedTeam == null)
            {
                return this.Gamestate.Teams.SelectMany(a => a.Players).ToList();
            }
            else
            {
                return this.SelectedTeam.Players.ToList();
            }
        }

        protected Dictionary<string, int?> GetPlayerHitsInViewForCourse(Course c)
        {
            var players = this.GetPlayersInView().Select(a => a.Id);

            return c.PlayerHits.Where(a => players.Contains(a.Key)).ToDictionary(a => a.Key, a => a.Value);
        }

        protected void IncreaseHitCount(Course c, Player p)
        {
            if (c.PlayerHits[p.Id] == null)
            {
                c.PlayerHits[p.Id] = 1;
            }
            else if (c.PlayerHits[p.Id] < 7)
            {
                c.PlayerHits[p.Id]++;
            }
            else if (c.PlayerHits[p.Id] == 7)
            {
                c.PlayerHits[p.Id] = null;
            }

            this.RandoPickAutoRefreshEmoji();
            this.AutoRefreshHelper.Push();

            this.Gamestate.CurrentCourseNumber = c.Number;
            this.Gamestate.RaiseStateChanged(this, Context_HitCount);
        }

        protected void IncreasePar(Course c)
        {
            if (c.Par < 7)
            {
                c.Par++;
            }
            else
            {
                c.Par = 1;
            }

            this.Gamestate.RaiseStateChanged(this, Context_Par);
        }

        protected bool RowShouldBeDisplayedTransparently(Course c)
        {
            if (this.Gamestate.Status != Gamestatus.Running)
                return false;

            if (this.Gamestate.CurrentCourseNumber == null)
                return false;

            if (this.Gamestate.CurrentCourseNumber == c.Number)
                return false;

            return true;
        }

        private void RandoPickAutoRefreshEmoji()
        {
            var rnd = new Random();
            var emoji = this.autoRefreshEmojis[rnd.Next(0, this.autoRefreshEmojis.Count)];

            this.AutoRefreshEmoji = emoji;
        }
        #endregion Methods

        protected class UserModeDropDownItem
        {
            public int ModeId { get; set; }
            public string Name { get; set; }
        }
    }
}