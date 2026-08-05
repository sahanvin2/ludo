using System.Collections.Generic;
using System.Linq;

namespace LudoGame
{
    public sealed class MoveOutcome
    {
        public MoveResult Result { get; init; } = MoveResult.NoMove;
        public bool CanExecute => Result is MoveResult.Moved or MoveResult.CaptureBonus;
        public bool NeedsAlternative { get; init; }
        public bool CreatesBlock { get; init; }
        public Piece? CaptureTarget { get; init; }
        public MoveSimulation ToSimulation() => new()
        {
            CanMove = CanExecute,
            CaptureTarget = CaptureTarget,
            CreatesBlock = CreatesBlock,
            GrantsBonus = Result == MoveResult.CaptureBonus,
            IsBlockMove = Result == MoveResult.Moved
        };
    }

    /// <summary>
    /// Single source of truth for move evaluation (AI) and execution (game).
    /// Facade pattern: exposes simple Evaluate/Simulate/Apply operations while hiding detailed movement, block, capture, and home-entry rules.
    /// This supports the assignment requirement that AI decisions and actual execution share the same rule engine.
    /// </summary>
    public static class MovementEngine
    {
        /// <summary>
        /// Checks whether any piece can capture an opponent by moving along the standard path with the given roll.
        /// </summary>
        public static bool CanCaptureWithRollOnPath(IPlayer player, int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            return player.Pieces
                .Where(p => p.IsInStandardPath() && p.CanMove())
                .Any(p => Evaluate(player, p, diceValue, board, players).CaptureTarget != null);
        }

        /// <summary>
        /// Returns a simulated move result used by the AI to decide which piece should move.
        /// This does not mutate game state and only reports whether a piece can move.
        /// </summary>
        public static MoveSimulation Simulate(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            if (piece.IsAtBase())
            {
                // If the piece is at base, only a roll of 6 can enter the track.
                return new MoveSimulation
                {
                    CanMove = diceValue == 6,
                    IsFromBase = diceValue == 6,
                    Piece = piece,
                    DiceValue = diceValue
                };
            }

            var outcome = Evaluate(player, piece, diceValue, board, players);
            var sim = outcome.ToSimulation();
            sim.Piece = piece;
            sim.DiceValue = diceValue;
            sim.IsHomeStraight = piece.IsInHomeStraight() && sim.CanMove;
            return sim;
        }

        /// <summary>
        /// Evaluates a move and returns the expected outcome without changing the game board.
        /// It handles home straight bounds, blocks, captures, approach entry, and blocked opponents.
        /// </summary>
        public static MoveOutcome Evaluate(IPlayer player, Piece piece, int diceValue, IBoard board, Dictionary<Color, IPlayer> players)
        {
            // Home straight moves are only valid if the target step is within the finish range.
            if (piece.IsInHomeStraight())
            {
                int targetStep = piece.HomeStraightPosition + diceValue;
                if (targetStep > GameConstants.HomeStraightLength)
                    return new MoveOutcome { Result = MoveResult.NoMove };
                return new MoveOutcome { Result = MoveResult.Moved };
            }

            if (!piece.CanMove()) return new MoveOutcome();

            int movement = GameRules.GetModifiedMovement(piece, diceValue);
            if (movement <= 0) return new MoveOutcome();

            var block = board.GetOwnBlock(piece.Position, player.Color, players);
            if (block.Count >= 2 && block.Contains(piece))
            {
                // Block move: split movement among block pieces and move in the block's direction.
                int divided = diceValue / block.Count;
                if (divided <= 0) return new MoveOutcome();
                var dir = GameRules.GetBlockMovementDirection(block);
                int blockTarget = board.CalculateNewPosition(piece.Position, divided, dir);
                var oppBlock = GameRules.GetOpponentBlockAt(blockTarget, player.Color, players);
                if (oppBlock.Count >= 2 && oppBlock.Count == block.Count)
                    return new MoveOutcome { Result = MoveResult.CaptureBonus };
                if (GameRules.IsOpponentBlock(blockTarget, player.Color, players)) return new MoveOutcome();
                return new MoveOutcome { Result = MoveResult.Moved };
            }

            var path = GameRules.GetCellsAlongPath(piece.Position, movement, piece.MovementDirection);
            int approach = GameConstants.ApproachCells[player.Color];
            int approachIdx = path.IndexOf(approach);

            if (approachIdx >= 0)
            {
                // The rolled move crosses the player's approach cell.
                // Check home entry rules: capture requirement and CCW two-pass approach.
                if (GameRules.CanCompleteApproachEntry(piece, movement, approachIdx, players, countingThisCross: true))
                    return new MoveOutcome { Result = MoveResult.Moved };

                // Cross approach but cannot enter home — continue on ring to path.Last()
            }

            int target = path.Last();

            if (GameRules.IsOpponentBlock(target, player.Color, players))
            {
                // If the landing cell is an opponent blockade, use T-6 partial movement rules.
                var partial = GameRules.TryPartialMoveBeforeBlock(piece.Position, movement, piece.MovementDirection, player.Color, players);
                if (partial.HasValue && partial.Value != piece.Position)
                    return new MoveOutcome { Result = MoveResult.Moved };
                return new MoveOutcome { Result = MoveResult.NoMove, NeedsAlternative = true };
            }

            var cap = GameRules.FindCapturableAt(target, player.Color, players);
            if (cap != null)
                return new MoveOutcome { Result = MoveResult.CaptureBonus, CaptureTarget = cap };

            var ownAt = GameRules.GetPiecesAtPosition(target, player.Color, players).Where(p => p != piece).ToList();
            if (ownAt.Count >= 2) return new MoveOutcome();
            if (ownAt.Count == 1)
                return new MoveOutcome { Result = MoveResult.Moved, CreatesBlock = true };

            if (board.IsApproachCell(target, player.Color))
                return new MoveOutcome { Result = MoveResult.Moved };

            return new MoveOutcome { Result = MoveResult.Moved };
        }

