using System.Linq;

namespace LudoT_Simulation;

public class RedPlayer : Player
{
    public RedPlayer(Color color) : base(color) { }

    public override bool PlayTurn(int roll, GameEngine engine)
    {
        var moves = engine.GetPossibleMoves(this, roll);
        if (moves.Count == 0) return false;

        // 1. Prioritize Capturing
        var captureMoves = moves.Where(m => m.DoesCapture).ToList();
        if (captureMoves.Any())
        {
            // Closest to opponent home means the captured piece that has travelled the furthest.
            // Simplified: we'll just pick the first one for now, or sort by some metric.
            var bestCapture = captureMoves.First();
            engine.ExecuteMove(this, bestCapture);
            return true;
        }

        // 2. Keep exactly one piece on path if possible
        int onPath = Pieces.Count(p => p.State == PieceState.StandardPath);
        var baseToXMoves = moves.Where(m => m.MovesFromBaseToX).ToList();
        
        if (baseToXMoves.Any() && onPath == 0)
        {
            engine.ExecuteMove(this, baseToXMoves.First());
            return true;
        }

        // 3. Avoid blocks
        var nonBlockMoves = moves.Where(m => !WouldCreateBlock(m, engine)).ToList();
        var safeMoves = nonBlockMoves.Any() ? nonBlockMoves : moves;

        // Just move the first available piece
        var finalMove = safeMoves.FirstOrDefault(m => !m.MovesFromBaseToX) ?? safeMoves.First();
        engine.ExecuteMove(this, finalMove);
        return true;
    }

    private bool WouldCreateBlock(Move m, GameEngine engine)
    {
        if (m.TargetState != PieceState.StandardPath) return false;
        return engine.AllPieces.Any(p => p.Color == this.Color && p != m.Piece && p.State == PieceState.StandardPath && p.Position == m.TargetPosition);
    }
}
