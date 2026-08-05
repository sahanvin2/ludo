using LudoGame;
using Xunit;

namespace LudoGame.Tests
{
    public class HomePathTests
    {
        [Fact]
        public void StepsInHome_zero_means_entrance_tile_not_invalid()
        {
            Assert.True(GameRules.TryGetApproachCrossing(
                Color.Blue, 8, 5, Direction.Clockwise, out int approachIdx, out int stepsInHome));
            Assert.Equal(0, stepsInHome);

            var piece = new Piece(Color.Blue, 3);
            Assert.True(GameRules.ApplyHomeStraightSteps(piece, 0));
            Assert.Equal(PieceState.HomeStraight, piece.State);
            Assert.Equal(0, piece.HomeStraightPosition);
            Assert.Equal(-1, piece.Position);
        }

        [Fact]
        public void Cw_from_8_by_5_lands_on_approach_with_zero_home_steps()
        {
            var path = GameRules.GetCellsAlongPath(8, 5, Direction.Clockwise);
            Assert.Equal(13, path[^1]);
            Assert.True(GameRules.TryGetApproachCrossing(Color.Blue, 8, 5, Direction.Clockwise, out _, out int stepsInHome));
            Assert.Equal(0, stepsInHome);
        }

        [Fact]
        public void ApplyHomeStraightSteps_maps_steps_to_positions_0_through_4()
        {
            for (int steps = 1; steps <= 4; steps++)
            {
                var piece = new Piece(Color.Red, 1);
                Assert.True(GameRules.ApplyHomeStraightSteps(piece, steps));
                Assert.Equal(steps, piece.HomeStraightPosition);
                Assert.True(GameRules.IsValidHomePathPosition(piece.HomeStraightPosition));
            }
        }

        [Fact]
        public void Exact_roll_from_homepath0_requires_5()
        {
            var piece = new Piece(Color.Blue, 1)
            {
                State = PieceState.HomeStraight,
                HomeStraightPosition = 0
            };
            Assert.False(piece.HomeStraightPosition + 4 == GameConstants.HomeStraightLength);
            Assert.True(piece.HomeStraightPosition + 5 == GameConstants.HomeStraightLength);
        }

        [Fact]
        public void Exact_roll_from_homepath4_requires_1()
        {
            var piece = new Piece(Color.Blue, 1)
            {
                State = PieceState.HomeStraight,
                HomeStraightPosition = 4
            };
            Assert.True(piece.HomeStraightPosition + 1 == GameConstants.HomeStraightLength);
        }

        [Fact]
        public void FormatPieceLocation_uses_bracketed_home_path_index()
        {
            var piece = new Piece(Color.Blue, 3)
            {
                State = PieceState.HomeStraight,
                HomeStraightPosition = 0
            };
            Assert.Equal("blue homepath[0]", GameRules.FormatPieceLocation(piece, Color.Blue));
        }

        [Fact]
        public void HomeStraight_piece_can_move_partially_along_home_path()
        {
            var piece = new Piece(Color.Green, 1)
            {
                State = PieceState.HomeStraight,
                HomeStraightPosition = 1
            };

            var players = PlayerFactory.CreatePlayers();
            var board = new Board(new TestRandom(new[] { 1 }));
            var outcome = MovementEngine.Evaluate(players[Color.Green], piece, 2, board, players);

            Assert.Equal(MoveResult.Moved, outcome.Result);
        }

        [Fact]
        public void HomeStraight_piece_cannot_move_beyond_home()
        {
            var piece = new Piece(Color.Green, 1)
            {
                State = PieceState.HomeStraight,
                HomeStraightPosition = 4
            };

            var players = PlayerFactory.CreatePlayers();
            var board = new Board(new TestRandom(new[] { 1 }));
            var outcome = MovementEngine.Evaluate(players[Color.Green], piece, 2, board, players);

            Assert.Equal(MoveResult.NoMove, outcome.Result);
        }

        [Fact]
        public void Overshoot_home_path_returns_false()
        {
            var piece = new Piece(Color.Green, 1);
            Assert.False(GameRules.ApplyHomeStraightSteps(piece, 6));
        }
    }
}
