using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoGame
{
    /// <summary>
    /// Teleport destinations used by the mystery cell system for logging.
    /// </summary>
    public enum TeleportLocation
    {
        Alpha,
        Beta,
        Gamma,
        Base,
        X,
        Approach
    }

    /// <summary>
    /// Contains all formatted console output for the LUDO-T simulation.
    /// This class keeps message text separate from game logic and supports detailed move reporting.
    /// </summary>
    public static class GameLogger
    {
        /// <summary>
        /// Logs the initial piece names assigned to each player.
        /// </summary>
        public static void LogInitialPlayer(Color color, string n1, string n2, string n3, string n4)
        {
            Console.WriteLine($"The {color.ToLowerString()} player has four (04) pieces named {n1}, {n2}, {n3}, and {n4}.");
        }

        public static void LogOpeningRoll(Color color, int value)
        {
            Console.WriteLine($"{color.ToLowerString()} rolls {value}");
        }

        public static void LogFirstPlayer(Color color)
        {
            Console.WriteLine($"{color.ToLowerString()} player has the highest roll and will begin the game.");
        }

        public static void LogRoundOrder(string order)
        {
            Console.WriteLine($"The order of a single round is {order}.");
        }

        public static void LogDiceRoll(Color color, int value)
        {
            Console.WriteLine($"{color.ToLowerString()} player rolled {value}.");
        }

        public static void LogStartingPointMove(Color color, string pieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} player moves piece {pieceName} to the starting point.");
        }

        public static void LogPieceStatus(Color color, int boardCount, int baseCount, int homeCount = 0)
        {
            if (homeCount > 0)
                Console.WriteLine($"{color.ToLowerString()} player now has {boardCount}/4 pieces on the board, {baseCount}/4 pieces on the base, and {homeCount}/4 pieces in home.");
            else
                Console.WriteLine($"{color.ToLowerString()} player now has {boardCount}/4 pieces on the board and {baseCount}/4 pieces on the base.");
        }

        public static string HomePathLocation(Color color, int step) =>
            $"{color.ToLowerString()}homepath[{step}]";

        public static void LogMove(Color color, string pieceName, string fromLocation, string toLocation, int units, Direction direction)
        {
            string dir = DirectionText(direction);
            Console.WriteLine($"{color.ToLowerString()} moves piece {pieceName} from location {fromLocation} to {toLocation} by {units} units in {dir} direction.");
        }

        public static void LogMoveDetailed(Color color, string pieceName, string fromLocation, string toLocation, int diceValue, int movedUnits, Direction direction)
        {
            string dir = DirectionText(direction);
            Console.WriteLine($"{color.ToLowerString()} moves piece {pieceName} from location {fromLocation} to {toLocation} (roll {diceValue}, moved {movedUnits}) in {dir} direction.");
        }

        public static void LogMove(Color color, string pieceName, int startPos, int endPos, int units, Direction direction) =>
            LogMove(color, pieceName, startPos.ToString(), endPos.ToString(), units, direction);

        /// <summary>
        /// Logs a piece transitioning from the ring into the home straight.
        /// </summary>
        public static void LogMoveEnterHomeStraight(
            Color color, string pieceName, int ringPosition, int homePathPosition,
            int totalUnits, int stepsInHome, Direction direction)
        {
            LogMove(color, pieceName, ringPosition.ToString(), HomePathLocation(color, homePathPosition), totalUnits, direction);
        }

        /// <summary>
        /// Logs a detailed move when the piece enters the home straight, including the original dice roll.
        /// </summary>
        public static void LogMoveEnterHomeStraightDetailed(
            Color color, string pieceName, int ringPosition, int homePathPosition,
            int diceValue, int movedUnits, int stepsInHome, Direction direction)
        {
            LogMoveDetailed(color, pieceName, ringPosition.ToString(), HomePathLocation(color, homePathPosition), diceValue, movedUnits, direction);
        }

        public static void LogMoveOnHomeStraight(Color color, string pieceName, int fromStep, int toStep, int units, Direction direction)
        {
            string from = HomePathLocation(color, fromStep);
            string to = toStep >= GameConstants.HomeStraightLength ? "Home" : HomePathLocation(color, toStep);
            LogMove(color, pieceName, from, to, units, direction);
        }

        public static void LogMoveOnHomeStraightDetailed(Color color, string pieceName, int fromStep, int toStep, int diceValue, int movedUnits, Direction direction)
        {
            string from = HomePathLocation(color, fromStep);
            string to = toStep >= GameConstants.HomeStraightLength ? "Home" : HomePathLocation(color, toStep);
            LogMoveDetailed(color, pieceName, from, to, diceValue, movedUnits, direction);
        }

        private static string DirectionText(Direction direction) =>
            direction == Direction.Clockwise ? "clockwise" : "counter-clockwise";

        public static void LogBlockedMove(Color color, string pieceName, int startPos, int endPos, Color blockingColor, string blockingPieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} is blocked from moving from {startPos} to {endPos} by {blockingColor.ToLowerString()} piece {blockingPieceName}.");
        }

        public static void LogNoAlternativeMove(Color color)
        {
            Console.WriteLine($"{color.ToLowerString()} does not have other pieces in the board to move instead of the blocked piece.");
            Console.WriteLine("Ignoring the throw and moving on to the next player.");
        }

        public static void LogPartialMove(Color color, int partialPosition)
        {
            Console.WriteLine($"{color.ToLowerString()} moved the piece to square {partialPosition}, which is the cell before the block.");
        }

        /// <summary>
        /// Logs the partial move when a player can only move up to the block instead of full dice value.
        /// </summary>
        public static void LogPartialMove(int partialPosition)
        {
            Console.WriteLine($"Moved the piece to square {partialPosition} which is the cell before the block.");
        }

        public static void LogCapture(Color color, string pieceName, int position, Color capturedColor, string capturedPieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} lands on square {position}, captures {capturedColor.ToLowerString()} piece {capturedPieceName}, and returns it to the base.");
        }

        public static void LogMysterySpawn(int location, int rounds)
        {
            string roundWord = rounds == 1 ? "round" : "rounds";
            Console.WriteLine($"A mystery cell has spawned in location {location} and will be at this location for the next {rounds} {roundWord}.");
        }

        public static void LogMysteryCellLanding(Color color, string locationName)
        {
            Console.WriteLine($"{color.ToLowerString()} player lands on a mystery cell and is teleported to {locationName}.");
        }

        public static void LogMysteryCellTeleport(Color color, string pieceName, TeleportLocation location)
        {
            string dest = location switch
            {
                TeleportLocation.X => "X",
                TeleportLocation.Approach => "Approach",
                _ => location.ToString()
            };
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} teleported to {dest}.");
        }

        public static void LogEnergizedEffect(Color color, string pieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} feels energized, and movement speed doubles.");
        }

        public static void LogSickEffect(Color color, string pieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} feels sick, and movement speed halves.");
        }

        public static void LogBriefingEffect(Color color, string pieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} attends briefing and cannot move for four rounds.");
        }

        public static void LogBriefingEscape(Color color, string pieceName)
        {
            Console.WriteLine($"{color.ToLowerString()} piece {pieceName} is movement-restricted and has rolled three consecutively. Teleporting piece {pieceName} to base.");
        }

        public static void LogDirectionChange(Color color, string pieceName)
        {
            Console.WriteLine($"The {color.ToLowerString()} piece {pieceName}, which was moving clockwise, has changed to moving counterclockwise.");
        }

        public static void LogGammaToBeta(Color color, string pieceName)
        {
            Console.WriteLine($"The {color.ToLowerString()} piece {pieceName} is moving in a counterclockwise direction. Teleporting to Beta from Gamma.");
        }

        public static void LogConsecutiveSixesIgnored(Color color)
        {
            // Rule 4: third consecutive six is ignored; turn passes with no extra message required.
        }

        public static void LogPlacement(Color color, int place)
        {
            if (place == 1)
                Console.WriteLine($"{color.ToLowerString()} player wins!!!");
            else
                Console.WriteLine($"{color.ToLowerString()} player finished in {OrdinalSuffix(place)} place.");
        }

        public static void LogWinner(Color color, int place) => LogPlacement(color, place);

        public static void LogFinalStandings(IReadOnlyList<(Color color, int place)> standings)
        {
            Console.WriteLine("============================");
            Console.WriteLine("Final standings");
            Console.WriteLine("============================");
            foreach (var (color, place) in standings.OrderBy(s => s.place))
            {
                if (place == 1)
                    Console.WriteLine($"1st: {color.ToLowerString()} player wins!!!");
                else
                    Console.WriteLine($"{OrdinalSuffix(place)}: {color.ToLowerString()} player");
            }
        }

        private static string OrdinalSuffix(int place) => place switch
        {
            2 => "2nd",
            3 => "3rd",
            4 => "4th",
            _ => $"{place}th"
        };

        public static void LogRoundStatusHeader(Color color)
        {
            Console.WriteLine("============================");
            Console.WriteLine($"Location of pieces {color.ToLowerString()}");
            Console.WriteLine("============================");
        }

        public static void LogPieceLocation(string pieceName, string location)
        {
            Console.WriteLine($"Piece {pieceName} -> {location}");
        }

        public static void LogMysteryCellStatus(int location, int roundsRemaining)
        {
            string roundWord = roundsRemaining == 1 ? "round" : "rounds";
            Console.WriteLine($"The mystery cell is at {location} and will be at that location for the next {roundsRemaining} {roundWord}.");
        }

        /// <summary>
        /// Maps a special board location to a teleport destination for logging output.
        /// </summary>
        public static TeleportLocation GetTeleportLocation(SpecialLocation location) => location switch
        {
            SpecialLocation.Alpha => TeleportLocation.Alpha,
            SpecialLocation.Beta => TeleportLocation.Beta,
            SpecialLocation.Gamma => TeleportLocation.Gamma,
            SpecialLocation.Base => TeleportLocation.Base,
            SpecialLocation.X => TeleportLocation.X,
            SpecialLocation.Approach => TeleportLocation.Approach,
            _ => TeleportLocation.Base
        };
    }
}
