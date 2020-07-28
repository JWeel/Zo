using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zo.Data
{
    public class Cell
    {
        #region Constructors

        public Cell(int id, Rgba rgba, Vector2 center, Vector2 position, string name, int size,
            Color[] colorsByPixelIndex, Texture2D texture, Texture2D outlineTexture)
        {
            this.Id = id;
            this.Color = (Color) rgba;
            this.Rgba = rgba;
            this.Center = center;
            this.Position = position;
            this.Name = name;
            this.Size = size;
            this.ColorsByPixelIndex = colorsByPixelIndex;
            this.Texture = texture;
            this.OutlineTexture = outlineTexture;

            this.OriginalColor = this.Color;
        }

        #endregion

        #region Properties

        public int Id { get; }

        public Color Color { get; }

        public Rgba Rgba { get; }

        public Vector2 Center { get; }

        public Vector2 Position { get; }

        public string Name { get; }

        public int Size { get; }

        public Color[] ColorsByPixelIndex { get; }

        public Texture2D Texture { get; }

        public Texture2D OutlineTexture { get; }

        // ??
        protected Color OriginalColor { get; }

        public Cell[] Neighbours { get; protected set; }

        #endregion

        #region Methods

        public void SetNeighbours(Cell[] neighbours) =>
            this.Neighbours = neighbours;

        #endregion
    }
}