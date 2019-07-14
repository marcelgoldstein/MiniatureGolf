using MiniatureGolf.DAL;
using System;

namespace MiniatureGolf.Models
{
    public class Gamestate : LightweightGamestate
    {
        #region Properties
        public MiniatureGolfContext GameDbContext { get; set; }
        public bool IsAutoSaveActive { get; set; }
        public DateTime? MostRecentIsActivelyUsedHeartbeatTime { get; set; } // wird regelmäßig aktualisiert, wenn das game im GameScoreboard geöffnet ist
        #endregion Properties

        #region Events
        public event StateChangedHandler StateChanged;
        public delegate void StateChangedHandler(object sender, object caller, StateChangedContext context);

        public void RaiseStateChanged(object caller, StateChangedContext context) => this.StateChanged?.Invoke(this, caller, context);
        #endregion Events
    }

    public class StateChangedContext
    {
        public string Key { get; set; }
        public object Payload { get; set; }
    }
}
