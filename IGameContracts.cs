using System.Collections.Generic;

namespace LudoGame
{
    /// <summary>
    /// Random number provider used by the game to decouple dice rolls from the runtime engine.
    /// </summary>
    public interface IRandomSource
    {
        int Next(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// Represents the board and its special locations, including approach cells, mystery cells, and position arithmetic.
    /// Interface Segregation Principle: board consumers depend only on path and mystery-cell access, not game orchestration.
    /// </summary>
    public interface IBoard
    {
        int CalculateNewPosition(int currentPos, int movement, Direction direction);
        bool IsApproachCell(int position, Color color);
        bool IsMysteryCell(int position);
        int GetSpecialPosition(SpecialLocation location, Color color);
        int? MysteryCellPosition { get; }
        int MysteryCellRoundsRemaining { get; }
        bool UpdateMysteryCell(Dictionary<Color, IPlayer> players, out int? spawnedAt);
        List<Piece> GetOwnBlock(int position, Color color, Dictionary<Color, IPlayer> players);
    }

    /// <summary>
    /// Represents a player and its move strategy.
    /// Single Responsibility Principle: the player tracks pieces and selects moves, while move execution is handled by the engine.
    /// </summary>
    public interface IPlayer
    {
        Color Color { get; }
        List<Piece> Pieces { get; }
        int FinishPlace { get; set; }
        List<Piece> GetPiecesOnBoard();
        List<Piece> GetPiecesAtBase();
        List<Piece> GetPiecesAtHome();
        int StandardPathCount();
        bool HasWon();
        void UpdateEffectsEndOfRound();
        (Piece piece, bool fromBase)? ChooseMove(int diceValue, IBoard board, Dictionary<Color, IPlayer> players);
    }

    public interface IGameLogger
    {
        void LogInitialPlayer(Color color, string n1, string n2, string n3, string n4);
        void LogOpeningRoll(Color color, int value);
        void LogFirstPlayer(Color color);
        void LogRoundOrder(string order);
        void LogDiceRoll(Color color, int value);
        void LogStartingPointMove(Color color, string pieceName);
        void LogPieceStatus(Color color, int boardCount, int baseCount, int homeCount = 0);
        void LogMove(Color color, string pieceName, string fromLocation, string toLocation, int units, Direction direction);
        void LogMove(Color color, string pieceName, int startPos, int endPos, int units, Direction direction);
        void LogMoveDetailed(Color color, string pieceName, string fromLocation, string toLocation, int diceValue, int movedUnits, Direction direction);
        void LogMoveDetailed(Color color, string pieceName, int startPos, int endPos, int diceValue, int movedUnits, Direction direction);
        void LogMoveEnterHomeStraight(Color color, string pieceName, int ringPosition, int homePathPosition, int totalUnits, int stepsInHome, Direction direction);
        void LogMoveEnterHomeStraightDetailed(Color color, string pieceName, int ringPosition, int homePathPosition, int diceValue, int movedUnits, int stepsInHome, Direction direction);
        void LogMoveOnHomeStraight(Color color, string pieceName, int fromStep, int toStep, int units, Direction direction);
        void LogMoveOnHomeStraightDetailed(Color color, string pieceName, int fromStep, int toStep, int diceValue, int movedUnits, Direction direction);
        void LogBlockedMove(Color color, string pieceName, int startPos, int endPos, Color blockingColor, string blockingPieceName);
        void LogNoAlternativeMove(Color color);
        void LogPartialMove(Color color, int partialPosition);
        void LogCapture(Color color, string pieceName, int position, Color capturedColor, string capturedPieceName);
        void LogMysterySpawn(int location, int rounds);
        void LogPlacement(Color color, int place);
        void LogWinner(Color color, int place);
        void LogFinalStandings(IReadOnlyList<(Color color, int place)> standings);
        void LogRoundStatusHeader(Color color);
        void LogPieceLocation(string pieceName, string location);
        void LogMysteryCellStatus(int location, int roundsRemaining);
        void LogMysteryCellLanding(Color color, string locationName);
        void LogMysteryCellTeleport(Color color, string pieceName, TeleportLocation location);
        void LogEnergizedEffect(Color color, string pieceName);
        void LogSickEffect(Color color, string pieceName);
        void LogBriefingEffect(Color color, string pieceName);
        void LogBriefingEscape(Color color, string pieceName);
        void LogDirectionChange(Color color, string pieceName);
        void LogGammaToBeta(Color color, string pieceName);
        void LogConsecutiveSixesIgnored(Color color);
    }
}
