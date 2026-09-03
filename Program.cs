using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoT_Simulation;

public class Program
{
    static void Main(string[] args)
    {
        var rand = new Random();
        
        var red = new RedPlayer(Color.Red);
        var yellow = new YellowPlayer(Color.Yellow);
        var green = new GreenPlayer(Color.Green);
        var blue = new BluePlayer(Color.Blue);

        var players = new List<Player> { red, green, yellow, blue }; // CW order: Red -> Green -> Yellow -> Blue

        foreach (var p in players)
        {
            string colorName = p.Color.ToString().ToLower();
            string pieceNames = string.Join(", ", p.Pieces.Select(piece => piece.Id).Take(3)) + ", and " + p.Pieces.Last().Id;
            Console.WriteLine($"The {colorName} player has four (04) pieces named {pieceNames}.");
        }

        // Determine first player
        Dictionary<Player, int> initialRolls = new Dictionary<Player, int>();
        int maxRoll = 0;
        Player startingPlayer = null;

        // Simplified tie-breaking (first one to get max keeps it)
        foreach (var p in players)
        {
            int roll = rand.Next(1, 7);
            initialRolls[p] = roll;
            Console.WriteLine($"[{p.Color}] rolls {roll}");
            if (roll > maxRoll)
            {
                maxRoll = roll;
                startingPlayer = p;
            }
        }

        Console.WriteLine($"[{startingPlayer.Color}] player has the highest roll and will begin the game.");
        
        int startIndex = players.IndexOf(startingPlayer);
        var playOrder = new List<Player>();
        for (int i = 0; i < 4; i++)
        {
            playOrder.Add(players[(startIndex + i) % 4]);
        }
        
        Console.WriteLine($"The order of a single round is [{playOrder[0].Color}], [{playOrder[1].Color}], [{playOrder[2].Color}], and [{playOrder[3].Color}].");

        var engine = new GameEngine(players);
        int currentRound = 1;
        bool gameWon = false;
        Player winner = null;

        while (!gameWon && currentRound < 1000) // safety limit
        {
            foreach (var p in playOrder)
            {
                if (gameWon) break;
                
                int bonusRolls = 0;
                int consecutiveSixes = 0;
                
                do
                {
                    int roll = rand.Next(1, 7);
                    Console.WriteLine($"[{p.Color}] player rolled {roll}.");

                    if (roll == 6) consecutiveSixes++;
                    else consecutiveSixes = 0;

                    if (consecutiveSixes == 3)
                    {
                        // Ignore roll and end turn (Rule 4), unless Rule T-6 applies
                        // For simplicity, we just end the turn
                        break;
                    }
                    
                    // Track pre-move captures
                    int preCaptures = p.Pieces.Sum(piece => piece.CapturedPiecesCount);

                    bool moved = p.PlayTurn(roll, engine);
                    
                    if (!moved)
                    {
                        // Could not move
                        Console.WriteLine($"[{p.Color}] does not have other pieces in the board to move instead of the blocked piece. Ignoring the throw and moving on to the next player.");
                    }
                    else
                    {
                        // Did piece win?
                        if (p.PiecesInHome() == 4)
                        {
                            winner = p;
                            gameWon = true;
                            Console.WriteLine($"[{p.Color}] player wins!!!");
                            break;
                        }
                    }

                    // Bonus rolls
                    if (roll == 6) bonusRolls++;
                    int postCaptures = p.Pieces.Sum(piece => piece.CapturedPiecesCount);
                    if (postCaptures > preCaptures) bonusRolls++; // Rule T-2
                    
                    if (bonusRolls > 0)
                    {
                        bonusRolls--;
                    }
                    else
                    {
                        break;
                    }
                    
                } while (true);
            }

            if (!gameWon)
            {
                // End of round logging
                foreach (var p in playOrder)
                {
                    Console.WriteLine($"[{p.Color}] player now has {p.PiecesOnBoard()}/4 on pieces on the board and {p.PiecesInBase()}/4 pieces on the base.");
                    Console.WriteLine($"============================ Location of pieces [{p.Color}] ============================");
                    foreach (var piece in p.Pieces)
                    {
                        string loc = piece.State == PieceState.Base ? "Base" : piece.State == PieceState.Home ? "Home" : piece.Position.ToString();
                        Console.WriteLine($"Piece {piece.Id} -> {loc}.");
                    }
                }
                
                engine.MysteryCell.Update(currentRound, engine.AllPieces);
                if (engine.MysteryCell.CurrentLocation.HasValue)
                {
                    Console.WriteLine($"The mystery cell is at {engine.MysteryCell.CurrentLocation.Value} and will be at that location for the next {engine.MysteryCell.RoundsUntilDisappear} values.");
                }
            }

            currentRound++;
        }
    }
}
