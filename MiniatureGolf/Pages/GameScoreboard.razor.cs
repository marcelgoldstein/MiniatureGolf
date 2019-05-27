using Microsoft.AspNetCore.Components;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages
{
    public class GameScoreboardModel : ComponentBase
    {
        #region Properties
        [Inject]
        public GameService GameService { get; private set; }
        [Inject]
        protected IUriHelper UriHelper { get; private set; }

        [Parameter] protected string GameId { get; set; }
        [Parameter] protected string Mode { get; set; }
        [Parameter] protected string TeamNumber { get; set; }

        public Gamestate Gamestate { get; private set; }

        public string PlayerNameToAdd { get; set; }
        public int CourseParNumberToAdd { get; set; } = 3;

        public List<Player> RankedPlayers { get; set; } = new List<Player>();
        public Course CurrentEditCourse { get; set; }
        public decimal DataGridHeight { get; set; }
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
        public Team NullTeamSelection { get; set; } = new Team { Number = -1 };

        public bool IsNotificationWindowVisible { get; set; }
        public bool ShowEditColumn { get; set; } = true;
        #endregion Properties

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
                    this.Gamestate = gs;

                    this.ChangeUserMode(desiredUserMode);

                    if (!string.IsNullOrWhiteSpace(this.TeamNumber))
                    {
                        var t = this.Gamestate.Teams.SingleOrDefault(a => a.Number == Convert.ToInt32(this.TeamNumber));
                        if (t != null)
                        { // ein gültiges team wurde per parameter angesteuert
                            this.SelectedTeamNumber = t.Number;
                        }
                    }

                    this.RefreshDataGridHeight();
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

        private void StartAutoRefreshLoop(Func<bool> keepRunningWhileTrueExpression)
        {
            Task.Run(async () =>
            {
                while (keepRunningWhileTrueExpression())
                {
                    this.RefreshDataGridHeight();
                    this.RefreshPlayerRanking();

                    await this.Invoke(this.StateHasChanged);

                    await Task.Delay(1000);
                }
            });
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
            if (this.GameService.TryAddCourse(this.Gamestate.Id, this.CourseParNumberToAdd))
            {
                this.RefreshDataGridHeight();
            }
        }

        protected void RemoveCourse()
        {
            if (this.GameService.TryRemoveCourse(this.Gamestate.Id))
            {
                this.RefreshDataGridHeight();
            }
        }

        private void RefreshDataGridHeight()
        {
            if (this.Gamestate == null)
            {
                this.DataGridHeight = 26M * 3 + 37M;
                return;
            }

            var rowCount = this.Gamestate.Courses.Count;

            if (rowCount < 3)
            {
                rowCount = 3;
            }

            this.Invoke(() =>
            {
                this.DataGridHeight = 26M * rowCount + 37M;
            });
        }

        protected void StartEdit(Course c)
        {
            if (c != null)
            {
                this.CurrentEditCourse = c;
                this.Gamestate.CurrentCourseNumber = c.Number;

                this.StateHasChanged();
            }
        }

        protected void EndEdit()
        {
            this.CurrentEditCourse = null;

            this.RefreshPlayerRanking();
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

            if (this.CurrentUserMode == UserMode.Editor)
            {
                // fießer workaround, damit die EditButtonSpalte auf der richtigen position dargestellt wird (nur für den editor nötig)
                this.ShowEditColumn = !this.ShowEditColumn;
                this.StateHasChanged();
                var _ = this.InvokeAsync(async () =>
                {
                    await Task.Delay(1);
                    this.ShowEditColumn = !this.ShowEditColumn;
                    this.StateHasChanged();
                });
            }
        }

        protected void CreateNewGame()
        {
            if (this.GameService.TryGetGame(this.GameService.CreateNewGame(), out var gs))
            {
                this.Gamestate = gs;

                this.RefreshPlayerRanking();

                this.Gamestate.Status = Gamestatus.Configuring;
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
                this.RefreshDataGridHeight();
            }
        }

        protected void ChangeUserMode(UserMode newMode)
        {
            this.CurrentUserMode = newMode;
            this.RefreshPlayerRanking();
            this.RefreshDataGridHeight();

            switch (newMode)
            {
                case UserMode.Editor:
                    break;
                case UserMode.Spectator:
                case UserMode.SpectatorReadOnly:
                    this.StartAutoRefreshLoop(() => (this.CurrentUserMode switch { UserMode.Spectator => true, UserMode.SpectatorReadOnly => true, _ => false }));
                    break;
                default:
                    throw new NotImplementedException();
            }

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

            if (this.CurrentUserMode == UserMode.Editor && this.CurrentEditCourse != null)
            {
                this.CurrentEditCourse = null;
            }
        }

        protected class UserModeDropDownItem
        {
            public int ModeId { get; set; }
            public string Name { get; set; }
        }
    }
}