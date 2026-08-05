using System.Collections.Generic;

namespace LudoGame
{
    /// <summary>
    /// Builds the four AI player instances used in the simulation.
    /// Factory pattern: centralises instantiation so the game manager depends on IPlayer, not concrete classes.
    /// This also supports the assignment requirement for clean dependency injection and testability.
    /// </summary>
    public static class PlayerFactory
    {
        public static Dictionary<Color, IPlayer> CreatePlayers() => new()
        {
            [Color.Red] = new RedPlayer(),
            [Color.Green] = new GreenPlayer(),
            [Color.Yellow] = new YellowPlayer(),
            [Color.Blue] = new BluePlayer()
        };
    }
}
