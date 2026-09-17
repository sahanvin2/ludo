using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using LudoGame;
using GameColor = LudoGame.Color;

namespace LudoGame
{
    public sealed class SnapshotGameLogger : IGameLogger
    {
        private readonly object _sync = new();
        private readonly GameSnapshot _snapshot = new();
        private GameColor? _activeStatusColor;

        public event Action<GameSnapshot>? SnapshotChanged;

        public int DelayMilliseconds { get; set; } = 18;

        public void Reset()
        {
            lock (_sync)
            {
                _snapshot.Players.Clear();
                _snapshot.LogLines.Clear();
                _snapshot.MysteryCellPosition = null;
                _snapshot.MysteryCellRoundsRemaining = 0;
                _snapshot.StatusText = "Ready to start.";
                _snapshot.CurrentSection = string.Empty;
                _snapshot.CurrentStatusColor = null;
                _snapshot.IsRunning = false;
                _snapshot.IsComplete = false;
                _activeStatusColor = null;
            }

            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void MarkRunning(string message = "Starting simulation...")
        {
            lock (_sync)
            {
                _snapshot.IsRunning = true;
                _snapshot.IsComplete = false;
                _snapshot.StatusText = message;
                AddLogLocked(message);
            }

            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void MarkComplete(string message = "Simulation complete.")
        {
            lock (_sync)
            {
                _snapshot.IsRunning = false;
                _snapshot.IsComplete = true;
                _snapshot.StatusText = message;
                AddLogLocked(message);
            }

            SnapshotChanged?.Invoke(GetSnapshot());
        }

        public void LogInitialPlayer(GameColor color, string n1, string n2, string n3, string n4)
            => Write(color, $"{color} team pieces: {n1}, {n2}, {n3}, {n4}", () =>
            {
                var player = _snapshot.GetOrCreatePlayer(color);
                player.GetOrCreatePiece(n1);
                player.GetOrCreatePiece(n2);
                player.GetOrCreatePiece(n3);
                player.GetOrCreatePiece(n4);
                player.RecalculateCounts();
            });

        public void LogOpeningRoll(GameColor color, int value)
            => Write(color, $"Opening roll for {color}: {value}");

        public void LogFirstPlayer(GameColor color)
            => Write(color, $"{color} starts the game");

        public void LogRoundOrder(string order)
            => Write(null, $"Round order: {order}");

        public void LogDiceRoll(GameColor color, int value)
            => Write(color, $"{color} rolled {value}");

        public void LogStartingPointMove(GameColor color, string pieceName)
            => Write(color, $"{pieceName} entered the board", () =>
            {
                var piece = EnsurePiece(color, pieceName);
                piece.MoveToRing(GameConstants.XPositions[color]);
            });

        public void LogPieceStatus(GameColor color, int boardCount, int baseCount, int homeCount = 0)
            => Write(color, $"{color}: board {boardCount}, base {baseCount}, home {homeCount}", () =>
            {
                var player = _snapshot.GetOrCreatePlayer(color);
                player.BoardCount = boardCount;
                player.BaseCount = baseCount;
                player.HomeCount = homeCount;
            });

        public void LogMove(GameColor color, string pieceName, string fromLocation, string toLocation, int units, Direction direction)
            => Write(color, $"{pieceName} moved from {fromLocation} to {toLocation}", () => ApplyLocation(color, pieceName, toLocation));

        public void LogMove(GameColor color, string pieceName, int startPos, int endPos, int units, Direction direction)
            => LogMove(color, pieceName, startPos.ToString(CultureInfo.InvariantCulture), endPos.ToString(CultureInfo.InvariantCulture), units, direction);

        public void LogMoveDetailed(GameColor color, string pieceName, string fromLocation, string toLocation, int diceValue, int movedUnits, Direction direction)
            => Write(color, $"{pieceName} moved {movedUnits} on roll {diceValue}", () => ApplyLocation(color, pieceName, toLocation));

        public void LogMoveDetailed(GameColor color, string pieceName, int startPos, int endPos, int diceValue, int movedUnits, Direction direction)
            => LogMoveDetailed(color, pieceName, startPos.ToString(CultureInfo.InvariantCulture), endPos.ToString(CultureInfo.InvariantCulture), diceValue, movedUnits, direction);

        public void LogMoveEnterHomeStraight(GameColor color, string pieceName, int ringPosition, int homePathPosition, int totalUnits, int stepsInHome, Direction direction)
            => LogMoveEnterHomeStraightDetailed(color, pieceName, ringPosition, homePathPosition, totalUnits, totalUnits, stepsInHome, direction);

        public void LogMoveEnterHomeStraightDetailed(GameColor color, string pieceName, int ringPosition, int homePathPosition, int diceValue, int movedUnits, int stepsInHome, Direction direction)
            => Write(color, $"{pieceName} entered home straight", () =>
            {
                var piece = EnsurePiece(color, pieceName);
                if (stepsInHome >= GameConstants.HomeStraightLength)
                    piece.MoveToHome();
                else
                    piece.MoveToHomeStraight(homePathPosition);
            });

        public void LogMoveOnHomeStraight(GameColor color, string pieceName, int fromStep, int toStep, int units, Direction direction)
            => LogMoveOnHomeStraightDetailed(color, pieceName, fromStep, toStep, units, units, direction);

        public void LogMoveOnHomeStraightDetailed(GameColor color, string pieceName, int fromStep, int toStep, int diceValue, int movedUnits, Direction direction)
            => Write(color, $"{pieceName} advanced on the home straight", () =>
            {
                var piece = EnsurePiece(color, pieceName);
                if (toStep >= GameConstants.HomeStraightLength)
                    piece.MoveToHome();
                else
                    piece.MoveToHomeStraight(toStep);
            });

        public void LogBlockedMove(GameColor color, string pieceName, int startPos, int endPos, GameColor blockingColor, string blockingPieceName)
            => Write(color, $"{pieceName} was blocked by {blockingColor} {blockingPieceName}");

        public void LogNoAlternativeMove(GameColor color)
            => Write(color, $"{color} had no alternative move");

        public void LogPartialMove(GameColor color, int partialPosition)
            => Write(color, $"Partial move stopped at {partialPosition}");

        public void LogCapture(GameColor color, string pieceName, int position, GameColor capturedColor, string capturedPieceName)
            => Write(color, $"{pieceName} captured {capturedColor} {capturedPieceName} at {position}", () =>
            {
                var mover = EnsurePiece(color, pieceName);
                mover.MoveToRing(position);
                var captured = EnsurePiece(capturedColor, capturedPieceName);
                captured.MoveToBase();
            });

        public void LogMysterySpawn(int location, int rounds)
            => Write(null, $"Mystery cell spawned at {location} for {rounds} rounds", () =>
            {
                _snapshot.MysteryCellPosition = location;
                _snapshot.MysteryCellRoundsRemaining = rounds;
            });

        public void LogPlacement(GameColor color, int place)
            => Write(color, $"{color} finished in {place}{OrdinalSuffix(place)} place", () =>
            {
                var player = _snapshot.GetOrCreatePlayer(color);
                player.FinishPlace = place;
            });

        public void LogWinner(GameColor color, int place)
            => LogPlacement(color, place);

        public void LogFinalStandings(IReadOnlyList<(GameColor color, int place)> standings)
            => Write(null, $"Final standings: {string.Join(", ", standings.OrderBy(s => s.place).Select(s => $"{s.color}={s.place}"))}", () =>
            {
                _snapshot.IsComplete = true;
            });

        public void LogRoundStatusHeader(GameColor color)
            => Write(color, $"Round status for {color}", () =>
            {
                _snapshot.CurrentStatusColor = color;
                _snapshot.CurrentSection = $"Board snapshot for {color}";
            });

        public void LogPieceLocation(string pieceName, string location)
            => Write(_snapshot.CurrentStatusColor, $"{pieceName} at {location}", () =>
            {
                if (_snapshot.CurrentStatusColor is not GameColor color)
                    return;
                ApplyLocation(color, pieceName, location);
            });

        public void LogMysteryCellStatus(int location, int roundsRemaining)
            => Write(null, $"Mystery cell at {location}, {roundsRemaining} rounds remaining", () =>
            {
                _snapshot.MysteryCellPosition = location;
                _snapshot.MysteryCellRoundsRemaining = roundsRemaining;
            });

        public void LogMysteryCellLanding(GameColor color, string locationName)
            => Write(color, $"{color} landed on mystery cell and teleported to {locationName}");

        public void LogMysteryCellTeleport(GameColor color, string pieceName, TeleportLocation location)
            => Write(color, $"{pieceName} teleported to {location}", () => ApplyTeleport(color, pieceName, location));

        public void LogEnergizedEffect(GameColor color, string pieceName)
            => Write(color, $"{pieceName} became energized");

        public void LogSickEffect(GameColor color, string pieceName)
            => Write(color, $"{pieceName} became sick");

        public void LogBriefingEffect(GameColor color, string pieceName)
            => Write(color, $"{pieceName} is in briefing");

        public void LogBriefingEscape(GameColor color, string pieceName)
            => Write(color, $"{pieceName} escaped briefing and returned to base", () =>
            {
                EnsurePiece(color, pieceName).MoveToBase();
            });

        public void LogDirectionChange(GameColor color, string pieceName)
            => Write(color, $"{pieceName} switched direction");

        public void LogGammaToBeta(GameColor color, string pieceName)
            => Write(color, $"{pieceName} moved from Gamma to Beta", () =>
            {
                EnsurePiece(color, pieceName).MoveToRing(GameConstants.BetaPosition);
            });

        public void LogConsecutiveSixesIgnored(GameColor color)
            => Write(color, $"Three consecutive sixes were ignored");

        public GameSnapshot GetSnapshot()
        {
            lock (_sync)
                return _snapshot.Clone();
        }

        private void ApplyTeleport(GameColor color, string pieceName, TeleportLocation location)
        {
            var piece = EnsurePiece(color, pieceName);
            switch (location)
            {
                case TeleportLocation.Base:
                    piece.MoveToBase();
                    break;
                case TeleportLocation.X:
                    piece.MoveToRing(GameConstants.XPositions[color]);
                    break;
                case TeleportLocation.Approach:
                    piece.MoveToRing(GameConstants.ApproachCells[color]);
                    break;
                case TeleportLocation.Alpha:
                    piece.MoveToRing(GameConstants.AlphaPosition);
                    break;
                case TeleportLocation.Beta:
                    piece.MoveToRing(GameConstants.BetaPosition);
                    break;
                case TeleportLocation.Gamma:
                    piece.MoveToRing(GameConstants.GammaPosition);
                    break;
            }
        }

        private PieceViewModel EnsurePiece(GameColor color, string pieceName)
        {
            var player = _snapshot.GetOrCreatePlayer(color);
            return player.GetOrCreatePiece(pieceName);
        }

        private void ApplyLocation(GameColor color, string pieceName, string location)
        {
            var piece = EnsurePiece(color, pieceName);
            if (string.Equals(location, "Base", StringComparison.OrdinalIgnoreCase))
            {
                piece.MoveToBase();
                return;
            }

            if (string.Equals(location, "Home", StringComparison.OrdinalIgnoreCase))
            {
                piece.MoveToHome();
                return;
            }

            if (location.IndexOf("homepath[", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int start = location.IndexOf('[');
                int end = location.IndexOf(']');
                if (start >= 0 && end > start && int.TryParse(location.Substring(start + 1, end - start - 1), out int step))
                {
                    if (step >= GameConstants.HomeStraightLength)
                        piece.MoveToHome();
                    else
                        piece.MoveToHomeStraight(step);
                }
                return;
            }

            if (int.TryParse(location, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ringPosition))
            {
                piece.MoveToRing(ringPosition);
            }
        }

        private void Write(GameColor? color, string text, Action? mutation = null)
        {
            lock (_sync)
            {
                mutation?.Invoke();
                _snapshot.StatusText = text;
                AddLogLocked(text);
                foreach (var player in _snapshot.Players.Values)
                    player.RecalculateCounts();

                _activeStatusColor = color ?? _activeStatusColor;
            }

            SnapshotChanged?.Invoke(GetSnapshot());
            if (DelayMilliseconds > 0)
                Thread.Sleep(DelayMilliseconds);
        }

        private void AddLogLocked(string text)
        {
            _snapshot.LogLines.Add(text);
            if (_snapshot.LogLines.Count > 500)
                _snapshot.LogLines.RemoveRange(0, _snapshot.LogLines.Count - 500);
        }

        private static string OrdinalSuffix(int place) => place switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}