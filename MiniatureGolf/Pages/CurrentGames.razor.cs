using Microsoft.AspNetCore.Components;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages
{
    public class CurrentGamesModel : ComponentBase
    {
        #region Properties
        [Inject] public GameService GameService { get; private set; }

        public List<Gamestate> CurrentGames { get; set; } = new List<Gamestate>();
        #endregion Properties

        #region Methods
        protected override Task OnInitAsync()
        {
            this.LoadGames(DateFilter.Day);

            return base.OnInitAsync();
        }

        protected void LoadGames(DateFilter dateFilter)
        {
            this.CurrentGames = this.GameService.GetGames(Gamestatus.Running, dateFilter).OrderBy(a => a.CreationTime).ToList();
        }
        #endregion Methods
    }
}