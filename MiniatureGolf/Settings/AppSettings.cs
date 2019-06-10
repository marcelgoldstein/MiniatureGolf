using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Settings
{
    public class AppSettings
    {
        public WorkerSettings WorkerSettings { get; set; }
    }

    public class WorkerSettings
    {
        public int AutoSaveIntervalInSeconds { get; set; }
    }
}
