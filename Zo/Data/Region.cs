using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Zo.Extensions;

namespace Zo.Data
{
    public class Region
    {
        #region

        private const float DEFAULT_OUTLINE_MODIFIER = -0.14f;
        private const float DEFAULT_OUTLINE_ALPHA = 0.5f;

        #endregion

        #region Constructors

        public Region(string id, string name, Rgba rgba, Vector2 position, Vector2 center, Color[] colorsByPixelIndex, Texture2D texture, Texture2D outlineTexture, Texture2D combinedTexture)
        {
            this.HasValue = true;
            this.Id = id;
            this.Name = name;
            this.Color = (Color) rgba;
            this.OutlineColor = this.Color.Brightened(DEFAULT_OUTLINE_MODIFIER, alpha: DEFAULT_OUTLINE_ALPHA);
            this.Rgba = rgba;
            this.Position = position;
            this.Center = center;
            this.Size = colorsByPixelIndex.Count(x => (x != default));
            this.ColorsByPixelIndex = colorsByPixelIndex;
            this.Texture = texture;
            this.OutlineTexture = outlineTexture;
            this.CombinedTexture = combinedTexture;
        }

        #endregion

        #region Members

        // useful if changing type from class to struct
        public bool HasValue { get; }

        public string Id { get; }

        public string Name { get; }

        public Color Color { get; }

        public Color OutlineColor { get; }

        public Rgba Rgba { get; }

        public Vector2 Position { get; }

        public Vector2 Center { get; }

        public int Size { get; }

        public Color[] ColorsByPixelIndex { get; }

        public Texture2D Texture { get; }

        public Texture2D OutlineTexture { get; }

        public Texture2D CombinedTexture { get; }

        #endregion

        #region Methods

        // no one using this anymore
        public bool Contains(Vector2 position) =>
            position.LiesWithin(this.Position, this.Texture.Width, this.Texture.Height) &&
                (this.ColorsByPixelIndex.Item(position - this.Position, this.Texture.Width) != default);

        public bool Contains(Region region)
        {
            var thisRectangle = this.GetBounds();
            var thatRectangle = region.GetBounds();
            if (!thisRectangle.Intersects(thatRectangle))
                return false;

            var offset = this.Position - region.Position;
            for (var index = 0; index < this.ColorsByPixelIndex.Length; index++)
            {
                if (this.ColorsByPixelIndex[index] == default) continue;

                var x = index % this.Texture.Width;
                var y = index / this.Texture.Width;

                var otherX = x + (int) offset.X;
                var otherY = y + (int) offset.Y;
                if ((otherX < 0) || (otherX >= region.Texture.Width) 
                    || (otherY < 0) || (otherY >= region.Texture.Height))
                    continue;
                var otherIndex = otherX + (otherY * region.Texture.Width);

                if (region.ColorsByPixelIndex[otherIndex] != default)
                    return true;
            }
            return false;
        }

        protected Rectangle GetBounds() =>
            new Rectangle((int) this.Position.X, (int) this.Position.Y, this.Texture.Width, this.Texture.Height);

        #endregion
    }
}