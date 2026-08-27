using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoT_Arena.Models;

public class GameEngine
{
    public static Action<string>? OnLogEvent;

    public static void Log(string message)
    {
        OnLogEvent?.Invoke(message);
    }

    public List<Player> Players { get; }
    public MysteryCellManager MysteryCell { get; }
    public List<Piece> AllPieces => Players.SelectMany(p => p.Pieces).ToList();
    
    private Random rand = new Random();

    public GameEngine(List<Player> players)
    {
        Players = players;
        MysteryCell = new MysteryCellManager();
    }

    public List<Move> GetPossibleMoves(Player player, int roll)
    {
        List<Move> moves = new List<Move>();
        
        // Find blocks first to see if any block move is possible
        var blocks = GetPlayerBlocks(player);
        
        foreach (var piece in player.Pieces)
        {
            if (piece.State == PieceState.Home) continue;

            if (piece.Status == StatusEffect.Briefing)
            {
                if (roll == 3)
                {
                    var m = new Move { Piece = piece, MovesToBaseFromBriefing = (piece.ConsecutiveThreesRolled + 1) == 3 };
                    // We can return a special move if it's the 3rd three, but for now we'll just track it inside GameEngine
                    // Let's create a dummy move that just records the roll
                    moves.Add(new Move { Piece = piece, MoveDistance = 0, TargetPosition = piece.Position, TargetState = piece.State });
                }
                continue; // Cannot move otherwise
            }

            if (piece.State == PieceState.Base)
            {
                if (roll == 6)
                {
                    moves.Add(new Move
                    {
                        Piece = piece,
                        MovesFromBaseToX = true,
                        TargetState = PieceState.StandardPath,
                        TargetPosition = Board.GetStartCellX(player.Color),
                        MoveDistance = 1
                    });
                }
                continue;
            }

            // Normal piece move
            int effectiveRoll = roll;
            if (piece.Status == StatusEffect.Energized) effectiveRoll = roll * 2;
            else if (piece.Status == StatusEffect.Sick) effectiveRoll = roll / 2;

            if (effectiveRoll == 0) continue;

            if (piece.State == PieceState.HomeStraight)
            {
                int newPos = piece.Position + effectiveRoll;
                if (newPos == Board.HomeStraightLength) // Exact roll to enter Home (5)
                {
                    moves.Add(new Move { Piece = piece, MoveDistance = effectiveRoll, TargetPosition = 5, TargetState = PieceState.Home });
                }
                else if (newPos < Board.HomeStraightLength)
                {
                    moves.Add(new Move { Piece = piece, MoveDistance = effectiveRoll, TargetPosition = newPos, TargetState = PieceState.HomeStraight });
                }
                continue;
            }

            if (piece.State == PieceState.StandardPath)
            {
                // Is piece in a block?
                var block = blocks.FirstOrDefault(b => b.Contains(piece));
                if (block != null && block.Count > 1)
                {
                    // Can break block by moving individually
                    AddPathMove(moves, piece, effectiveRoll, false, null);
                }
                else
                {
                    AddPathMove(moves, piece, effectiveRoll, false, null);
                }
            }
        }
        
        // Block moves
        foreach (var block in blocks)
        {
            if (block.Count > 1)
            {
                int blockDist = roll / block.Count;
                if (blockDist > 0)
                {
                    // Block moves in direction of longest distance from home
                    Direction moveDir = GetBlockDirection(block);
                    // Add block move
                    AddPathMove(moves, block[0], blockDist, true, block);
                }
            }
        }
        
        return moves;
    }

    private Direction GetBlockDirection(List<Piece> block)
    {
        // Distance from home: If CW, distance is 50 to approach. CCW, 54 to approach.
        // We find the piece with max distance to home straight and use its direction.
        // To simplify, we'll calculate steps to Approach.
        Piece longest = block[0];
        int maxDist = -1;
        foreach (var p in block)
        {
            int dist = GetDistanceToApproach(p);
            if (dist > maxDist)
            {
                maxDist = dist;
                longest = p;
            }
        }
        return longest.Direction;
    }

    private int GetDistanceToApproach(Piece p)
    {
        int approach = Board.GetApproachCell(p.Color);
        if (p.Direction == Direction.Clockwise)
        {
            return Board.DistanceTo(p.Position, approach, Direction.Clockwise);
        }
        else
        {
            // CCW needs to pass approach twice if it just started, but we just use basic distance for now
            return Board.DistanceTo(p.Position, approach, Direction.CounterClockwise);
        }
    }

