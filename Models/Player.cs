using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoT_Arena.Models;

public abstract class Player
{
    public Color Color { get; }
    public List<Piece> Pieces { get; }

    protected Player(Color color)
    {
        Color = color;
        Pieces = new List<Piece>
        {
            new Piece($"{color.ToString().Substring(0, 1)}1", color),
            new Piece($"{color.ToString().Substring(0, 1)}2", color),
            new Piece($"{color.ToString().Substring(0, 1)}3", color),
            new Piece($"{color.ToString().Substring(0, 1)}4", color)
        };
    }

    public abstract bool PlayTurn(int roll, GameEngine engine);

    public int PiecesOnBoard() => Pieces.Count(p => p.State != PieceState.Base && p.State != PieceState.Home);
    public int PiecesInBase() => Pieces.Count(p => p.State == PieceState.Base);
    public int PiecesInHome() => Pieces.Count(p => p.State == PieceState.Home);
}
