﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace MiniatureGolf.Tools
{
    public class RedundantExecutionSuppressor
    {
        #region Variables
        private readonly TimeSpan delay;
        private readonly Timer timer;
        private readonly Stopwatch watch = new Stopwatch();
        private Task refresher = null;
        private bool stopRefresher = false;
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
        public RedundantExecutionSuppressor(Action toExecuteAction, TimeSpan delay)
        {
            this.delay = delay;
            this.timer = new Timer { AutoReset = false, Interval = delay.TotalMilliseconds };
            this.timer.Elapsed += (s, e) =>
            {
                this.watch.Stop();
                this.Progress = 1.0D;
                this.stopRefresher = true;
                this.refresher = null;
                this.IsRunning = false;

                toExecuteAction();
            };
        }
        #endregion ctor

        #region Methods
        public void Push()
        {
            if (this.timer.Enabled == false)
            {
                this.Progress = 0D;
            }

            this.IsRunning = true;
            this.timer.Stop();
            this.timer.Start();
            this.watch.Restart();

            this.stopRefresher = false;
            if (this.refresher == null)
            {
                this.refresher = Task.Run(async () =>
                {
                    while (this.stopRefresher == false)
                    {
                        await Task.Delay(50);
                        this.RefreshProgress(delay, this.watch.Elapsed);
                    }
                });
            }
        }

        private void RefreshProgress(TimeSpan total, TimeSpan elapsed)
        {
            this.Progress = elapsed / total;
            this.ProgressChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion Methods
    }
}
