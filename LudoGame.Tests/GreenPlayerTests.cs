using System.Collections.Generic;
using LudoGame;
using Xunit;

namespace LudoGame.Tests
{
    public class GreenPlayerTests
    {
        [Fact]
        public void ChooseMove_advances_piece_on_home_straight_when_roll_is_exact()
        {
            var green = new GreenPlayer();
            var piece = green.Pieces[0];
            piece.State = PieceState.HomeStraight;
            piece.HomeStraightPosition = 4;
            piece.Captures = 1;

            var board = new Board(new GameRandom(42));
            var players = PlayerFactory.CreatePlayers();
            players[Color.Green] = green;

            var choice = green.ChooseMove(1, board, players);

            Assert.NotNull(choice);
            Assert.Equal(piece, choice!.Value.piece);
            Assert.False(choice.Value.fromBase);
        }
    }
}
