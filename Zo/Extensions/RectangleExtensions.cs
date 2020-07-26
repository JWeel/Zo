using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Zo.Extensions
{
    public static class RectangleExtensions
    {
        public static bool Contains(this Rectangle source, MouseState mouseState) =>
            source.Contains(mouseState.ToPoint());
    }
}