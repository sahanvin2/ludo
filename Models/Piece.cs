using System;

namespace LudoT_Arena.Models;

public class Piece
{
    public string Id { get; }
    public Color Color { get; }
    public PieceState State { get; set; }
    public int Position { get; set; } // Base: -1, StandardPath: 0-51, HomeStraight: 0-4, Home: 5
    public Direction Direction { get; set; }
    public int CapturedPiecesCount { get; set; }
    public StatusEffect Status { get; set; }
    public int EffectRoundsRemaining { get; set; }
    
    // For teleportation to Beta (Rule T-13)
    public int ConsecutiveThreesRolled { get; set; }
    
    // Original direction tracking (Rule T-5)
    public Direction OriginalDirection { get; set; }

    public Piece(string id, Color color)
    {
        Id = id;
        Color = color;
        Reset();
    }

    public void Reset()
    {
        State = PieceState.Base;
        Position = -1;
        Direction = Direction.Clockwise;
        OriginalDirection = Direction.Clockwise;
        CapturedPiecesCount = 0;
        Status = StatusEffect.Normal;
        EffectRoundsRemaining = 0;
        ConsecutiveThreesRolled = 0;
    }
}
