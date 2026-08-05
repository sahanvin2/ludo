using System.Collections.Generic;

namespace LudoGame
{
    /// <summary>
    /// Observer pattern: subscribers are notified when important game events occur.
    /// </summary>
    public interface IGameObserver
    {
        void OnPlayerPlaced(IPlayer player, int place);
    }

    /// <summary>
    /// Null Object pattern: no-op observer used when no tracking is required (e.g. unit tests).
    /// </summary>
    public sealed class NullGameObserver : IGameObserver
    {
        public void OnPlayerPlaced(IPlayer player, int place) { }
    }

    /// <summary>
    /// Observer that records each placement for statistics or future extensions.
    /// </summary>
    public sealed class PlacementTrackerObserver : IGameObserver
    {
        private readonly List<(Color color, int place)> _placements = new();

        public IReadOnlyList<(Color color, int place)> Placements => _placements;

        public void OnPlayerPlaced(IPlayer player, int place) =>
            _placements.Add((player.Color, place));
    }
}
