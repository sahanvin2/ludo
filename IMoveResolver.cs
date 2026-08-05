using System.Collections.Generic;

namespace LudoGame
{
    /// <summary>Resolves move validity (AI/simulation) and execution through one engine.</summary>
    public interface IMoveResolver
    {
        MoveOutcome Evaluate(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players);
        MoveSimulation Simulate(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players);
        MoveResult Apply(
            IPlayer player,
            Piece piece,
            int diceValue,
            IBoard board,
            Dictionary<Color, IPlayer> players,
            MysteryCellSystem? mystery,
            IGameLogger logger);
    }

    /// <summary>
    /// Proxy pattern: provides the same interface as the movement subsystem while delegating
    /// all work to the static <see cref="MovementEngine"/> (controls access without duplicating rules).
    /// </summary>
    public sealed class MovementResolver : IMoveResolver
    {
        public MoveOutcome Evaluate(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players) =>
            MovementEngine.Evaluate(player, piece, diceValue, board, players);

        public MoveSimulation Simulate(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players) =>
            MovementEngine.Simulate(player, piece, diceValue, board, players);

        public MoveResult Apply(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players, MysteryCellSystem? mystery, IGameLogger logger) =>
            MovementEngine.Apply(player, piece, diceValue, board, players, mystery, logger);
    }
}



















