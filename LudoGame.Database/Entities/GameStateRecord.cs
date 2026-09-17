using System;

namespace LudoGame.Database.Entities
{
    public class GameStateRecord
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string SerializedSnapshot { get; set; } = string.Empty;
        public string LatestAction { get; set; } = string.Empty;
    }
}
