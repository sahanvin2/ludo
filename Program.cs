using System;
using LudoGame;

namespace LudoGameSimulation
{
    /// <summary>
    /// Entry-point for the LUDO-T simulation.
    /// This application starts the game controller and does not accept runtime user input.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Default: step through output section by section. Use --no-pause for continuous output.
            bool interactive = !Array.Exists(args, a => a.Equals("--no-pause", StringComparison.OrdinalIgnoreCase));
            var game = new LudoGameController(interactiveSections: interactive);
            game.StartGame();
        }
    }
}
