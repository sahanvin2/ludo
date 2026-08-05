namespace LudoGame
{
    /// <summary>
    /// Helper utilities for color ordering and display formatting.
    /// </summary>
    public static class ColorHelper
    {
        public static readonly Color[] ClockwisePlayOrder = { Color.Red, Color.Green, Color.Yellow, Color.Blue };

        public static string ToLowerString(this Color color) => color switch
        {
            Color.Red => "red",
            Color.Yellow => "yellow",
            Color.Green => "green",
            Color.Blue => "blue",
            _ => color.ToString().ToLower()
        };

        public static Color NextLeft(Color color)
        {
            int i = System.Array.IndexOf(ClockwisePlayOrder, color);
            return ClockwisePlayOrder[(i + 1) % ClockwisePlayOrder.Length];
        }
    }
}
