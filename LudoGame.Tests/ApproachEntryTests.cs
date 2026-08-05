using LudoGame;
using Xunit;

namespace LudoGame.Tests
{
    public class ApproachEntryTests
    {
        [Fact]
        public void Ccw_from_18_by_6_ring_end_is_12_but_home_entry_is_position_1()
        {
            const int start = 18;
            const int movement = 6;
            var direction = Direction.CounterClockwise;

            Assert.Equal(12, GameRules.RingEndPosition(start, movement, direction));

            Assert.True(GameRules.TryGetApproachCrossing(Color.Blue, start, movement, direction, out int approachIdx, out int stepsInHome));
            Assert.Equal(13, GameConstants.ApproachCells[Color.Blue]);
            Assert.Equal(4, approachIdx);
            Assert.Equal(1, stepsInHome);

            var piece = new Piece(Color.Blue, 3) { Captures = 1, ApproachPassCount = 2 };
            Assert.True(GameRules.ApplyHomeStraightSteps(piece, stepsInHome));
            Assert.Equal(1, piece.HomeStraightPosition);
        }

        [Fact]
        public void Ccw_crossing_approach_requires_second_pass_before_entry()
        {
            var piece = new Piece(Color.Blue, 3)
            {
                State = PieceState.StandardPath,
                MovementDirection = Direction.CounterClockwise,
                Captures = 1,
                ApproachPassCount = 1
            };

            Assert.True(GameRules.CanEnterHomeStraightAfterCross(piece));
            Assert.False(GameRules.CanEnterHomeStraight(piece));
        }
    }
}
