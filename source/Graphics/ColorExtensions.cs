using Microsoft.Xna.Framework;

namespace Sol2E.Graphics
{
    /// <summary>
    /// Extension class for Color
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns the inverted color (doesn't change alpha channel).
        /// </summary>
        /// <param name="color">color instance</param>
        /// <returns>inverted color</returns>
        public static Color Inverted(this Color color)
        {
            return new Color(255 - color.R, 255 - color.G, 255 - color.B, color.A);
        }

        /// <summary>
        /// Sets the alpha value of a color instance and returns it.
        /// </summary>
        /// <param name="color">color instance</param>
        /// <param name="alpha">alpha value</param>
        /// <returns>color with new alpha value</returns>
        public static Color AddAlpha(this Color color, byte alpha)
        {
            color.A = alpha;
            return color;
        }
    }
}