        /// <summary>
        /// Applies the chosen move to the game state and logs the result.
        /// This method mutates piece positions, captures opponents, and handles special landing rules.
        /// </summary>
        public static MoveResult Apply(
            IPlayer player,
            Piece piece,
            int diceValue,
            IBoard board,
            Dictionary<Color, IPlayer> players,
            MysteryCellSystem? mystery,
            IGameLogger logger)
        {
            // Apply the move chosen by the selected player and log the resulting game events.
            if (piece.IsInHomeStraight())
                return ApplyHomeStraight(player, piece, diceValue, logger);

            int oldPos = piece.Position;
            int movement = GameRules.GetModifiedMovement(piece, diceValue);
            if (movement <= 0) return MoveResult.NoMove;

            var block = board.GetOwnBlock(piece.Position, player.Color, players);
            if (block.Count >= 2 && block.Contains(piece))
            {
                int divided = diceValue / block.Count;
                if (divided <= 0) return MoveResult.NoMove;
                return ApplyBlockMove(player, block, divided, diceValue, oldPos, board, players, logger);
            }

            var path = GameRules.GetCellsAlongPath(piece.Position, movement, piece.MovementDirection);
            int approach = GameConstants.ApproachCells[player.Color];
            int approachIdx = path.IndexOf(approach);

            if (approachIdx >= 0)
            {
                piece.RecordApproachPass();

                // A roll that crosses approach may try to enter home path.
                if (GameRules.CanEnterHomeStraight(piece, players) &&
                    GameRules.CanCompleteApproachEntry(piece, movement, approachIdx, players))
                {
                    int stepsInHome = GameRules.StepsIntoHomeAfterApproach(movement, approachIdx);
                    if (!GameRules.ApplyHomeStraightSteps(piece, stepsInHome))
                        return MoveResult.NoMove;
                    if (piece.IsAtHome())
                        logger.LogMoveEnterHomeStraightDetailed(player.Color, piece.Name, oldPos, GameConstants.HomePathMinPosition, diceValue, movement, stepsInHome, piece.MovementDirection);
                    else
                        logger.LogMoveEnterHomeStraightDetailed(player.Color, piece.Name, oldPos, piece.HomeStraightPosition, diceValue, movement, stepsInHome, piece.MovementDirection);
                    return MoveResult.Moved;
                }

                return ApplyStandardLanding(player, piece, diceValue, oldPos, path.Last(), movement, board, players, mystery, logger, approachPassRecorded: true);
            }

            return ApplyStandardLanding(player, piece, diceValue, oldPos, path.Last(), movement, board, players, mystery, logger);
        }

