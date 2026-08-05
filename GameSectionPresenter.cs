using System;

namespace LudoGame
{
    /// <summary>
    /// Controls how the simulation output is grouped into readable sections.
    /// Strategy pattern for presentation: different presentation strategies can be interchanged without changing game flow.
    /// </summary>
    public interface IGameSectionPresenter
    {
        void BeginSection(string title);
        void WaitForContinue(string? reason = null);
    }

    /// <summary>
    /// No-op presenter used by unit tests and non-interactive runs.
    /// </summary>
    public sealed class NullSectionPresenter : IGameSectionPresenter
    {
        public void BeginSection(string title) { }
        public void WaitForContinue(string? reason = null) { }
    }

    /// <summary>
    /// Interactive presenter: prints a section header and blocks until the user presses ENTER.
    /// Uses ReadLine (not ReadKey) for reliable blocking in IDE and integrated terminals.
    /// </summary>
    public sealed class InteractiveSectionPresenter : IGameSectionPresenter
    {
        private int _sectionNumber;

        public void BeginSection(string title)
        {
            _sectionNumber++;
            Console.Out.Flush();
            Console.WriteLine();
            Console.WriteLine("============================");
            Console.WriteLine($"SECTION {_sectionNumber}: {title}");
            Console.WriteLine("============================");
            Console.WriteLine();
            Console.Out.Flush();
        }

        public void WaitForContinue(string? reason = null)
        {
            if (Console.IsInputRedirected)
                return;

            Console.Out.Flush();
            string next = string.IsNullOrWhiteSpace(reason) ? "the next section" : reason;
            Console.WriteLine();
            Console.WriteLine($">>> Press ENTER to continue ({next}) <<<");
            Console.Out.Flush();
            Console.In.ReadLine();
            Console.WriteLine();
        }
    }
}
