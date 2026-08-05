namespace LudoGame
{
    /// <summary>
    /// Abstracts dice rolling so tests can provide deterministic values.
    /// </summary>
    public interface IDiceService
    {
        int Roll();
    }

    public sealed class DiceService : IDiceService
    {
        private readonly IRandomSource _random;
        public DiceService(IRandomSource random) => _random = random;
        public int Roll() => _random.Next(1, 7);
    }
}
