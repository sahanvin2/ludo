namespace LudoGame
{
    /// <summary>
    /// Default random number provider used by the simulation.
    /// Supports optional seeding for deterministic tests.
    /// </summary>
    public sealed class GameRandom : IRandomSource
    {
        private readonly Random _random;

        public GameRandom(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
