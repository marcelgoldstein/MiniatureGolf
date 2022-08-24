namespace MiniatureGolf.Models;

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

public enum RankingDisplayMode
{
    Average,
    Sum,
}
