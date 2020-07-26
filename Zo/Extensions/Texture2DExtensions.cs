using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zo.Extensions
{
    public static class Texture2DExtensions
    {
        #region GetData Methods

        public static Color[] GetColorsByPixelIndex(this Texture2D texture)
        {
            var colors1D = new Color[texture.Width * texture.Height];
            texture.GetData(colors1D);
            return colors1D;
        }

        #endregion

        #region SetData Methods

        public static Texture2D WithSetData(this Texture2D texture, Color[] colorsByPixelIndex) =>
            texture.With(x => x.SetData(colorsByPixelIndex));

        #endregion
    }
}