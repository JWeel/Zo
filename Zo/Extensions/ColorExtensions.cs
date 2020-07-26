using Microsoft.Xna.Framework;

namespace Zo.Extensions
{
    public static class ColorExtensions
    {
        #region Constants

        public const int COLOR_DEPTH = byte.MaxValue;

        #endregion

        #region Brightened Methods

        public static Color Brightened(this Color color, float amount, float alpha = 1f) =>
            ((int) (COLOR_DEPTH * amount))
                .Into(d => new Color(color.R + d, color.G + d, color.B + d, (int) (alpha * COLOR_DEPTH)));

        #endregion
    }
}