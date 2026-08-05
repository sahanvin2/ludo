using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoGame
{
    /// <summary>
    /// Player colours used by the LUDO-T game.
    /// </summary>
    public enum Color { Red, Yellow, Green, Blue }

    /// <summary>
    /// Piece lifecycle states in the game.
    /// </summary>
    public enum PieceState { Base, StandardPath, HomeStraight, Home }

    /// <summary>
    /// Movement direction on the standard ring.
    /// </summary>
    public enum Direction { Clockwise, CounterClockwise }

    /// <summary>
    /// Special board locations used for mystery teleportation.
    /// </summary>
    public enum SpecialLocation { Alpha, Beta, Gamma, Base, X, Approach }

    /// <summary>
    /// Constants for board layout, special positions, and home path indexing.
    /// </summary>
    public static class GameConstants
    {
        public const int BoardSize = 52;
        /// <summary>Five home-straight cells indexed 0..4. A piece finishes when home index + dice roll equals <see cref="HomeStraightFinishStep"/> (5).</summary>
        public const int HomeStraightCellCount = 5;
        public const int HomePathMinPosition = 0;
        public const int HomePathMaxPosition = HomeStraightCellCount - 1;
        public const int HomeStraightFinishStep = HomeStraightCellCount;
        public const int HomeStraightLength = HomeStraightFinishStep;

        public const int AlphaPosition = 9;
        public const int BetaPosition = 27;
        public const int GammaPosition = 46;

        public static readonly Dictionary<Color, int> ApproachCells = new()
        {
            { Color.Yellow, 0 }, { Color.Blue, 13 }, { Color.Red, 26 }, { Color.Green, 39 }
        };

        public static readonly Dictionary<Color, int> XPositions = new()
        {
            { Color.Yellow, 1 }, { Color.Blue, 14 }, { Color.Red, 27 }, { Color.Green, 40 }
        };
    }

    /// <summary>
    /// Tracks temporary status effects produced by mystery teleport events.
    /// </summary>
    public class PieceEffect
    {
        public int EnergizedRounds { get; set; }
        public int SickRounds { get; set; }
        public int BriefingRounds { get; set; }
        public int ConsecutiveThrees { get; set; }
    }

    /// <summary>
    /// Represents a single player's token and its game state.
    /// Includes ring position, home path progress, movement direction, and active effects.
    /// </summary>
    public class Piece
    {
        public Color Color { get; }
        public string Name { get; }
        public int Number { get; }
        public PieceState State { get; set; } = PieceState.Base;
        public Direction MovementDirection { get; set; } = Direction.Clockwise;
        /// <summary>T-5: direction assigned at X when piece first entered the board.</summary>
        public Direction DirectionAtX { get; set; } = Direction.Clockwise;
        /// <summary>Counts approach passes for CCW pieces, used by T-7 and two-pass home entry.</summary>
        public int ApproachPassCount { get; set; }
        public int Position { get; set; } = -1;
        /// <summary>Tracks captures made by this piece, needed for home entry restrictions.</summary>
        public int Captures { get; set; }
        public PieceEffect Effect { get; set; } = new();
        public int HomeStraightPosition { get; set; } = -1;

        public Piece(Color color, int number)
        {
            Color = color;
            Number = number;
            Name = $"{color.ToString().ToUpper()[0]}{number}";
        }

        public void ResetToBase()
        {
            State = PieceState.Base;
            Position = -1;
            Captures = 0;
            ApproachPassCount = 0;
            MovementDirection = Direction.Clockwise;
            DirectionAtX = Direction.Clockwise;
            Effect = new PieceEffect();
            HomeStraightPosition = -1;
        }

        /// <summary>
        /// Each counterclockwise pass of the approach is counted.
        /// This implements the assignment rule that CCW pieces must cross twice before entering home.
        /// </summary>
        public void RecordApproachPass()
        {
            if (MovementDirection == Direction.CounterClockwise)
                ApproachPassCount++;
        }

        public bool IsInStandardPath() => State == PieceState.StandardPath;
        public bool IsInHomeStraight() => State == PieceState.HomeStraight;
        public bool IsAtHome() => State == PieceState.Home;
        public bool IsAtBase() => State == PieceState.Base;
        public bool CanMove() => State != PieceState.Home && Effect.BriefingRounds == 0;
        public bool NeedsCaptureForHome() => Captures < 1;
    }

    /// <summary>
    /// Represents the game board and mystery cell lifecycle.
    /// Handles ring position movement, special location lookup, and mystery cell spawning.
    /// </summary>
    public class Board : IBoard
    {
        public int? MysteryCellPosition { get; private set; }
        public int MysteryCellRoundsRemaining { get; private set; }
        private int? _lastMysteryPosition;
        private int _roundsWithPiecesOnPath;
        private bool _pathTrackingStarted;
        private readonly IRandomSource _random;

        public Board(IRandomSource random) => _random = random;

        public int CalculateNewPosition(int currentPos, int movement, Direction direction)
        {
            int mult = direction == Direction.Clockwise ? 1 : -1;
            return (currentPos + movement * mult + GameConstants.BoardSize) % GameConstants.BoardSize;
        }

        public bool IsApproachCell(int position, Color color) =>
            position == GameConstants.ApproachCells[color];

        public bool IsMysteryCell(int position) => MysteryCellPosition == position;

        public int GetSpecialPosition(SpecialLocation location, Color color) => location switch
        {
            SpecialLocation.Alpha => GameConstants.AlphaPosition,
            SpecialLocation.Beta => GameConstants.BetaPosition,
            SpecialLocation.Gamma => GameConstants.GammaPosition,
            SpecialLocation.X => GameConstants.XPositions[color],
            SpecialLocation.Approach => GameConstants.ApproachCells[color],
            _ => -1
        };

        public bool UpdateMysteryCell(Dictionary<Color, IPlayer> players, out int? spawnedAt)
        {
            spawnedAt = null;
            if (GameRules.CountStandardPathPieces(players) > 0)
            {
                // Start tracking time once any piece is on the ring.
                if (!_pathTrackingStarted) _pathTrackingStarted = true;
                _roundsWithPiecesOnPath++;
            }

            if (!_pathTrackingStarted) return false;

            if (!MysteryCellPosition.HasValue && _roundsWithPiecesOnPath >= 2)
            {
                spawnedAt = SpawnMysteryCell(players);
                return spawnedAt.HasValue;
            }

            if (MysteryCellPosition.HasValue)
            {
                MysteryCellRoundsRemaining--;
                if (MysteryCellRoundsRemaining <= 0)
                {
                    spawnedAt = SpawnMysteryCell(players);
                    return spawnedAt.HasValue;
                }
            }
            return false;
        }

        private int? SpawnMysteryCell(Dictionary<Color, IPlayer> players)
        {
            // Choose an empty ring cell that is not currently occupied and not the last mystery location.
            var empty = new List<int>();
            for (int pos = 0; pos < GameConstants.BoardSize; pos++)
            {
                if (pos == _lastMysteryPosition) continue;
                bool occupied = players.Values
                    .SelectMany(p => p.Pieces)
                    .Any(pi => pi.IsInStandardPath() && pi.Position == pos);
                if (!occupied) empty.Add(pos);
            }

            if (empty.Count == 0) return null;
            MysteryCellPosition = empty[_random.Next(0, empty.Count)];
            MysteryCellRoundsRemaining = 4;
            _lastMysteryPosition = MysteryCellPosition;
            return MysteryCellPosition;
        }

        public List<Piece> GetOwnBlock(int position, Color color, Dictionary<Color, IPlayer> players)
        {
            var pieces = GameRules.GetPiecesAtPosition(position, color, players);
            return pieces.Count >= 2 ? pieces : new List<Piece>();
        }
    }

    public class Dice
    {
        private readonly IRandomSource _random;
        public Dice(IRandomSource random) => _random = random;
        public int Roll() => _random.Next(1, 7);
    }

    public class CoinToss
    {
        private readonly IRandomSource _random;
        public CoinToss(IRandomSource random) => _random = random;
        public Direction Toss() => _random.Next(0, 2) == 0 ? Direction.Clockwise : Direction.CounterClockwise;
    }

    /// <summary>
    /// Handles landing on mystery cells and applies teleportation effects to pieces.
    /// </summary>
    public class MysteryCellSystem
    {
        private readonly IBoard _board;
        private readonly IGameLogger _logger;
        private readonly IRandomSource _random;

        public MysteryCellSystem(IBoard board, IGameLogger logger, IRandomSource random)
        {
            _board = board;
            _logger = logger;
            _random = random;
        }

        public bool HandleLanding(Piece piece, Color playerColor)
        {
            if (!_board.IsMysteryCell(piece.Position)) return false;

            // Randomly select one of the six teleport destinations when landing on a mystery cell.
            var locations = Enum.GetValues<SpecialLocation>().Cast<SpecialLocation>().ToList();
            var dest = locations[_random.Next(0, locations.Count)];
            string destName = dest == SpecialLocation.X ? "X"
                : dest == SpecialLocation.Approach ? "Approach"
                : dest.ToString();

            _logger.LogMysteryCellLanding(playerColor, destName);

            if (dest == SpecialLocation.Base)
            {
                piece.ResetToBase();
                _logger.LogMysteryCellTeleport(playerColor, piece.Name, TeleportLocation.Base);
                return false;
            }

            int newPos = _board.GetSpecialPosition(dest, playerColor);
            piece.Position = newPos;
            piece.State = PieceState.StandardPath;
            _logger.LogMysteryCellTeleport(playerColor, piece.Name, GameLogger.GetTeleportLocation(dest));
            ApplyTeleportEffect(piece, playerColor, dest);
            return false;
        }

        private void ApplyTeleportEffect(Piece piece, Color playerColor, SpecialLocation dest)
        {
            switch (dest)
            {
                case SpecialLocation.Alpha:
                    if (_random.Next(0, 2) == 0)
                    {
                        piece.Effect.EnergizedRounds = 4;
                        _logger.LogEnergizedEffect(playerColor, piece.Name);
                    }
                    else
                    {
                        piece.Effect.SickRounds = 4;
                        _logger.LogSickEffect(playerColor, piece.Name);
                    }
                    break;
                case SpecialLocation.Beta:
                    piece.Effect.BriefingRounds = 4;
                    _logger.LogBriefingEffect(playerColor, piece.Name);
                    break;
                case SpecialLocation.Gamma:
                    if (piece.MovementDirection == Direction.Clockwise)
                    {
                        piece.MovementDirection = Direction.CounterClockwise;
                        _logger.LogDirectionChange(playerColor, piece.Name);
                    }
                    else
                    {
                        piece.Position = GameConstants.BetaPosition;
                        piece.Effect.BriefingRounds = 4;
                        _logger.LogGammaToBeta(playerColor, piece.Name);
                        _logger.LogBriefingEffect(playerColor, piece.Name);
                    }
                    break;
            }
        }

        public bool HandleBriefingRoll(Piece piece, Color playerColor, int diceValue)
        {
            if (piece.Effect.BriefingRounds <= 0) return false;
            if (diceValue == 3)
            {
                piece.Effect.ConsecutiveThrees++;
                if (piece.Effect.ConsecutiveThrees >= 3)
                {
                    piece.ResetToBase();
                    _logger.LogBriefingEscape(playerColor, piece.Name);
                    return true;
                }
            }
            else
            {
                piece.Effect.ConsecutiveThrees = 0;
            }
            return false;
        }
    }

    /// <summary>
    /// Base player class used by all four LUDO-T AI strategies.
    /// Encapsulates shared piece state and effect handling.
    /// 
    /// Pattern: Template Method / Strategy
    /// Each concrete player chooses moves with its own AI strategy,
    /// while shared behavior is defined here.
    /// </summary>
    public abstract class Player : IPlayer
    {
        // Template Method pattern: shared player lifecycle and behaviour are defined here,
        // while each concrete player subclass implements its own ChooseMove strategy.
        public Color Color { get; }
        public List<Piece> Pieces { get; }
        public int FinishPlace { get; set; }

        protected Player(Color color)
        {
            Color = color;
            Pieces = Enumerable.Range(1, 4).Select(i => new Piece(color, i)).ToList();
        }

        public List<Piece> GetPiecesOnBoard() =>
            Pieces.Where(p => p.IsInStandardPath() || p.IsInHomeStraight()).ToList();

        public List<Piece> GetPiecesAtBase() => Pieces.Where(p => p.IsAtBase()).ToList();
        public List<Piece> GetPiecesAtHome() => Pieces.Where(p => p.IsAtHome()).ToList();
        public int StandardPathCount() => Pieces.Count(p => p.IsInStandardPath());
        public bool HasWon() => GetPiecesAtHome().Count == 4;
        public bool IsEliminated() => HasWon();

        public void UpdateEffectsEndOfRound()
        {
            // Decrement duration on all piece effects at end of each round.
            foreach (var piece in Pieces)
            {
                if (piece.Effect.EnergizedRounds > 0) piece.Effect.EnergizedRounds--;
                if (piece.Effect.SickRounds > 0) piece.Effect.SickRounds--;
                if (piece.Effect.BriefingRounds > 0) piece.Effect.BriefingRounds--;
                if (piece.Effect.BriefingRounds == 0) piece.Effect.ConsecutiveThrees = 0;
            }
        }

        public abstract (Piece piece, bool fromBase)? ChooseMove(int diceValue, IBoard board, Dictionary<Color, IPlayer> players);
    }

    /// <summary>
    /// Red player strategy: prioritises captures, avoids unnecessary base entry if path capture is possible, and avoids creating blocks.
    /// </summary>
    public class RedPlayer : Player
    {
        public RedPlayer() : base(Color.Red) { }

        public override (Piece piece, bool fromBase)? ChooseMove(int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            var captures = FindCaptures(diceValue, board, players);
            if (captures.Count > 0)
            {
                var best = captures.OrderBy(c => GameRules.DistanceCapturedToHome(c.victim)).First();
                return (best.attacker, false);
            }

            if (diceValue == 6 && StandardPathCount() == 0)
            {
                if (!MovementEngine.CanCaptureWithRollOnPath(this, 6, board, players))
                {
                    var basePieces = GetPiecesAtBase();
                    if (basePieces.Any())
                        return (basePieces.First(), true);
                }
            }

            var pathPieces = Pieces.Where(p => p.IsInStandardPath() && p.CanMove()).ToList();
            if (pathPieces.Count > 0)
            {
                var pathMoves = pathPieces
                    .Select(p => (p, sim: MovementEngine.Simulate(this, p, diceValue, board, players)))
                    .Where(x => x.sim.CanMove)
                    .ToList();
                if (pathMoves.Count > 0)
                {
                    var withoutBlock = pathMoves.Where(x => !x.sim.CreatesBlock).ToList();
                    return ((withoutBlock.Count > 0 ? withoutBlock : pathMoves)[0].p, false);
                }
            }

            var homeMoves = Pieces
                .Where(p => p.IsInHomeStraight() && p.CanMove())
                .Select(p => (p, sim: MovementEngine.Simulate(this, p, diceValue, board, players)))
                .Where(x => x.sim.CanMove)
                .OrderByDescending(x => x.p.HomeStraightPosition)
                .ToList();
            if (homeMoves.Any()) return (homeMoves[0].p, false);

            return null;
        }

        private List<(Piece attacker, Piece victim)> FindCaptures(int dice, IBoard board, Dictionary<Color, IPlayer> players)
        {
            var list = new List<(Piece, Piece)>();
            foreach (var piece in Pieces.Where(p => (p.IsInStandardPath() || p.IsInHomeStraight()) && p.CanMove()))
            {
                var sim = MovementEngine.Simulate(this, piece, dice, board, players);
                if (sim.CanMove && sim.CaptureTarget != null)
                    list.Add((piece, sim.CaptureTarget));
            }
            return list;
        }
    }

    /// <summary>
    /// Green player strategy: prefers home-straight progress, avoids creating blocks with new base entries, and breaks own blockades when necessary.
    /// </summary>
    public class GreenPlayer : Player
    {
        public GreenPlayer() : base(Color.Green) { }

        public override (Piece piece, bool fromBase)? ChooseMove(int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            if (diceValue == 6)
            {
                var basePieces = GetPiecesAtBase();
                if (basePieces.Any() && !WouldCreateBlockWithSix(board, players))
                    return (basePieces.First(), true);
            }

            var homeStraightMoves = Pieces
                .Where(p => p.IsInHomeStraight() && p.CanMove())
                .Select(p => (p, sim: MovementEngine.Simulate(this, p, diceValue, board, players)))
                .Where(x => x.sim.CanMove)
                .OrderByDescending(x => x.p.HomeStraightPosition)
                .ToList();
            if (homeStraightMoves.Any())
                return (homeStraightMoves.First().p, false);

            var nonBlockPieces = Pieces.Where(p => p.IsInStandardPath() && board.GetOwnBlock(p.Position, Color, players).Count < 2).ToList();
            var towardHome = nonBlockPieces
                .Select(p => (p, sim: MovementEngine.Simulate(this, p, diceValue, board, players)))
                .Where(x => x.sim.CanMove)
                .OrderBy(x => GameRules.DistanceFromHome(x.p))
                .ToList();
            if (towardHome.Any()) return (towardHome.First().p, false);

            var blockMoves = Pieces.Where(p => p.IsInStandardPath())
                .Where(p => board.GetOwnBlock(p.Position, Color, players).Count >= 2)
                .Select(p => (p, sim: MovementEngine.Simulate(this, p, diceValue, board, players)))
                .Where(x => x.sim.CanMove).ToList();
            if (blockMoves.Any())
                return (blockMoves.First().p, false);

            return TryBreakBlock(diceValue, board, players);
        }

        private bool WouldCreateBlockWithSix(IBoard board, Dictionary<Color, IPlayer> players)
        {
            foreach (var p in Pieces.Where(x => x.IsInStandardPath()))
            {
                var sim = MovementEngine.Simulate(this, p, 6, board, players);
                if (sim.CanMove && sim.CreatesBlock) return true;
            }
            return false;
        }

        private (Piece, bool)? TryBreakBlock(int dice, IBoard board, Dictionary<Color, IPlayer> players)
        {
            var blockGroups = Pieces.Where(p => p.IsInStandardPath())
                .GroupBy(p => p.Position).Where(g => g.Count() >= 2).ToList();

            bool othersCanMove = Pieces
                .Where(p => p.IsInStandardPath() && !blockGroups.Any(g => g.Key == p.Position))
                .Any(p => MovementEngine.Simulate(this, p, dice, board, players).CanMove);
            if (othersCanMove) return null;

            foreach (var grp in blockGroups)
            {
                foreach (var piece in grp)
                {
                    var alone = MovementEngine.Simulate(this, piece, dice, board, players);
                    if (alone.CanMove) return (piece, false);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Yellow player strategy: enters pieces from base on sixes and prioritises captures for pieces that need a capture to enter home.
    /// </summary>
    public class YellowPlayer : Player
    {
        public YellowPlayer() : base(Color.Yellow) { }

        public override (Piece piece, bool fromBase)? ChooseMove(int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            if (diceValue == 6)
            {
                var basePieces = GetPiecesAtBase();
                if (basePieces.Any()) return (basePieces.First(), true);
            }

            var needCapture = Pieces.Where(p => p.NeedsCaptureForHome() && (p.IsInStandardPath() || p.IsInHomeStraight()))
                .ToList();
            foreach (var piece in needCapture)
            {
                var sim = MovementEngine.Simulate(this, piece, diceValue, board, players);
                if (sim.CanMove && sim.CaptureTarget != null) return (piece, false);
            }

            var moves = Pieces.Where(p => !p.IsAtBase() && !p.IsAtHome())
                .Select(p => (p, sim: MovementEngine.Simulate(this, p, diceValue, board, players)))
                .Where(x => x.sim.CanMove)
                .OrderBy(x => GameRules.DistanceFromHome(x.p))
                .ToList();
            if (moves.Any()) return (moves.First().p, false);
            return null;
        }
    }

    /// <summary>
    /// Blue player strategy: uses cyclic piece ordering and prefers avoiding mystery landings when moving counterclockwise.
    /// </summary>
    public class BluePlayer : Player
    {
        private int _nextPieceNumber = 1;

        public BluePlayer() : base(Color.Blue) { }

        public override (Piece piece, bool fromBase)? ChooseMove(int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            var ordered = GetCyclicPieces();

            if (diceValue == 6)
            {
                foreach (var piece in ordered)
                {
                    if (piece.IsAtBase())
                        return (piece, true);
                }
            }

            foreach (var piece in ordered)
            {
                if (piece.IsAtBase() || piece.IsAtHome()) continue;
                if (!MovementEngine.Simulate(this, piece, diceValue, board, players).CanMove) continue;
                if (!piece.IsInStandardPath()) return (piece, false);

                int movement = GameRules.GetModifiedMovement(piece, diceValue);
                int target = board.CalculateNewPosition(piece.Position, movement, piece.MovementDirection);
                if (piece.MovementDirection == Direction.CounterClockwise && board.IsMysteryCell(target))
                    return (piece, false);
            }

            foreach (var piece in ordered)
            {
                if (piece.IsAtBase() || piece.IsAtHome()) continue;
                if (!MovementEngine.Simulate(this, piece, diceValue, board, players).CanMove) continue;
                if (!piece.IsInStandardPath()) return (piece, false);

                int movement = GameRules.GetModifiedMovement(piece, diceValue);
                int target = board.CalculateNewPosition(piece.Position, movement, piece.MovementDirection);
                if (piece.MovementDirection == Direction.Clockwise && board.IsMysteryCell(target))
                    continue;
                return (piece, false);
            }

            return null;
        }

        private List<Piece> GetCyclicPieces()
        {
            var ordered = new List<Piece>();
            for (int attempt = 0; attempt < 4; attempt++)
            {
                ordered.Add(Pieces.First(p => p.Number == _nextPieceNumber));
                _nextPieceNumber = _nextPieceNumber % 4 + 1;
            }
            return ordered;
        }
    }
}
