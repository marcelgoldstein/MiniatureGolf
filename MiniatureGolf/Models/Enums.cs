using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniatureGolf.Models
{
    public enum Gamestatus
    {
        Created = 1,
        Configuring = 5,
        Running = 10,
        Finished = 15,
    }

    public enum UserMode
    {
        Editor = 0,
        Spectator = 1,
        SpectatorReadOnly = 2,
    }
}
