using System;
using System.Timers;

namespace MiniatureGolf.Tools
{
    public class RedundantExecutionSuppressor
    {
        #region Variables
        private Timer timer;
        #endregion Variables

        #region ctor
        public RedundantExecutionSuppressor(Action toExecuteAction, TimeSpan delay)
        {
            this.timer = new Timer { AutoReset = false, Interval = delay.TotalMilliseconds };
            this.timer.Elapsed += (s, e) => toExecuteAction();
        }
        #endregion ctor

        #region Methods
        public void Push()
        {
            this.timer.Stop();
            this.timer.Start();
        }
        #endregion Methods
    }
}
