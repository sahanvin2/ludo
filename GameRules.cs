using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoGame
{
    public enum MoveResult
    {
        NoMove,
        Moved,
        CaptureBonus,
        IgnoredTurn
    }

    /// <summary>
    /// Pure rule helper methods for LUDO-T movement logic.
    /// Contains path generation, block detection, home entry rules, partial movement, and capture validation.
    /// These methods support assignment-level rules such as T-4 block movement, T-7 capture-before-home rules, and T-6 partial block moves.
    /// </summary>
    public static class GameRules
    {
        /// <summary>
        /// Adjusts raw dice movement for energized or sick pieces.
        /// This implements the mystery cell effects rule: energized doubles movement; sick halves it.
        /// </summary>
        public static int GetModifiedMovement(Piece piece, int diceValue)
        {
            if (diceValue <= 0) return 0;
            int movement = diceValue;
            if (piece.Effect.EnergizedRounds > 0)
                movement *= 2;
            else if (piece.Effect.SickRounds > 0)
                movement = Math.Max(1, movement / 2);
            return movement;
        }

        /// <summary>
        /// Advances a board position by one cell in the given direction, wrapping around the 52-cell ring.
        /// </summary>
        public static int StepForward(int pos, Direction direction)
        {
            int mult = direction == Direction.Clockwise ? 1 : -1;
            return (pos + mult + GameConstants.BoardSize) % GameConstants.BoardSize;
        }

        /// <summary>
        /// Builds the sequence of ring positions traversed by a piece moving a number of steps.
        /// </summary>
        public static List<int> GetCellsAlongPath(int start, int steps, Direction direction)
        {
            var cells = new List<int>();
            int pos = start;
            for (int i = 0; i < steps; i++)
            {
                pos = StepForward(pos, direction);
                cells.Add(pos);
            }
            return cells;
        }

        /// <summary>Ring cell after full move if the piece stayed on the track (ignores home entry).</summary>
        public static int RingEndPosition(int start, int steps, Direction direction) =>
            GetCellsAlongPath(start, steps, direction).Last();

        /// <summary>When the path crosses the approach, returns index on path and steps into home straight after the approach cell.</summary>
        public static bool TryGetApproachCrossing(Color color, int start, int movement, Direction direction, out int approachIdx, out int stepsInHome)
        {
            approachIdx = -1;
            stepsInHome = 0;
            if (movement <= 0) return false;
            var path = GetCellsAlongPath(start, movement, direction);
            int approach = GameConstants.ApproachCells[color];
            approachIdx = path.IndexOf(approach);
            if (approachIdx < 0) return false;
            stepsInHome = StepsIntoHomeAfterApproach(movement, approachIdx);
            return true;
        }

        public static int DistanceFromHome(Piece piece)
        {
            if (piece.IsInHomeStraight())
                return GameConstants.HomeStraightLength - piece.HomeStraightPosition;
            if (!piece.IsInStandardPath()) return int.MaxValue / 2;
            int approach = GameConstants.ApproachCells[piece.Color];
            if (piece.MovementDirection == Direction.Clockwise)
                return (approach - piece.Position + GameConstants.BoardSize) % GameConstants.BoardSize;
            return (piece.Position - approach + GameConstants.BoardSize) % GameConstants.BoardSize;
        }

        /// <summary>T-4: block moves in direction of piece farthest from home when directions differ.</summary>
        /// <remarks>Matches the assignment rule for opposite-direction blocks on the ring.</remarks>
        public static Direction GetBlockMovementDirection(List<Piece> block)
        {
            // T-4: If the block contains pieces going both directions,
            // move in the direction of the piece farthest from home.
            var dirs = block.Select(p => p.MovementDirection).Distinct().ToList();
            if (dirs.Count == 1) return dirs[0];
            return block.OrderByDescending(DistanceFromHome).First().MovementDirection;
        }

        public static bool HasOppositeDirectionsInBlock(List<Piece> block) =>
            block.Select(p => p.MovementDirection).Distinct().Count() > 1;

        /// <summary>Steps on the home path after the approach cell (0 = entrance only; movement ended on approach).</summary>
        public static int StepsIntoHomeAfterApproach(int movement, int approachPathIndex) =>
            movement - (approachPathIndex + 1);

        public static bool IsValidHomePathPosition(int position) =>
            position >= GameConstants.HomePathMinPosition && position <= GameConstants.HomePathMaxPosition;

        public static string FormatPieceLocation(Piece piece, Color color)
        {
            if (piece.IsAtBase()) return "Base";
            if (piece.IsAtHome()) return "Home";
            if (piece.IsInHomeStraight())
            {
                return $"{color.ToLowerString()} homepath[{piece.HomeStraightPosition}]";
            }
            return piece.Position.ToString();
        }

        /// <summary>
        /// Apply home-straight transition after crossing/landing on approach.
        /// stepsInHome: 0 = entrance tile; 1..4 = home-path positions; 5 = finished (past cell 4).
        /// </summary>
        public static bool ApplyHomeStraightSteps(Piece piece, int stepsInHome)
        {
            piece.State = PieceState.HomeStraight;
            piece.Position = -1;
            if (stepsInHome == 0)
            {
                piece.HomeStraightPosition = GameConstants.HomePathMinPosition;
                return true;
            }
            if (stepsInHome == GameConstants.HomeStraightLength)
            {
                piece.State = PieceState.Home;
                piece.HomeStraightPosition = -1;
                return true;
            }
            if (stepsInHome > 0 && stepsInHome < GameConstants.HomeStraightLength)
            {
                piece.HomeStraightPosition = stepsInHome;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a piece can complete approach entry into the home straight.
        /// </summary>
        public static bool CanCompleteApproachEntry(Piece piece, int movement, int approachIdx, Dictionary<Color, IPlayer>? players = null, bool countingThisCross = false)
        {
            if (approachIdx < 0) return false;
            int stepsInHome = StepsIntoHomeAfterApproach(movement, approachIdx);
            bool canEnter = countingThisCross
                ? CanEnterHomeStraightAfterCross(piece, players)
                : CanEnterHomeStraight(piece, players);
            if (!canEnter) return false;
            return stepsInHome >= 0 && stepsInHome <= GameConstants.HomeStraightLength;
        }

        public static bool AnyOpponentOnStandardPath(Color color, Dictionary<Color, IPlayer> players) =>
            players.Where(kvp => kvp.Key != color)
                .SelectMany(kvp => kvp.Value.Pieces)
                .Any(p => p.IsInStandardPath());

        public static int DistanceCapturedToHome(Piece captured)
        {
            return DistanceFromHome(captured);
        }

        public static List<Piece> GetPiecesAtPosition(int position, Color color, Dictionary<Color, IPlayer> players, bool standardOnly = true)
        {
            return players[color].Pieces
                .Where(p => (!standardOnly || p.IsInStandardPath()) && p.Position == position)
                .ToList();
        }

        public static List<Piece> GetOpponentPiecesAt(int position, Color movingColor, Dictionary<Color, IPlayer> players)
        {
            var list = new List<Piece>();
            foreach (var kvp in players)
            {
                if (kvp.Key == movingColor) continue;
                list.AddRange(kvp.Value.Pieces.Where(p => p.IsInStandardPath() && p.Position == position));
            }
            return list;
        }

        public static bool IsOpponentBlock(int position, Color movingColor, Dictionary<Color, IPlayer> players)
        {
            foreach (var kvp in players)
            {
                if (kvp.Key == movingColor) continue;
                if (GetPiecesAtPosition(position, kvp.Key, players).Count >= 2)
                    return true;
            }
            return false;
        }

        public static bool IsOwnBlock(int position, Color color, Dictionary<Color, IPlayer> players)
        {
            return GetPiecesAtPosition(position, color, players).Count >= 2;
        }

        public static bool WouldCreateBlock(int position, Piece moving, Dictionary<Color, IPlayer> players)
        {
            var atTarget = GetPiecesAtPosition(position, moving.Color, players)
                .Where(p => p != moving).ToList();
            return atTarget.Count >= 1;
        }

        /// <summary>Rule 9 / T-7: capture required before home straight while opponents remain on the ring.</summary>
        /// <remarks>Fails home entry until the piece has captured at least one opponent while others remain on the standard path.</remarks>
        public static bool CanEnterHomeStraight(Piece piece, Dictionary<Color, IPlayer>? players = null)
        {
            if (piece.Captures < 1 && players != null && AnyOpponentOnStandardPath(piece.Color, players))
                return false;
            if (piece.MovementDirection == Direction.Clockwise) return true;
            return piece.ApproachPassCount >= 2;
        }

        /// <summary>
        /// When a counterclockwise piece crosses the approach on the current roll, count that crossing as one approach pass.
        /// </summary>
        /// <remarks>Implements the CCW two-pass approach requirement from the assignment.</remarks>
        public static bool CanEnterHomeStraightAfterCross(Piece piece, Dictionary<Color, IPlayer>? players = null)
        {
            if (piece.Captures < 1 && players != null && AnyOpponentOnStandardPath(piece.Color, players))
                return false;
            if (piece.MovementDirection == Direction.Clockwise) return true;
            return piece.ApproachPassCount + 1 >= 2;
        }

        public static bool PathCrossesApproach(int from, int steps, Direction dir, Color color)
        {
            int approach = GameConstants.ApproachCells[color];
            var path = GetCellsAlongPath(from, steps, dir);
            return path.Contains(approach);
        }

        /// <summary>
        /// Calculates the last safe ring cell before an opponent block is reached, if any.
        /// Used to implement partial moves when a full dice roll would land on an opponent blockade.
        /// </summary>
        public static int? TryPartialMoveBeforeBlock(int from, int steps, Direction dir, Color color, Dictionary<Color, IPlayer> players)
        {
            int? lastSafe = null;
            int pos = from;
            for (int i = 0; i < steps; i++)
            {
                pos = StepForward(pos, dir);
                if (IsOpponentBlock(pos, color, players))
                    return lastSafe; // T-6 partial move rule: stop before opponent blockade.
                lastSafe = pos;
            }
            return lastSafe;
        }

        /// <summary>T-6: move up to <paramref name="steps"/> ring cells; stops before an opponent block.</summary>
        /// <remarks>Used to evaluate partial movement safely when a full move would land on an opponent blockade.</remarks>
        public static int? TryMoveCumulative(int from, int steps, Direction dir, Color color, Dictionary<Color, IPlayer> players)
        {
            int? lastSafe = null;
            int pos = from;
            for (int i = 0; i < steps; i++)
            {
                int next = StepForward(pos, dir);
                if (IsOpponentBlock(next, color, players))
                    return lastSafe;
                pos = next;
                lastSafe = pos;
            }
            return lastSafe;
        }

        public static Piece? FindCapturableAt(int position, Color attackerColor, Dictionary<Color, IPlayer> players)
        {
            var opponents = GetOpponentPiecesAt(position, attackerColor, players);
            if (opponents.Count == 0) return null;
            if (opponents.Count >= 2) return null; // Cannot capture a block.
            return opponents[0];
        }

        public static bool HasAnyMovablePiece(IPlayer player, int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            if (diceValue == 6 && player.GetPiecesAtBase().Any()) return true;
            foreach (var piece in player.Pieces)
            {
                if (piece.IsInHomeStraight())
                {
                    if (piece.HomeStraightPosition + diceValue <= GameConstants.HomeStraightLength)
                        return true;
                    continue;
                }
                if (piece.IsInStandardPath() && piece.CanMove() && MovementEngine.Simulate(player, piece, diceValue, board, players).CanMove)
                    return true;
            }
            return false;
        }

        /// <summary>Delegates to MovementEngine — single rule source for AI and execution.</summary>
        public static MoveSimulation SimulateMove(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players) =>
            MovementEngine.Simulate(player, piece, diceValue, board, players);

        public static List<Piece> GetOpponentBlockAt(int position, Color attackerColor, Dictionary<Color, IPlayer> players)
        {
            foreach (var kvp in players)
            {
                if (kvp.Key == attackerColor) continue;
                var block = GetPiecesAtPosition(position, kvp.Key, players);
                if (block.Count >= 2) return block;
            }
            return new List<Piece>();
        }

        public static int CountStandardPathPieces(Dictionary<Color, IPlayer> players) =>
            players.Values.SelectMany(p => p.Pieces).Count(x => x.IsInStandardPath());
    }

    public class MoveSimulation
    {
        public Piece Piece { get; set; } = null!;
        public int DiceValue { get; set; }
        public bool CanMove { get; set; }
        public bool IsFromBase { get; set; }
        public bool IsHomeStraight { get; set; }
        public bool IsPartial { get; set; }
        public bool CreatesBlock { get; set; }
        public bool EntersHomeStraight { get; set; }
        public bool LandsOnMystery { get; set; }
        public bool IsBlockCapture { get; set; }
        public bool IsBlockMove { get; set; }
        public bool GrantsBonus { get; set; }
        public int EndPosition { get; set; }
        public int BlockedTarget { get; set; }
        public Piece? CaptureTarget { get; set; }
        public List<Piece> BlockCaptureTargets { get; set; } = new();
    }
}
