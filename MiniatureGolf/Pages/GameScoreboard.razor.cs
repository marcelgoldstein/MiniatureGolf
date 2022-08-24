using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
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
using Telerik.Blazor.Components;

namespace MiniatureGolf.Pages;

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
    [Inject] protected NavigationManager NavigationManager { get; private set; }
    [Inject] protected IOptions<AppSettings> AppSettings { get; private set; }
    [Inject] protected IHostApplicationLifetime applicationLifetime { get; private set; }

    [Parameter] public string GameId { get; set; }
    [Parameter] public string Mode { get; set; }
    [Parameter] public string TeamNumber { get; set; }
    [Parameter] public string RankingMode { get; set; }

    public Gamestate Gamestate { get; private set; }

    public string PlayerNameToAdd { get; set; }


    private int courseHitLimit = 7;
    public int CourseHitLimit { get => courseHitLimit; set { courseHitLimit = value; RecalibrateCoursePars(value); Gamestate.Game.CourseHitLimit = CourseHitLimit; } }

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
        set { selectedTeamNumber = value; OnSelectedTeamNumberChanged(); }
    }
    public Team SelectedTeam { get; set; }

    public bool IsNotificationWindowVisible { get; set; }

    /// <summary>
    /// Wird verwendet, um eine Verzögerung der Aktualisierung zu bewirken, wenn der Editierer Punkte geändert hat. 
    /// </summary>
    protected RedundantExecutionSuppressor AutoRefreshHelper { private set; get; }
    protected TelerikAnimationContainer AutoRefreshAnimationContainer { get; set; }

    public string AutoRefreshEmoji { get; set; } = "😜";

    public bool ShowOuterViewEditOverlay { get; set; }
    public bool OuterViewEditOverlayAnimationTrigger { get; set; }
    protected RedundantExecutionSuppressor OuterViewEditOverlayHelper { private set; get; }

    protected RankingDisplayMode RankingDisplayMode { get; set; } = RankingDisplayMode.Average;

    #endregion Properties

    #region ctor
    public GameScoreboardModel()
    {
        AutoRefreshHelper = new RedundantExecutionSuppressor(async (t) => 
        {
            if (disposedValue == false)
                RefreshPlayerRanking();

            if (disposedValue == false && AutoRefreshAnimationContainer != null && CurrentUserMode == UserMode.Editor && Gamestate.Status == Gamestatus.Running)
                await AutoRefreshAnimationContainer.HideAsync();

            if (disposedValue == false)
                await InvokeAsync(StateHasChanged); 
        }, TimeSpan.FromSeconds(3));

        AutoRefreshHelper.ProgressChanged += (sender, e) =>
        {
            _ = InvokeAsync(StateHasChanged);
        };

        OuterViewEditOverlayHelper = new RedundantExecutionSuppressor(async (t) => { OuterViewEditOverlayAnimationTrigger = false; await InvokeAsync(StateHasChanged); await Task.Delay(1000, t); t.ThrowIfCancellationRequested(); ShowOuterViewEditOverlay = false; await InvokeAsync(StateHasChanged); }, TimeSpan.FromSeconds(3));
    }
    #endregion ctor

    #region Methods
    protected override Task OnInitializedAsync()
    {
        ShareModes = new List<UserModeDropDownItem>
        {
            new UserModeDropDownItem{ ModeId = (int)UserMode.Editor, Name = "editor" },
            new UserModeDropDownItem{ ModeId = (int)UserMode.Spectator, Name = "spectator" },
            new UserModeDropDownItem{ ModeId = (int)UserMode.SpectatorReadOnly, Name = "read only" },
        };

        return base.OnInitializedAsync();
    }

    protected override Task OnParametersSetAsync()
    {
        UserMode desiredUserMode;
        if (string.IsNullOrWhiteSpace(GameId))
        { // sample game erstellen und dann per navigate als editor öffnen
            desiredUserMode = UserMode.Editor;
            CreateSampleGame();
            ChangeUserMode(desiredUserMode);
            SelectedShareMode = (int)UserMode.Editor;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Mode))
            { // ohne parameter wird SpectatorReadOnly drauß
                desiredUserMode = UserMode.SpectatorReadOnly;
            }
            else
            {
                desiredUserMode = (UserMode)Convert.ToInt32(Mode);

                if (desiredUserMode == UserMode.Editor)
                    SelectedShareMode = (int)UserMode.Editor;
            }

            if (GameService.TryGetGame(GameId, out var gs))
            {
                SetCurrentGameState(gs);

                ChangeUserMode(desiredUserMode);

                if (!string.IsNullOrWhiteSpace(TeamNumber))
                {
                    var t = Gamestate.Game.Teams.SingleOrDefault(a => a.Number == Convert.ToInt32(TeamNumber));
                    if (t != null)
                    { // ein gültiges team wurde per parameter angesteuert
                        SelectedTeamNumber = t.Number;
                    }
                }

                RefreshPlayerRanking();
            }
            else
            { // ungültie gameId übergeben
                IsNotificationWindowVisible = true;
                CreateSampleGame();
                ChangeUserMode(UserMode.Editor);
                SelectedShareMode = (int)UserMode.Editor;
            }
        }

        mostRecentTouchUpdaterTaskToken = new CancellationTokenSource();

        _ = applicationLifetime.ApplicationStopping.Register(() =>
        {
            mostRecentTouchUpdaterTaskToken.Cancel();
        });

        _ = StartMostRecentTouchUpdater(mostRecentTouchUpdaterTaskToken.Token);

        if (RankingMode != null)
            RankingDisplayMode = Enum.Parse<RankingDisplayMode>(RankingMode); 

        return base.OnParametersSetAsync();
    }

    private async Task StartMostRecentTouchUpdater(CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            while (ct.IsCancellationRequested == false)
            {
                await Task.Delay(TimeSpan.FromMinutes(AppSettings.Value.WorkerSettings.IdleGamesCacheCleanerSettings.WorkerIntervallInMinutes - (AppSettings.Value.WorkerSettings.IdleGamesCacheCleanerSettings.WorkerIntervallInMinutes / 10D)));

                if (Gamestate != null)
                    Gamestate.MostRecentIsActivelyUsedHeartbeatTime = DateTime.UtcNow;
            }
        }, ct);
    }

    public void SetCurrentUserMode(UserMode userMode)
    {
        CurrentUserMode = userMode;
    }

    public void SetRankingDisplayMode(RankingDisplayMode rankingDisplayMode)
    {
        RankingDisplayMode = rankingDisplayMode;
    }

    private void SetCurrentGameState(Gamestate gs)
    {
        if (Gamestate != null)
            Gamestate.StateChanged -= Gamestate_StateChanged;

        Gamestate = gs;
        CourseHitLimit = Gamestate.Game.CourseHitLimit;
        SelectedTeamNumber = 0;

        if (Gamestate != null)
            Gamestate.StateChanged += Gamestate_StateChanged;

        Gamestate.MostRecentIsActivelyUsedHeartbeatTime = DateTime.UtcNow;
    }

    private void Gamestate_StateChanged(object sender, object caller, StateChangedContext context)
    {
        if (caller == this)
            return; // nur events, welche von anderen  Page-Instanzen ausgelöst wurden sind relevant

        if ((sender as Gamestate)?.Game.GUID != Gamestate?.Game.GUID)
            return; // nicht das eigene 'game'

        if (context.Key == Context_HitCount)
        {
            if (SelectedTeam.TeamPlayers.Select(a => a.Player).Any(a => a == (context.Payload as Player)))
            { // nur wenn der betroffenen Spieler im aktuell angezeigten Team ist
                ShowOuterViewEditOverlay = true;
                OuterViewEditOverlayAnimationTrigger = true;
                RefreshPlayerRanking();
                OuterViewEditOverlayHelper.Push();
            }
        }

        if (context.Key == Context_Teams_Players)
        {
            if (Gamestate.Game.Teams.Contains(SelectedTeam) == false)
            { // ist das ausgewählte team nicht mehr vorhande, das default-team (0) auswählen
                SelectedTeamNumber = 0;
            }

            RefreshPlayerRanking();
        }

        _ = InvokeAsync(StateHasChanged);
    }

    private void CreateSampleGame(int recursiveCounter = 0)
    {
        CreateNewGame();

        if (Gamestate == null)
        {
            if (recursiveCounter <= 10)
            {
                CreateSampleGame(++recursiveCounter);
                return;
            }
        }

        CourseParNumberToAdd = 4;
        AddCourse();
        CourseParNumberToAdd = 3;
        AddCourse();
        CourseParNumberToAdd = 5;
        AddCourse();
        CourseParNumberToAdd = 2;
        AddCourse();
        CourseParNumberToAdd = 3;
        AddCourse();


        PlayerNameToAdd = "Peter";
        AddPlayer();
        PlayerNameToAdd = "Hugo";
        AddPlayer();
        PlayerNameToAdd = "Melissa";
        AddPlayer();
        PlayerNameToAdd = "Saul";
        AddPlayer();

        // Peter
        var p1 = Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[0].Player;
        IncreaseHitCountInternal(Gamestate.Game.Courses[0], p1, 5);
        IncreaseHitCountInternal(Gamestate.Game.Courses[1], p1, 4);
        IncreaseHitCountInternal(Gamestate.Game.Courses[2], p1, 5);
        IncreaseHitCountInternal(Gamestate.Game.Courses[3], p1, 2);
        IncreaseHitCountInternal(Gamestate.Game.Courses[4], p1, 5);

        // Hugo
        var p2 = Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[1].Player;
        IncreaseHitCountInternal(Gamestate.Game.Courses[0], p2, 3);
        IncreaseHitCountInternal(Gamestate.Game.Courses[1], p2, 2);
        IncreaseHitCountInternal(Gamestate.Game.Courses[2], p2, 3);
        IncreaseHitCountInternal(Gamestate.Game.Courses[3], p2, 3);
        IncreaseHitCountInternal(Gamestate.Game.Courses[4], p2, 4);

        // Melissa
        var p3 = Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[2].Player;
        IncreaseHitCountInternal(Gamestate.Game.Courses[0], p3, 2);
        IncreaseHitCountInternal(Gamestate.Game.Courses[1], p3, 4);
        IncreaseHitCountInternal(Gamestate.Game.Courses[2], p3, 4);
        IncreaseHitCountInternal(Gamestate.Game.Courses[3], p3, 2);
        IncreaseHitCountInternal(Gamestate.Game.Courses[4], p3, 1);

        // Saul
        var p4 = Gamestate.Game.Teams.Single(a => a.IsDefaultTeam).TeamPlayers[3].Player;
        IncreaseHitCountInternal(Gamestate.Game.Courses[0], p4, 4);
        IncreaseHitCountInternal(Gamestate.Game.Courses[1], p4, 3);
        IncreaseHitCountInternal(Gamestate.Game.Courses[2], p4, 4);
        IncreaseHitCountInternal(Gamestate.Game.Courses[3], p4, 2);
        IncreaseHitCountInternal(Gamestate.Game.Courses[4], p4, 4);

        RefreshPlayerRanking();
    }

    protected void AddPlayer()
    {
        if (GameService.TryAddPlayer(Gamestate.Game.GUID, PlayerNameToAdd))
        {
            PlayerNameToAdd = null;

            RefreshPlayerRanking();

            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Teams_Players });
        }
    }

    protected void RemovePlayer()
    {
        if (GameService.TryRemovePlayer(Gamestate.Game.GUID))
        {
            if (Gamestate.Game.Teams.Contains(SelectedTeam) == false)
            { // ist das ausgewählte team nicht mehr vorhande, das default-team (0) auswählen
                SelectedTeamNumber = 0;
            }

            RefreshPlayerRanking();

            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Teams_Players });
        }
    }

    protected void CreateNewTeam()
    {
        if (GameService.TryAddTeam(Gamestate.Game.GUID))
        {
            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Teams_Players });
        }
    }

    /// <summary>
    /// Removes all Columns and readds them so the order is correct.
    /// huge buggability here!!! only use with caution
    /// </summary>
    protected void ResetColumnsOrder()
    {
        ShowColumns = false;

        var _ = InvokeAsync(async () =>
        {
            await Task.Delay(1);
            ShowColumns = true;
            StateHasChanged();
        });
    }

    protected void AddCourse()
    {
        if (GameService.TryAddCourse(Gamestate.Game.GUID, CourseParNumberToAdd))
        {
            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Courses });
        }
    }

    protected void RemoveCourse()
    {
        if (GameService.TryRemoveCourse(Gamestate.Game.GUID))
        {
            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Courses });
        }
    }

    private void RefreshPlayerRanking()
    {
        if (SelectedTeam == null)
        {
            SelectedTeamNumber = 0;
        }

        var players = SelectedTeam.TeamPlayers.Select(a => a.Player);

        if (Gamestate.Status >= Gamestatus.Running)
        {
            players = Gamestate.Status switch
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

        RankedPlayers = players.ToList();
    }

    protected void CreateNewGame()
    {
        if (GameService.TryGetGame(GameService.CreateNewGame(), out var gs))
        {
            SetCurrentGameState(gs);

            RefreshPlayerRanking();

            Gamestate.Game.State = (int)Gamestatus.Configuring;
        }
    }

    protected void FinishGame()
    {
        if (Gamestate != null)
        {
            Gamestate.Game.State = (int)Gamestatus.Finished;
            Gamestate.Game.FinishTime = DateTime.UtcNow;

            Gamestate.IsAutoSaveActive = false; // nach fertigstellen die autosave-modus wieder deaktivieren

            GameService.SaveToDatabase(Gamestate);

            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_GameFinished });
        }
    }

    protected void StartGame()
    {
        if (Gamestate != null)
        {
            // remove empty teams that remained from configuration
            GameService.RemoveEmptyTeams(Gamestate.Game.GUID);

            Gamestate.Game.State = (int)Gamestatus.Running;
            Gamestate.Game.StartTime = DateTime.UtcNow;

            RefreshPlayerRanking();
            Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_GameStarted });
        }
    }

    protected void ChangeUserMode(UserMode newMode)
    {
        CurrentUserMode = newMode;
        RefreshPlayerRanking();

        if (newMode == UserMode.Editor && Gamestate.Status <= Gamestatus.Running)
        { // gamestate als 'IsAutoSaveActive' markieren, da dort potenziell änderungen geschehen
            Gamestate.IsAutoSaveActive = true;
        }

        StateHasChanged();
    }

    private void OnSelectedTeamNumberChanged()
    {
        SelectedTeam = Gamestate.Game.Teams.SingleOrDefault(a => a.Number == selectedTeamNumber);
        RefreshPlayerRanking();
    }

    protected CourseState GetCurrentCourseStateForView(Course c)
    {
        var relevantPlayers = SelectedTeam.TeamPlayers.Select(a => a.Player);
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

        RandoPickAutoRefreshEmoji();

        if (disposedValue == false)
            AutoRefreshHelper.Push();

        if (disposedValue == false && AutoRefreshAnimationContainer != null && CurrentUserMode == UserMode.Editor && Gamestate.Status == Gamestatus.Running)
            _ = AutoRefreshAnimationContainer.ShowAsync();

        if (Gamestate.Status == Gamestatus.Running)
            SelectedTeam.CurrentCourseNumber = c.Number;

        Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_HitCount, Payload = p });
    }

    private void IncreaseHitCountInternal(Course c, Player p, int step = 1)
    {
        if (!(p.PlayerCourseHits.SingleOrDefault(a => a.Course == c) is PlayerCourseHit pch))
        {
            pch = new PlayerCourseHit();
            pch.Course = c;
            pch.Player = p;
            p.PlayerCourseHits.Add(pch);
        }

        pch.HitCount = (pch.HitCount ?? 0) + step;

        // modulo 8, damit ein überrollen angewendet wird (8, da null/0 bis 7 gleich 8 elemente)
        // statt 7 in der formel gilt 'CourseHitLimit'
        pch.HitCount %= (CourseHitLimit + 1);
        // dann 0 durch null ersetzen
        pch.HitCount = pch.HitCount == 0 ? null : pch.HitCount;
    }

    protected void IncreasePar(Course c)
    {
        if (c.Par < CourseHitLimit)
        {
            c.Par++;
        }
        else
        {
            c.Par = 1;
        }

        Gamestate.RaiseStateChanged(this, new StateChangedContext { Key = Context_Par });
    }

    protected bool RowShouldBeDisplayedTransparently(Course c)
    {
        if (Gamestate.Game.State != (int)Gamestatus.Running)
            return false;

        if (SelectedTeam.CurrentCourseNumber == null)
            return false;

        if (SelectedTeam.CurrentCourseNumber == c.Number)
            return false;

        return true;
    }

    private void RandoPickAutoRefreshEmoji()
    {
        var rnd = new Random();
        var emoji = autoRefreshEmojis[rnd.Next(0, autoRefreshEmojis.Count)];

        AutoRefreshEmoji = emoji;
    }

    protected void RecalibrateCoursePars(int upperLimit)
    {
        if (CourseParNumberToAdd > upperLimit)
            CourseParNumberToAdd = upperLimit;

        foreach (var course in Gamestate.Game.Courses)
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
                AutoRefreshAnimationContainer = null;
                mostRecentTouchUpdaterTaskToken.Cancel();
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