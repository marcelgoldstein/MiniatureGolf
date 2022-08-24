using Microsoft.AspNetCore.Components;
using MiniatureGolf.Models;
using MiniatureGolf.Services;
using MiniatureGolf.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniatureGolf.Pages;

public class GamesListModel : ComponentBase
{
    #region Properties
    [Inject] public GameService GameService { get; private set; }

    public List<LightweightGamestate> CurrentGames { get; set; } = new List<LightweightGamestate>();

    public List<FilterState> FilterStates { get; set; } = new List<FilterState>();

    public DateFilter SelectedDateFilter { get; set; } = DateFilter.Day;

    private int selectedFilterStateId = -1;
    public int SelectedFilterStateId { get => selectedFilterStateId; set { selectedFilterStateId = value; LoadGames(); } }

    private string playerFilterInput;
    public string PlayerFilterInput { get => playerFilterInput; set { playerFilterInput = value; LoadGames(); } }

    public bool IsGridBusyIndicatorVisible { get; set; }
    public bool IsGridBusyIndicatorOpactyAnimationTrigger { get; set; }
    protected RedundantExecutionSuppressor IsGridBusyIndicatorHelper { private set; get; }

    protected RankingDisplayMode RankingDisplayMode { get; set; } = RankingDisplayMode.Average;
    #endregion Properties

    #region ctor
    public GamesListModel()
    {
        IsGridBusyIndicatorHelper = new RedundantExecutionSuppressor(async (t) =>
        {
            await LoadGamesInternalAsync(t);
        }, TimeSpan.FromSeconds(0.2));
    }
    #endregion ctor

    #region Methods
    protected override Task OnInitializedAsync()
    {
        FillFilterStates();

        LoadGames();

        return base.OnInitializedAsync();
    }

    protected void LoadGames()
    {
        IsGridBusyIndicatorHelper.Push();
    }

    private async Task LoadGamesInternalAsync(CancellationToken t)
    {
        IsGridBusyIndicatorVisible = true;
        IsGridBusyIndicatorOpactyAnimationTrigger = true;
        CurrentGames = new List<LightweightGamestate>();
        await InvokeAsync(StateHasChanged);

        var state = (SelectedFilterStateId == -1 ? (Gamestatus?)null : (Gamestatus?)SelectedFilterStateId);
        var dateFilter = SelectedDateFilter;
        var playerFilterInput = PlayerFilterInput?.ToLower();

        await Task.Run(() =>
        {
            var games = GameService
                .GetGamesLightweight(state, dateFilter)
                    .Where(a => string.IsNullOrWhiteSpace(playerFilterInput) || (RankingDisplayMode switch { RankingDisplayMode.Average => a.PlayersTextForAvgRanking, RankingDisplayMode.Sum => a.PlayersTextForSumRanking, _ => throw new NotImplementedException()}).ToLower().Contains(playerFilterInput))
                    .OrderBy(a => a.Game.CreationTime)
                    .ThenBy(a => a.Game.FinishTime)
                    .ToList();
            t.ThrowIfCancellationRequested();
            CurrentGames = games;
        }, t);

        IsGridBusyIndicatorOpactyAnimationTrigger = false;
        await InvokeAsync(StateHasChanged);
        await Task.Delay(1000, t);
        t.ThrowIfCancellationRequested();
        IsGridBusyIndicatorVisible = false;
        await InvokeAsync(StateHasChanged);
    }

    protected void FillFilterStates()
    {
        FilterStates.Clear();

        FilterStates.Add(new FilterState { Id = -1, Name = "all" });
        foreach (Gamestatus filterState in Enum.GetValues(typeof(Gamestatus)))
        {
            FilterStates.Add(new FilterState { Id = (int)filterState, Name = Enum.GetName(typeof(Gamestatus), filterState).ToLower() });
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

    public void SetRankingDisplayMode(RankingDisplayMode rankingDisplayMode)
    {
        RankingDisplayMode = rankingDisplayMode;
    }
    #endregion Methods
}

public class FilterState
{
    public int Id { get; set; }
    public string Name { get; set; }
}