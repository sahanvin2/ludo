using System.Collections.Generic;

namespace LudoT_Arena.Models;

public class Move
{
    public Piece Piece { get; set; } = null!;
    public bool IsBlockMove { get; set; }
    public List<Piece> BlockPieces { get; set; } = new List<Piece>();
    
    public int StartPosition { get; set; }
    public int TargetPosition { get; set; }
    public PieceState TargetState { get; set; }
    
    public int MoveDistance { get; set; }
    public Direction MoveDirection { get; set; }
    public bool MovesToBaseFromBriefing { get; set; }
    public bool MovesFromBaseToX { get; set; }
    public bool LandsOnMysteryCell { get; set; }
    
    public List<Piece> CapturedPieces { get; set; } = new List<Piece>();
    
    // Helper to identify if it captures anything
    public bool DoesCapture => CapturedPieces.Count > 0;
}