    private void AddPathMove(List<Move> moves, Piece piece, int dist, bool isBlockMove, List<Piece> blockPieces)
    {
        int currentPos = piece.Position;
        Direction dir = isBlockMove ? GetBlockDirection(blockPieces) : piece.Direction;
        int targetPos = currentPos;
        bool entersHomeStraight = false;

        // Step by step to check blocks
        for (int i = 1; i <= dist; i++)
        {
            targetPos = (dir == Direction.Clockwise) ? Board.NormalizePosition(targetPos + 1) : Board.NormalizePosition(targetPos - 1);
            
            // Check for opponent block
            var oppBlock = GetBlockAt(targetPos, piece.Color);
            if (oppBlock != null && oppBlock.Count > 1)
            {
                // Blocked! Can only move up to here.
                targetPos = (dir == Direction.Clockwise) ? Board.NormalizePosition(targetPos - 1) : Board.NormalizePosition(targetPos + 1);
                dist = i - 1;
                break;
            }

            // Check if passed approach
            int approach = Board.GetApproachCell(piece.Color);
            if (targetPos == approach)
            {
                // Can enter if captured at least 1 (Rule T-7)
                if (piece.CapturedPiecesCount > 0)
                {
                    if (dir == Direction.Clockwise)
                    {
                        // Enters next step
                        entersHomeStraight = true;
                    }
                    else
                    {
                        // CCW must pass twice. For now, assume if it hits it, it can enter if it travelled enough.
                        // We'll simplify: CCW piece can enter if it hits approach and has traveled at least > 0.
                        entersHomeStraight = true; 
                    }
                }
            }
        }

        if (entersHomeStraight)
        {
            // Need remaining steps in home straight
            // (Simplification for now: entering home straight consumes 1 step past approach)
            int remaining = dist - Board.DistanceTo(currentPos, Board.GetApproachCell(piece.Color), dir);
            if (remaining > 0 && remaining <= 5)
            {
                moves.Add(new Move { Piece = piece, IsBlockMove = isBlockMove, BlockPieces = blockPieces, TargetPosition = remaining - 1, TargetState = PieceState.HomeStraight, MoveDistance = dist, MoveDirection = dir });
                return;
            }
        }

        if (dist == 0) return;

        // Check captures
        var captures = new List<Piece>();
        foreach (var p in AllPieces)
        {
            if (p.Color != piece.Color && p.State == PieceState.StandardPath && p.Position == targetPos)
            {
                // Capture unless it's a block (we already stopped before blocks, but what if block is 1 piece? Wait, block is 2+ pieces).
                // Single piece is captured.
                var blockTest = GetBlockAt(targetPos, piece.Color);
                if (blockTest == null) 
                {
                    captures.Add(p);
                }
                else if (isBlockMove && blockPieces != null && blockPieces.Count == blockTest.Count)
                {
                    // Rule T-8: Blockade of same size can capture blockade
                    captures.AddRange(blockTest);
                }
            }
        }

        bool landsOnMystery = (targetPos == MysteryCell.CurrentLocation);

        moves.Add(new Move
        {
            Piece = piece,
            IsBlockMove = isBlockMove,
            BlockPieces = blockPieces,
            TargetPosition = targetPos,
            TargetState = PieceState.StandardPath,
            MoveDistance = dist,
            MoveDirection = dir,
            CapturedPieces = captures,
            LandsOnMysteryCell = landsOnMystery
        });
    }

    public List<List<Piece>> GetPlayerBlocks(Player player)
    {
        var blocks = new List<List<Piece>>();
        var onPath = player.Pieces.Where(p => p.State == PieceState.StandardPath).ToList();
        var groups = onPath.GroupBy(p => p.Position).Where(g => g.Count() >= 1); // We'll just group them
        foreach (var g in groups)
        {
            blocks.Add(g.ToList());
        }
        return blocks;
    }

    private List<Piece>? GetBlockAt(int position, Color myColor)
    {
        var piecesAtPos = AllPieces.Where(p => p.State == PieceState.StandardPath && p.Position == position && p.Color != myColor).ToList();
        if (piecesAtPos.Count > 1) return piecesAtPos; // Block
        return null;
    }

    public void ExecuteMove(Player player, Move move)
    {
        var piecesToMove = new List<Piece>();
        if (move.IsBlockMove) piecesToMove.AddRange(move.BlockPieces);
        else piecesToMove.Add(move.Piece);

        // Logging
        if (move.MovesFromBaseToX)
        {
            GameEngine.Log($"[{player.Color}] player moves piece {move.Piece.Id} to the starting point.");
            GameEngine.Log($"[{player.Color}] player now has {player.PiecesOnBoard() + 1}/4 on pieces on the board and {player.PiecesInBase() - 1}/4 pieces on the base.");
            
            // Removed coin toss to ensure standard Ludo clockwise movement
        }
        else if (move.MovesToBaseFromBriefing)
        {
            GameEngine.Log($"[{player.Color}] piece {move.Piece.Id} is movement-restricted and has rolled three consecutively. Teleporting piece {move.Piece.Id} to base.");
        }
        else if (move.MoveDistance > 0)
        {
            foreach (var p in piecesToMove)
            {
                GameEngine.Log($"[{player.Color}] moves piece {p.Id} from location {p.Position} to {move.TargetPosition} by {move.MoveDistance} units in {move.MoveDirection.ToString().ToLower()} direction.");
            }
        }

        // Apply state changes
        foreach (var p in piecesToMove)
        {
            p.Position = move.TargetPosition;
            p.State = move.TargetState;

            if (move.MovesToBaseFromBriefing)
            {
                p.Reset();
            }
        }

        // Captures
        if (move.DoesCapture)
        {
            foreach (var captured in move.CapturedPieces)
            {
                GameEngine.Log($"[{player.Color}] piece {move.Piece.Id} lands on square {move.TargetPosition}, captures [{captured.Color}] piece {captured.Id}, and returns it to the base.");
                captured.Reset();
                
                // Increment capture count for all participating in capture (Rule T-8)
                foreach (var p in piecesToMove) p.CapturedPiecesCount++;
            }
            GameEngine.Log($"[{player.Color}] player now has {player.PiecesOnBoard()}/4 on pieces on the board and {player.PiecesInBase()}/4 pieces on the base.");
        }

        // Mystery Cell
        if (move.LandsOnMysteryCell)
        {
            foreach (var p in piecesToMove)
            {
                MysteryCell.TeleportPiece(p);
            }
        }
    }
}
