using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zo.Extensions
{
    public static class SpriteBatchExtensions
    {
        #region Constants
            
        private static readonly Vector2 V0 = new Vector2(0);
            
        #endregion

        #region Draw Methods
            
        public static void DrawAt(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float scale, Color? color = default, float depth = 0f) =>
            spriteBatch.Draw(
                texture: texture,
                position: position,
                sourceRectangle: null,
                color: color ?? Color.White,
                rotation: 0f,
                origin: V0,
                scale: scale,
                effects: SpriteEffects.None,
                layerDepth: depth
            );
            
        public static void DrawTo(this SpriteBatch spriteBatch, Texture2D texture, Rectangle destinationRectangle, Color? color = default, float depth = 0f) =>
            spriteBatch.Draw(
                texture: texture,
                destinationRectangle: destinationRectangle,
                sourceRectangle: null,
                color: color ?? Color.White,
                rotation: 0f,
                origin: V0,
                effects: SpriteEffects.None,
                layerDepth: depth
            );

        #endregion
    }
}