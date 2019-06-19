using Microsoft.AspNetCore.Components;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using MiniatureGolf.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages
{
    public class GamesListModel : ComponentBase
    {
        #region Properties
        [Inject] public GameService GameService { get; private set; }

        public List<LightweightGamestate> CurrentGames { get; set; } = new List<LightweightGamestate>();

        public List<FilterState> FilterStates { get; set; } = new List<FilterState>();

        public DateFilter SelectedDateFilter { get; set; } = DateFilter.Day;

        private int selectedFilterStateId = -1;
        public int SelectedFilterStateId { get => selectedFilterStateId; set { selectedFilterStateId = value; this.LoadGames(); } }

        private string playerFilterInput;
        public string PlayerFilterInput { get => playerFilterInput; set { playerFilterInput = value; this.LoadGames(); } }

        public bool IsGridBusyIndicatorVisible { get; set; }
        public bool IsGridBusyIndicatorOpactyAnimationTrigger { get; set; }
        protected RedundantExecutionSuppressor IsGridBusyIndicatorHelper { private set; get; }
        #endregion Properties

        #region ctor
        public GamesListModel()
        {
            this.IsGridBusyIndicatorHelper = new RedundantExecutionSuppressor(async (t) =>
            {
                await this.LoadGamesInternalAsync(t);
            }, TimeSpan.FromSeconds(0.2));
        }
        #endregion ctor

        #region Methods
        protected override Task OnInitAsync()
        {
            this.FillFilterStates();

            this.LoadGames();

            return base.OnInitAsync();
        }

        protected void LoadGames()
        {
            this.IsGridBusyIndicatorHelper.Push();
        }

        private async Task LoadGamesInternalAsync(CancellationToken t)
        {
            this.IsGridBusyIndicatorVisible = true;
            this.IsGridBusyIndicatorOpactyAnimationTrigger = true;
            this.CurrentGames = new List<LightweightGamestate>();
            await this.Invoke(this.StateHasChanged);

            var state = (this.SelectedFilterStateId == -1 ? (Gamestatus?)null : (Gamestatus?)this.SelectedFilterStateId);
            var dateFilter = this.SelectedDateFilter;
            var playerFilterInput = this.PlayerFilterInput?.ToLower();

            await Task.Run(() =>
            {
                var games = this.GameService
                    .GetGamesLightweight(state, dateFilter)
                        .Where(a => string.IsNullOrWhiteSpace(playerFilterInput) || a.PlayersText.ToLower().Contains(playerFilterInput))
                        .OrderBy(a => a.Game.CreationTime)
                        .ThenBy(a => a.Game.FinishTime)
                        .ToList();
                t.ThrowIfCancellationRequested();
                this.CurrentGames = games;
            });

            this.IsGridBusyIndicatorOpactyAnimationTrigger = false;
            await this.Invoke(this.StateHasChanged);
            await Task.Delay(1000);
            t.ThrowIfCancellationRequested();
            this.IsGridBusyIndicatorVisible = false;
            await this.Invoke(this.StateHasChanged);
        }

        protected void FillFilterStates()
        {
            this.FilterStates.Clear();

            this.FilterStates.Add(new FilterState { Id = -1, Name = "all" });
            foreach (Gamestatus filterState in Enum.GetValues(typeof(Gamestatus)))
            {
                this.FilterStates.Add(new FilterState { Id = (int)filterState, Name = Enum.GetName(typeof(Gamestatus), filterState).ToLower() });
            }
        }

        protected string GetButtonClassForState(LightweightGamestate gs)
        {
            return gs.Status switch
            {
                var s when 
                    s == Gamestatus.Created || 
                    s == Gamestatus.Configuring => "btn-primary",
                Gamestatus.Running => "btn-success",
                Gamestatus.Finished => "btn-warning",
                _ => string.Empty,
            };
        }
        #endregion Methods
    }

    public class FilterState
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}