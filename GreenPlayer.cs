using System.Linq;

namespace LudoT_Simulation;

public class GreenPlayer : Player
{
    public GreenPlayer(Color color) : base(color) { }

    public override bool PlayTurn(int roll, GameEngine engine)
    {
        var moves = engine.GetPossibleMoves(this, roll);
        if (moves.Count == 0) return false;

        // Empty base whenever 6 is thrown unless creating a block
        var baseToXMoves = moves.Where(m => m.MovesFromBaseToX).ToList();
        if (baseToXMoves.Any())
        {
            var createBlockMoves = moves.Where(m => WouldCreateBlock(m, engine)).ToList();
            if (createBlockMoves.Any())
            {
                engine.ExecuteMove(this, createBlockMoves.First());
                return true;
            }
            engine.ExecuteMove(this, baseToXMoves.First());
            return true;
        }

        // Prioritize block moves
        var blockMoves = moves.Where(m => m.IsBlockMove).ToList();
        if (blockMoves.Any())
        {
            engine.ExecuteMove(this, blockMoves.First());
            return true;
        }

        // Prioritize moving other pieces home before breaking block
        var nonBlockBreakMoves = moves.Where(m => !IsBreakingBlock(m, engine)).ToList();
        if (nonBlockBreakMoves.Any())
        {
            engine.ExecuteMove(this, nonBlockBreakMoves.First());
            return true;
        }

        // Default
        engine.ExecuteMove(this, moves.First());
        return true;
    }

    private bool WouldCreateBlock(Move m, GameEngine engine)
    {
        if (m.TargetState != PieceState.StandardPath) return false;
        return engine.AllPieces.Any(p => p.Color == this.Color && p != m.Piece && p.State == PieceState.StandardPath && p.Position == m.TargetPosition);
    }
    
    private bool IsBreakingBlock(Move m, GameEngine engine)
    {
        if (m.IsBlockMove) return false;
        var blocks = engine.GetPlayerBlocks(this);
        return blocks.Any(b => b.Count > 1 && b.Contains(m.Piece));
    }
}
