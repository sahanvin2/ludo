using LudoGame;
using Xunit;
using Xunit.Abstractions;

namespace LudoGame.Tests
{
    /// <summary>
    /// Prints a readable catalogue of test types when the suite runs (for report screenshots).
    /// </summary>
    public class TestSuiteSummaryTests
    {
        private readonly ITestOutputHelper _output;

        public TestSuiteSummaryTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Test_suite_catalog_lists_all_categories_and_counts()
        {
            _output.WriteLine("");
            _output.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            _output.WriteLine("║           LUDO-T — CLEAN TEST SUITE CATALOGUE               ║");
            _output.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            _output.WriteLine("");
            _output.WriteLine("Total automated tests: 27 (including this catalogue test)");
            _output.WriteLine("");
            _output.WriteLine("┌─────────────────────────┬───────┬────────────────────────────────────────┐");
            _output.WriteLine("│ Test class              │ Count │ Type / purpose                         │");
            _output.WriteLine("├─────────────────────────┼───────┼────────────────────────────────────────┤");
            _output.WriteLine("│ ArchitectureTests       │   2   │ Unit — Factory, Dependency Injection │");
            _output.WriteLine("│ GameLoggerTests         │   3   │ Unit — Logger output formatting       │");
            _output.WriteLine("│ GameRulesTests          │   9   │ Unit — Pure rule helpers (T-4, T-7)    │");
            _output.WriteLine("│ HomePathTests           │   9   │ Unit — Home straight & Rule 10         │");
            _output.WriteLine("│ ApproachEntryTests      │   2   │ Unit — CCW approach crossing (T-1)     │");
            _output.WriteLine("│ GreenPlayerTests        │   1   │ Unit — Green AI strategy               │");
            _output.WriteLine("│ TestSuiteSummaryTests   │   1   │ Documentation — suite catalogue        │");
            _output.WriteLine("└─────────────────────────┴───────┴────────────────────────────────────────┘");
            _output.WriteLine("");
            _output.WriteLine("Test types used:");
            _output.WriteLine("  • Unit tests        — isolated logic (GameRules, MovementEngine.Evaluate)");
            _output.WriteLine("  • Architecture tests — design (Factory, injectable GameManager)");
            _output.WriteLine("  • Behaviour tests   — player ChooseMove (Green home-straight priority)");
            _output.WriteLine("");
            _output.WriteLine("Run command:");
            _output.WriteLine("  dotnet test LudoGame.Tests/LudoGame.Tests.csproj");
            _output.WriteLine("");
            Assert.True(true);
        }
    }
}
