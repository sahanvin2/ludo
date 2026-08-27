using System.Linq;

namespace LudoT_Arena.Models;

public class BluePlayer : Player
{
    private int currentPieceIndex = 0;

    public BluePlayer(Color color) : base(color) { }

    public override bool PlayTurn(int roll, GameEngine engine)
    {
        var moves = engine.GetPossibleMoves(this, roll);
        if (moves.Count == 0) return false;

        // Try to cycle pieces
        for (int i = 0; i < Pieces.Count; i++)
        {
            int idx = (currentPieceIndex + i) % Pieces.Count;
            var targetPiece = Pieces[idx];

            var pieceMoves = moves.Where(m => m.Piece == targetPiece).ToList();
            if (pieceMoves.Any())
            {
                var move = pieceMoves.First();
                
                // Prioritize mystery cell logic
                if (targetPiece.Direction == Direction.CounterClockwise)
                {
                    var mysteryMove = pieceMoves.FirstOrDefault(m => m.LandsOnMysteryCell);
                    if (mysteryMove != null) move = mysteryMove;
                }
                else
                {
                    var avoidMysteryMove = pieceMoves.FirstOrDefault(m => !m.LandsOnMysteryCell);
                    if (avoidMysteryMove != null) move = avoidMysteryMove;
                }

                currentPieceIndex = (idx + 1) % Pieces.Count;
                engine.ExecuteMove(this, move);
                return true;
            }
        }

        engine.ExecuteMove(this, moves.First());
        return true;
    }
}
