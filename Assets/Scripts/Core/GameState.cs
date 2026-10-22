namespace Cellstorm.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelUp,
        Won,
        Dead,
        /// <summary>Choosing the starting swarm at the beginning of a run (time is frozen).</summary>
        LoadoutSelect
    }
}