        /// <summary>
        /// Applies a normal path move, including block handling, capture resolution, and mystery cell landing.
        /// </summary>
        private static MoveResult ApplyStandardLanding(
            IPlayer player, Piece piece, int diceValue, int oldPos, int target, int movement,
            IBoard board, Dictionary<Color, IPlayer> players, MysteryCellSystem? mystery, IGameLogger logger,
            bool approachPassRecorded = false)
        {
                if (GameRules.IsOpponentBlock(target, player.Color, players))
                {
                    // Blocked by an opponent blockade; attempt a T-6 partial move before the blockade.
                    var partial = GameRules.TryPartialMoveBeforeBlock(piece.Position, movement, piece.MovementDirection, player.Color, players);
                    var blocker = GetBlockerName(target, player.Color, players);
                    logger.LogBlockedMove(player.Color, piece.Name, oldPos, target, blocker.color, blocker.name);
                    if (partial.HasValue)
                    {
                        piece.Position = partial.Value;
                        logger.LogPartialMove(player.Color, partial.Value);
                        return MoveResult.Moved;
                    }
                    return MoveResult.NoMove;
                }

            var cap = GameRules.FindCapturableAt(target, player.Color, players);
            if (cap != null)
            {
                // Single opponent piece is captured; reset it to base and award bonus.
                piece.Position = target;
                cap.ResetToBase();
                piece.Captures++;
                logger.LogCapture(player.Color, piece.Name, target, cap.Color, cap.Name);
                logger.LogPieceStatus(player.Color, player.GetPiecesOnBoard().Count, player.GetPiecesAtBase().Count);
                return MoveResult.CaptureBonus;
            }

            var ownAt = GameRules.GetPiecesAtPosition(target, player.Color, players).Where(p => p != piece).ToList();
            if (ownAt.Count >= 2) return MoveResult.NoMove;

            if (board.IsApproachCell(target, player.Color) && !approachPassRecorded)
                piece.RecordApproachPass();

            if (board.IsApproachCell(target, player.Color) && GameRules.CanEnterHomeStraight(piece, players))
            {
                // Landed on approach exactly, and home entry is allowed.
                GameRules.ApplyHomeStraightSteps(piece, 0);
                logger.LogMoveEnterHomeStraightDetailed(player.Color, piece.Name, oldPos, GameConstants.HomePathMinPosition, diceValue, movement, 0, piece.MovementDirection);
                return MoveResult.Moved;
            }

            piece.Position = target;
            logger.LogMoveDetailed(player.Color, piece.Name, oldPos, target, diceValue, movement, piece.MovementDirection);
            mystery?.HandleLanding(piece, player.Color);
            return MoveResult.Moved;
        }

        /// <summary>
        /// Applies block movement when multiple friendly pieces occupy the same square.
        /// This handles the split movement, block capture conditions, and shared direction.
        /// </summary>
        private static MoveResult ApplyBlockMove(
            IPlayer player, List<Piece> block, int divided, int diceValue, int oldPos,
            IBoard board, Dictionary<Color, IPlayer> players, IGameLogger logger)
        {
            // Block movement uses the farthest-from-home direction for mixed-direction blocks.
            var dir = GameRules.GetBlockMovementDirection(block);
            int target = board.CalculateNewPosition(oldPos, divided, dir);
            var oppBlock = GameRules.GetOpponentBlockAt(target, player.Color, players);

            if (oppBlock.Count >= 2 && oppBlock.Count == block.Count)
            {
                // Block capture: two equal-sized opposing blocks capture each other.
                string attacker = block[0].Name;
                foreach (var d in oppBlock)
                {
                    d.ResetToBase();
                    logger.LogCapture(player.Color, attacker, target, d.Color, d.Name);
                }
                foreach (var a in block)
                {
                    a.Captures++;
                    a.Position = target;
                }
                logger.LogPieceStatus(player.Color, player.GetPiecesOnBoard().Count, player.GetPiecesAtBase().Count);
                return MoveResult.CaptureBonus;
            }

            if (GameRules.IsOpponentBlock(target, player.Color, players))
                return MoveResult.NoMove;

            foreach (var p in block)
            {
                p.Position = target;
                p.MovementDirection = dir;
            }
            int movedUnits = divided;
            logger.LogMoveDetailed(player.Color, block[0].Name, oldPos.ToString(), target.ToString(), diceValue, movedUnits, dir);
            return MoveResult.Moved;
        }

        /// <summary>
        /// Applies movement along the home straight and logs the step change or final home arrival.
        /// </summary>
        private static MoveResult ApplyHomeStraight(IPlayer player, Piece piece, int diceValue, IGameLogger logger)
        {
            int targetStep = piece.HomeStraightPosition + diceValue;
            if (targetStep > GameConstants.HomeStraightLength)
                return MoveResult.NoMove;

            int old = piece.HomeStraightPosition;
            if (targetStep == GameConstants.HomeStraightLength)
            {
                piece.State = PieceState.Home;
                piece.HomeStraightPosition = -1;
                logger.LogMoveOnHomeStraightDetailed(player.Color, piece.Name, old, GameConstants.HomeStraightLength, diceValue, diceValue, piece.MovementDirection);
                return MoveResult.Moved;
            }

            piece.HomeStraightPosition = targetStep;
            logger.LogMoveOnHomeStraightDetailed(player.Color, piece.Name, old, targetStep, diceValue, diceValue, piece.MovementDirection);
            return MoveResult.Moved;
        }

        private static (Color color, string name) GetBlockerName(int pos, Color moving, Dictionary<Color, IPlayer> players)
        {
            foreach (var kvp in players)
            {
                if (kvp.Key == moving) continue;
                var at = GameRules.GetPiecesAtPosition(pos, kvp.Key, players);
                if (at.Count >= 1) return (kvp.Key, at[0].Name);
            }
            return (moving, "?");
        }
    }
}
