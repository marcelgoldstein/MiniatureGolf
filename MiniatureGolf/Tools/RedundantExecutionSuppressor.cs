using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MiniatureGolf.Tools;

public class RedundantExecutionSuppressor
{
    #region Variables
    private readonly TimeSpan delay;
    private readonly Timer timer;
    private readonly Stopwatch watch = new Stopwatch();
    private Task refresher = null;
    private bool stopRefresher = false;
    private CancellationTokenSource cts;
    #endregion Variables

    #region Properties
    public double Progress { get; private set; } = 0.0D;
    public bool IsRunning { get; set; }
    #endregion Properties

    #region Events
    public event ProgressChangedHandler ProgressChanged;
    public delegate void ProgressChangedHandler(object sender, EventArgs e);
    #endregion Events

    #region ctor
    public RedundantExecutionSuppressor(Func<CancellationToken, Task> toExecuteAction, TimeSpan delay)
    {
        this.delay = delay;
        timer = new Timer { AutoReset = false, Interval = delay.TotalMilliseconds };
        timer.Elapsed += async (s, e) =>
        {
            watch.Stop();
            Progress = 1.0D;
            stopRefresher = true;
            refresher = null;
            IsRunning = false;

            try
            {
                await toExecuteAction(cts.Token);
            }
            catch (OperationCanceledException)
            { }
        };
    }
    #endregion ctor

    #region Methods
    public void Push()
    {
        cts?.Cancel(); // falls bereits ein invoke ausgeführt wird, soll dieses wenn möglich abgebrochen werden
        cts = new CancellationTokenSource();

        if (timer.Enabled == false)
        {
            Progress = 0D;
        }

        IsRunning = true;
        timer.Stop();
        timer.Start();
        watch.Restart();

        stopRefresher = false;
        if (refresher == null)
        {
            refresher = Task.Run(async () =>
            {
                while (stopRefresher == false)
                {
                    await Task.Delay(50);
                    RefreshProgress(delay, watch.Elapsed);
                }
            });
        }
    }

    private void RefreshProgress(TimeSpan total, TimeSpan elapsed)
    {
        Progress = elapsed / total;
        ProgressChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion Methods
}
