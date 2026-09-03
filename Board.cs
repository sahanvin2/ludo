using System;

namespace LudoT_Simulation;

public static class Board
{
    public const int TotalStandardCells = 52;
    public const int HomeStraightLength = 5;

    public static int GetApproachCell(Color color)
    {
        return color switch
        {
            Color.Yellow => 0,
            Color.Blue => 13,
            Color.Red => 26,
            Color.Green => 39,
            _ => 0
        };
    }

    public static int GetStartCellX(Color color)
    {
        return color switch
        {
            Color.Yellow => 2,
            Color.Blue => 15,
            Color.Red => 28,
            Color.Green => 41,
            _ => 2
        };
    }

    public static int NormalizePosition(int pos)
    {
        if (pos < 0) return (pos % TotalStandardCells + TotalStandardCells) % TotalStandardCells;
        return pos % TotalStandardCells;
    }

    // Distance forward from start to target (considering direction)
    public static int DistanceTo(int fromPos, int toPos, Direction dir)
    {
        if (dir == Direction.Clockwise)
        {
            return NormalizePosition(toPos - fromPos);
        }
        else
        {
            return NormalizePosition(fromPos - toPos);
        }
    }
}
