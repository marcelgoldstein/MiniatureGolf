using Microsoft.AspNetCore.Components;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages
{
    public class GamesListModel : ComponentBase
    {
        #region Properties
        [Inject] public GameService GameService { get; private set; }

        public List<Gamestate> CurrentGames { get; set; } = new List<Gamestate>();

        public List<FilterState> FilterStates { get; set; } = new List<FilterState>();

        public DateFilter SelectedDateFilter { get; set; } = DateFilter.Day;

        private int selectedFilterStateId = -1;
        public int SelectedFilterStateId { get => selectedFilterStateId; set { selectedFilterStateId = value; this.LoadGames(); } }

        private string playerFilterInput;
        public string PlayerFilterInput { get => playerFilterInput; set { playerFilterInput = value; this.LoadGames(); } }
        #endregion Properties

        #region Methods
        protected override Task OnInitAsync()
        {
            this.FillFilterStates();

            this.LoadGames();

            return base.OnInitAsync();
        }

        protected void LoadGames()
        {
            var state = (this.SelectedFilterStateId == -1 ? (Gamestatus?)null : (Gamestatus?)this.SelectedFilterStateId);

            this.CurrentGames = this.GameService
                .GetGames(state, this.SelectedDateFilter)
                .Where(a => string.IsNullOrWhiteSpace(this.PlayerFilterInput) || a.PlayersText.ToLower().Contains(this.PlayerFilterInput.ToLower()))
                .OrderBy(a => a.Game.CreationTime)
                .ThenBy(a => a.Game.FinishTime)
                .ToList();
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

        protected string GetButtonClassForState(Gamestate gs)
        {
            switch ((Gamestatus)gs.Game.StateId)
            {
                case Gamestatus.Created:
                case Gamestatus.Configuring:
                    return "btn-primary";
                case Gamestatus.Running:
                    return "btn-success";
                case Gamestatus.Finished:
                    return "btn-warning";
                default:
                    return string.Empty;
            }
        }
        #endregion Methods
    }

    public class FilterState
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}