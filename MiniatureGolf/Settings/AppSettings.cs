using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Settings
{
    public class AppSettings
    {
        public WorkerSettings WorkerSettings { get; set; }
        public SpecialDays SpecialDays { get; set; }
    }
    
    public class WorkerSettings
    {
        public AutoSaveWorkerSettings AutoSaveWorkerSettings { get; set; }
        public UnstartedGamesCleanerSettings UnstartedGamesCleanerSettings { get; set; }
        public IdleGamesCacheCleanerSettings IdleGamesCacheCleanerSettings { get; set; }
    }

    public class AutoSaveWorkerSettings
    {
        public int AutoSaveIntervalInSeconds { get; set; }
    }

    public class UnstartedGamesCleanerSettings
    {
        public int WorkerIntervallInMinutes { get; set; }
        public int IdleTimeInHours { get; set; }
    }

    public class IdleGamesCacheCleanerSettings
    {
        public int WorkerIntervallInMinutes { get; set; }
        public int IdleTimeInMinutes { get; set; }
    }

    public class SpecialDays
    {
        public string Dates { get; set; }
        public string Headertext { get; set; }
    }
}
