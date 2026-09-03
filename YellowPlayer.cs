using System.Linq;

namespace LudoT_Simulation;

public class YellowPlayer : Player
{
    public YellowPlayer(Color color) : base(color) { }

    public override bool PlayTurn(int roll, GameEngine engine)
    {
        var moves = engine.GetPossibleMoves(this, roll);
        if (moves.Count == 0) return false;

        var baseToXMoves = moves.Where(m => m.MovesFromBaseToX).ToList();
        if (baseToXMoves.Any())
        {
            engine.ExecuteMove(this, baseToXMoves.First());
            return true;
        }

        var needsCaptureMoves = moves.Where(m => m.DoesCapture && m.Piece.CapturedPiecesCount == 0).ToList();
        if (needsCaptureMoves.Any())
        {
            engine.ExecuteMove(this, needsCaptureMoves.First());
            return true;
        }

        // Closest to home straight
        var ordered = moves.OrderBy(m => DistanceToHomeStraight(m.Piece)).ToList();
        engine.ExecuteMove(this, ordered.First());
        return true;
    }

    private int DistanceToHomeStraight(Piece piece)
    {
        if (piece.State == PieceState.HomeStraight) return 5 - piece.Position;
        if (piece.State == PieceState.Base) return 100;
        
        int approach = Board.GetApproachCell(piece.Color);
        if (piece.Direction == Direction.Clockwise)
        {
            return Board.DistanceTo(piece.Position, approach, Direction.Clockwise);
        }
        else
        {
            return Board.DistanceTo(piece.Position, approach, Direction.CounterClockwise);
        }
    }
}
