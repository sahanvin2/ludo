using System;
using System.Collections.Generic;

namespace LudoT_Simulation;

public class MysteryCellManager
{
    public int? CurrentLocation { get; private set; }
    public int RoundsUntilDisappear { get; private set; }
    private int roundsSinceStart = 0;
    private Random rand = new Random();

    public void Update(int currentRound, List<Piece> allPieces)
    {
        if (CurrentLocation == null)
        {
            if (currentRound >= 2 && roundsSinceStart == 0) // initial spawn after 2 rounds
            {
                Spawn(allPieces);
            }
            else if (roundsSinceStart > 0) // reappears
            {
                Spawn(allPieces);
            }
        }
        else
        {
            RoundsUntilDisappear--;
            if (RoundsUntilDisappear <= 0)
            {
                CurrentLocation = null;
                // Will spawn next round
            }
        }
        
        roundsSinceStart++;
    }

    private void Spawn(List<Piece> allPieces)
    {
        List<int> occupied = new List<int>();
        foreach (var p in allPieces)
        {
            if (p.State == PieceState.StandardPath)
            {
                occupied.Add(p.Position);
            }
        }

        List<int> available = new List<int>();
        for (int i = 0; i < Board.TotalStandardCells; i++)
        {
            if (!occupied.Contains(i))
            {
                available.Add(i);
            }
        }

        if (available.Count > 0)
        {
            CurrentLocation = available[rand.Next(available.Count)];
            RoundsUntilDisappear = 4;
            Console.WriteLine($"Mystery Cells : A mystery cell has spawned in location {CurrentLocation} and will be at this location for the next four rounds.");
        }
    }

    public void TeleportPiece(Piece p)
    {
        int choice = rand.Next(1, 7);
        string locName = "";
        switch (choice)
        {
            case 1: locName = "Alpha"; break;
            case 2: locName = "Beta"; break;
            case 3: locName = "Gamma"; break;
            case 4: locName = "Base"; break;
            case 5: locName = "X"; break;
            case 6: locName = "Approach cell"; break;
        }

        Console.WriteLine($"[{p.Color}] piece {p.Id} lands on Mystery Cell and is teleported to {locName}.");

        switch (choice)
        {
            case 1:
                p.Position = 9;
                if (rand.Next(2) == 0)
                {
                    p.Status = StatusEffect.Energized;
                    p.EffectRoundsRemaining = 4;
                    Console.WriteLine($"[{p.Color}] piece {p.Id} feels energized, and movement speed doubles.");
                }
                else
                {
                    p.Status = StatusEffect.Sick;
                    p.EffectRoundsRemaining = 4;
                    Console.WriteLine($"[{p.Color}] piece {p.Id} feels sick, and movement speed halves.");
                }
                break;
            case 2:
                p.Position = 27;
                p.Status = StatusEffect.Briefing;
                p.EffectRoundsRemaining = 4;
                p.ConsecutiveThreesRolled = 0;
                Console.WriteLine($"[{p.Color}] piece {p.Id} attends briefing and cannot move for four rounds.");
                break;
            case 3:
                p.Position = 46;
                if (p.Direction == Direction.Clockwise)
                {
                    p.Direction = Direction.CounterClockwise;
                    Console.WriteLine($"The [{p.Color}] piece {p.Id}, which was moving clockwise, has changed to moving counterclockwise.");
                }
                else
                {
                    Console.WriteLine($"The [{p.Color}] piece {p.Id} is moving in a counterclockwise direction. Teleporting to Beta from Gamma.");
                    p.Position = 27;
                    p.Status = StatusEffect.Briefing;
                    p.EffectRoundsRemaining = 4;
                    p.ConsecutiveThreesRolled = 0;
                    Console.WriteLine($"[{p.Color}] piece {p.Id} attends briefing and cannot move for four rounds.");
                }
                break;
            case 4:
                p.Reset();
                break;
            case 5:
                p.Position = Board.GetStartCellX(p.Color);
                break;
            case 6:
                p.Position = Board.GetApproachCell(p.Color);
                break;
        }
    }
}
