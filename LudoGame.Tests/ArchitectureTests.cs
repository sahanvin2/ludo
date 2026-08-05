using System;
using System.Collections.Generic;
using System.Linq;
using LudoGame;
using Xunit;

namespace LudoGame.Tests
{
    public class ArchitectureTests
    {
        [Fact]
        public void PlayerFactory_creates_all_four_players_as_IPlayer()
        {
            var players = PlayerFactory.CreatePlayers();

            Assert.Equal(4, players.Count);
            foreach (Color color in Enum.GetValues<Color>())
            {
                Assert.True(players.TryGetValue(color, out var player));
                Assert.NotNull(player);
                Assert.IsAssignableFrom<IPlayer>(player);
                Assert.Equal(color, player.Color);
            }
        }

        [Fact]
        public void GameManager_accepts_injected_random_and_logger()
        {
            var random = new TestRandom(new[] { 6, 6, 6, 6, 6, 6, 0, 1, 2, 3 });
            var logger = new TestLogger();
            var manager = new GameManager(random, logger, new NullSectionPresenter(), Array.Empty<IGameObserver>());

            Assert.NotNull(manager);
            Assert.Empty(logger.Messages);
        }
    }

    internal sealed class TestRandom : IRandomSource
    {
        private readonly Queue<int> _values;

        public TestRandom(IEnumerable<int> values)
        {
            _values = new Queue<int>(values);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (_values.Count == 0)
                return minInclusive;

            int next = _values.Dequeue();
            if (next < minInclusive) return minInclusive;
            if (next >= maxExclusive) return maxExclusive - 1;
            return next;
        }
    }

    internal sealed class TestLogger : IGameLogger
    {
        public List<string> Messages { get; } = new();

        public void LogInitialPlayer(Color color, string n1, string n2, string n3, string n4) => Messages.Add($"Initial {color}");
        public void LogOpeningRoll(Color color, int value) => Messages.Add($"Roll {color}={value}");
        public void LogFirstPlayer(Color color) => Messages.Add($"First {color}");
        public void LogRoundOrder(string order) => Messages.Add($"Order {order}");
        public void LogDiceRoll(Color color, int value) => Messages.Add($"Dice {color}={value}");
        public void LogStartingPointMove(Color color, string pieceName) => Messages.Add($"Start {color} {pieceName}");
        public void LogPieceStatus(Color color, int boardCount, int baseCount, int homeCount = 0) =>
            Messages.Add($"Status {color} {boardCount}/{baseCount}/{homeCount}");
        public void LogMove(Color color, string pieceName, string fromLocation, string toLocation, int units, Direction direction) =>
            Messages.Add($"Move {color} {pieceName} {fromLocation}->{toLocation}");
        public void LogMove(Color color, string pieceName, int startPos, int endPos, int units, Direction direction) =>
            Messages.Add($"Move {color} {pieceName} {startPos}->{endPos}");
        public void LogMoveDetailed(Color color, string pieceName, string fromLocation, string toLocation, int diceValue, int movedUnits, Direction direction) =>
            Messages.Add($"MoveDetailed {color} {pieceName} {fromLocation}->{toLocation} roll={diceValue} moved={movedUnits}");
        public void LogMoveDetailed(Color color, string pieceName, int startPos, int endPos, int diceValue, int movedUnits, Direction direction) =>
            Messages.Add($"MoveDetailed {color} {pieceName} {startPos}->{endPos} roll={diceValue} moved={movedUnits}");
        public void LogMoveEnterHomeStraight(Color color, string pieceName, int ringPosition, int homePathPosition, int totalUnits, int stepsInHome, Direction direction) =>
            Messages.Add($"EnterHome {color} {pieceName} ring={ringPosition} home={homePathPosition} stepsInHome={stepsInHome}");
        public void LogMoveEnterHomeStraightDetailed(Color color, string pieceName, int ringPosition, int homePathPosition, int diceValue, int movedUnits, int stepsInHome, Direction direction) =>
            Messages.Add($"EnterHomeDetailed {color} {pieceName} ring={ringPosition} home={homePathPosition} roll={diceValue} moved={movedUnits} stepsInHome={stepsInHome}");
        public void LogMoveOnHomeStraight(Color color, string pieceName, int fromStep, int toStep, int units, Direction direction) =>
            Messages.Add($"HomeStraight {color} {pieceName} {fromStep}->{toStep}");
        public void LogMoveOnHomeStraightDetailed(Color color, string pieceName, int fromStep, int toStep, int diceValue, int movedUnits, Direction direction) =>
            Messages.Add($"HomeStraightDetailed {color} {pieceName} {fromStep}->{toStep} roll={diceValue} moved={movedUnits}");
        public void LogBlockedMove(Color color, string pieceName, int startPos, int endPos, Color blockingColor, string blockingPieceName) => Messages.Add($"Blocked {color} {pieceName}");
        public void LogNoAlternativeMove(Color color) => Messages.Add($"NoAlt {color}");
        public void LogPartialMove(Color color, int partialPosition) => Messages.Add($"Partial {partialPosition}");
        public void LogCapture(Color color, string pieceName, int position, Color capturedColor, string capturedPieceName) => Messages.Add($"Capture {color} {pieceName}");
        public void LogMysterySpawn(int location, int rounds) => Messages.Add($"MysterySpawn {location}");
        public void LogPlacement(Color color, int place) => Messages.Add($"Place {color}={place}");
        public void LogWinner(Color color, int place) => Messages.Add($"Winner {color}");
        public void LogFinalStandings(IReadOnlyList<(Color color, int place)> standings) =>
            Messages.Add($"Standings {standings.Count}");
        public void LogRoundStatusHeader(Color color) => Messages.Add($"Header {color}");
        public void LogPieceLocation(string pieceName, string location) => Messages.Add($"PieceLoc {pieceName}");
        public void LogMysteryCellStatus(int location, int roundsRemaining) => Messages.Add($"MysteryStatus {location}");
        public void LogMysteryCellLanding(Color color, string locationName) => Messages.Add($"MysteryLanding {color}");
        public void LogMysteryCellTeleport(Color color, string pieceName, TeleportLocation location) => Messages.Add($"Teleport {color}");
        public void LogEnergizedEffect(Color color, string pieceName) => Messages.Add($"Energized {color}");
        public void LogSickEffect(Color color, string pieceName) => Messages.Add($"Sick {color}");
        public void LogBriefingEffect(Color color, string pieceName) => Messages.Add($"Briefing {color}");
        public void LogBriefingEscape(Color color, string pieceName) => Messages.Add($"BriefingEscape {color}");
        public void LogDirectionChange(Color color, string pieceName) => Messages.Add($"Direction {color}");
        public void LogGammaToBeta(Color color, string pieceName) => Messages.Add($"GammaToBeta {color}");
        public void LogConsecutiveSixesIgnored(Color color) => Messages.Add($"SixesIgnored {color}");
    }
}
