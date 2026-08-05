namespace LudoGame
{
    /// <summary>
    /// Simple logger implementation that writes all game events to the console.
    /// This class delegates formatting to the shared <see cref="GameLogger"/> helper.
    /// </summary>
    public sealed class ConsoleGameLogger : IGameLogger
    {
        /// <summary>
        /// Writes all game log events to the console by delegating to <see cref="GameLogger"/>.
        /// </summary>
        public void LogInitialPlayer(Color color, string n1, string n2, string n3, string n4) => GameLogger.LogInitialPlayer(color, n1, n2, n3, n4);
        public void LogOpeningRoll(Color color, int value) => GameLogger.LogOpeningRoll(color, value);
        public void LogFirstPlayer(Color color) => GameLogger.LogFirstPlayer(color);
        public void LogRoundOrder(string order) => GameLogger.LogRoundOrder(order);
        public void LogDiceRoll(Color color, int value) => GameLogger.LogDiceRoll(color, value);
        public void LogStartingPointMove(Color color, string pieceName) => GameLogger.LogStartingPointMove(color, pieceName);
        public void LogPieceStatus(Color color, int boardCount, int baseCount, int homeCount = 0) =>
            GameLogger.LogPieceStatus(color, boardCount, baseCount, homeCount);
        public void LogMove(Color color, string pieceName, string fromLocation, string toLocation, int units, Direction direction) =>
            GameLogger.LogMove(color, pieceName, fromLocation, toLocation, units, direction);
        public void LogMove(Color color, string pieceName, int startPos, int endPos, int units, Direction direction) =>
            GameLogger.LogMove(color, pieceName, startPos, endPos, units, direction);
            public void LogMoveDetailed(Color color, string pieceName, string fromLocation, string toLocation, int diceValue, int movedUnits, Direction direction) =>
                GameLogger.LogMoveDetailed(color, pieceName, fromLocation, toLocation, diceValue, movedUnits, direction);
            public void LogMoveDetailed(Color color, string pieceName, int startPos, int endPos, int diceValue, int movedUnits, Direction direction) =>
                GameLogger.LogMoveDetailed(color, pieceName, startPos.ToString(), endPos.ToString(), diceValue, movedUnits, direction);
        public void LogMoveEnterHomeStraight(Color color, string pieceName, int ringPosition, int homePathPosition, int totalUnits, int stepsInHome, Direction direction) =>
            GameLogger.LogMoveEnterHomeStraight(color, pieceName, ringPosition, homePathPosition, totalUnits, stepsInHome, direction);
        public void LogMoveEnterHomeStraightDetailed(Color color, string pieceName, int ringPosition, int homePathPosition, int diceValue, int movedUnits, int stepsInHome, Direction direction) =>
            GameLogger.LogMoveEnterHomeStraightDetailed(color, pieceName, ringPosition, homePathPosition, diceValue, movedUnits, stepsInHome, direction);
        public void LogMoveOnHomeStraight(Color color, string pieceName, int fromStep, int toStep, int units, Direction direction) =>
            GameLogger.LogMoveOnHomeStraight(color, pieceName, fromStep, toStep, units, direction);
        public void LogMoveOnHomeStraightDetailed(Color color, string pieceName, int fromStep, int toStep, int diceValue, int movedUnits, Direction direction) =>
            GameLogger.LogMoveOnHomeStraightDetailed(color, pieceName, fromStep, toStep, diceValue, movedUnits, direction);
        public void LogBlockedMove(Color color, string pieceName, int startPos, int endPos, Color blockingColor, string blockingPieceName) =>
            GameLogger.LogBlockedMove(color, pieceName, startPos, endPos, blockingColor, blockingPieceName);
        public void LogNoAlternativeMove(Color color) => GameLogger.LogNoAlternativeMove(color);
        public void LogPartialMove(Color color, int partialPosition) => GameLogger.LogPartialMove(partialPosition);
        public void LogCapture(Color color, string pieceName, int position, Color capturedColor, string capturedPieceName) =>
            GameLogger.LogCapture(color, pieceName, position, capturedColor, capturedPieceName);
        public void LogMysterySpawn(int location, int rounds) => GameLogger.LogMysterySpawn(location, rounds);
        public void LogPlacement(Color color, int place) => GameLogger.LogPlacement(color, place);
        public void LogWinner(Color color, int place) => GameLogger.LogWinner(color, place);
        public void LogFinalStandings(IReadOnlyList<(Color color, int place)> standings) =>
            GameLogger.LogFinalStandings(standings);
        public void LogRoundStatusHeader(Color color) => GameLogger.LogRoundStatusHeader(color);
        public void LogPieceLocation(string pieceName, string location) => GameLogger.LogPieceLocation(pieceName, location);
        public void LogMysteryCellStatus(int location, int roundsRemaining) => GameLogger.LogMysteryCellStatus(location, roundsRemaining);
        public void LogMysteryCellLanding(Color color, string locationName) => GameLogger.LogMysteryCellLanding(color, locationName);
        public void LogMysteryCellTeleport(Color color, string pieceName, TeleportLocation location) =>
            GameLogger.LogMysteryCellTeleport(color, pieceName, location);
        public void LogEnergizedEffect(Color color, string pieceName) => GameLogger.LogEnergizedEffect(color, pieceName);
        public void LogSickEffect(Color color, string pieceName) => GameLogger.LogSickEffect(color, pieceName);
        public void LogBriefingEffect(Color color, string pieceName) => GameLogger.LogBriefingEffect(color, pieceName);
        public void LogBriefingEscape(Color color, string pieceName) => GameLogger.LogBriefingEscape(color, pieceName);
        public void LogDirectionChange(Color color, string pieceName) => GameLogger.LogDirectionChange(color, pieceName);
        public void LogGammaToBeta(Color color, string pieceName) => GameLogger.LogGammaToBeta(color, pieceName);
        public void LogConsecutiveSixesIgnored(Color color) => GameLogger.LogConsecutiveSixesIgnored(color);
    }
}
