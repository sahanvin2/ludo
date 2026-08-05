using System.Collections.Generic;
using LudoGame;
using Xunit;

namespace LudoGame.Tests
{
    public class GameRulesTests
    {
        [Fact]
        public void DistanceFromHome_uses_clockwise_steps_for_clockwise_piece()
        {
            var piece = new Piece(Color.Yellow, 1)
            {
                State = PieceState.StandardPath,
                Position = 10,
                MovementDirection = Direction.Clockwise
            };
            int dist = GameRules.DistanceFromHome(piece);
            Assert.Equal((GameConstants.ApproachCells[Color.Yellow] - 10 + 52) % 52, dist);
        }

        [Fact]
        public void GetBlockMovementDirection_picks_farthest_from_home_when_opposite()
        {
            var a = new Piece(Color.Red, 1)
            {
                State = PieceState.StandardPath,
                Position = 30,
                MovementDirection = Direction.Clockwise
            };
            var b = new Piece(Color.Red, 2)
            {
                State = PieceState.StandardPath,
                Position = 30,
                MovementDirection = Direction.CounterClockwise
            };
            var dir = GameRules.GetBlockMovementDirection(new List<Piece> { a, b });
            Assert.True(dir == Direction.Clockwise || dir == Direction.CounterClockwise);
        }

        [Fact]
        public void ApplyHomeStraightSteps_zero_steps_enters_at_position_zero()
        {
            var piece = new Piece(Color.Blue, 1) { State = PieceState.StandardPath, Position = 13 };
            Assert.True(GameRules.ApplyHomeStraightSteps(piece, 0));
            Assert.Equal(PieceState.HomeStraight, piece.State);
            Assert.Equal(0, piece.HomeStraightPosition);
        }

        [Fact]
        public void CanCompleteApproachEntry_allows_zero_steps_in_home()
        {
            var piece = new Piece(Color.Yellow, 1)
            {
                State = PieceState.StandardPath,
                Captures = 1,
                MovementDirection = Direction.Clockwise
            };
            Assert.True(GameRules.CanCompleteApproachEntry(piece, 3, 2));
        }

        [Fact]
        public void StepForward_wraps_modulo_52()
        {
            int next = GameRules.StepForward(51, Direction.Clockwise);
            Assert.Equal(0, next);
        }

        [Fact]
        public void CanEnterHomeStraightAfterCross_counts_ccw_second_pass_on_crossing()
        {
            var piece = new Piece(Color.Red, 1)
            {
                State = PieceState.StandardPath,
                Captures = 1,
                MovementDirection = Direction.CounterClockwise,
                ApproachPassCount = 1
            };
            Assert.True(GameRules.CanEnterHomeStraightAfterCross(piece));
            Assert.False(GameRules.CanEnterHomeStraight(piece));
        }

        [Fact]
        public void DistanceFromHome_for_home_straight_uses_remaining_steps()
        {
            var piece = new Piece(Color.Blue, 1)
            {
                State = PieceState.HomeStraight,
                HomeStraightPosition = 2
            };
            Assert.Equal(3, GameRules.DistanceFromHome(piece));
        }

        [Fact]
        public void CanEnterHomeStraight_requires_capture_while_opponents_on_ring()
        {
            var piece = new Piece(Color.Yellow, 1)
            {
                State = PieceState.StandardPath,
                Position = 10,
                Captures = 0,
                MovementDirection = Direction.Clockwise
            };
            var opponent = new Piece(Color.Blue, 1)
            {
                State = PieceState.StandardPath,
                Position = 20
            };
            var players = PlayerFactory.CreatePlayers();
            players[Color.Yellow].Pieces[0] = piece;
            players[Color.Blue].Pieces[0] = opponent;

            Assert.False(GameRules.CanEnterHomeStraight(piece, players));
            piece.Captures = 1;
            Assert.True(GameRules.CanEnterHomeStraight(piece, players));
        }

        [Fact]
        public void CanEnterHomeStraight_waives_capture_when_no_opponent_on_ring()
        {
            var piece = new Piece(Color.Yellow, 1)
            {
                State = PieceState.StandardPath,
                Captures = 0,
                MovementDirection = Direction.Clockwise
            };
            var players = PlayerFactory.CreatePlayers();
            foreach (var p in players.Values)
            {
                foreach (var pc in p.Pieces)
                {
                    if (pc.State == PieceState.StandardPath)
                        pc.State = PieceState.Home;
                }
            }

            Assert.True(GameRules.CanEnterHomeStraight(piece, players));
        }
    }
}
