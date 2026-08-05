using System;
using System.IO;
using Xunit;

namespace LudoGame.Tests
{
    public class GameLoggerTests
    {
        [Fact]
        public void LogMysterySpawn_uses_singular_round_text_for_one_round()
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            GameLogger.LogMysterySpawn(5, 1);

            var output = writer.ToString().Trim();
            Assert.Equal("A mystery cell has spawned in location 5 and will be at this location for the next 1 round.", output);
        }

        [Fact]
        public void LogMysteryCellStatus_uses_singular_round_text_for_one_round()
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            GameLogger.LogMysteryCellStatus(12, 1);

            var output = writer.ToString().Trim();
            Assert.Equal("The mystery cell is at 12 and will be at that location for the next 1 round.", output);
        }

        [Fact]
        public void LogPartialMove_includes_player_color()
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);

            GameLogger.LogPartialMove(Color.Blue, 17);

            var output = writer.ToString().Trim();
            Assert.Equal("blue moved the piece to square 17, which is the cell before the block.", output);
        }
    }
}
