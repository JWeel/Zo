using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Zo.Data
{
    public class Cell
    {
        #region Constructors

        public Cell(int id, Rgba rgba, Vector2 center, Vector2 position, string name, int size, Texture2D texture)
        {
            this.Id = id;
            this.Color = (Color) rgba;
            this.Rgba = rgba;
            this.Center = center;
            this.Position = position;
            this.Name = name;
            this.Size = size;
            this.Texture = texture;
            
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

        public Texture2D Texture { get; }

        protected Color OriginalColor { get; }

        public Cell[] Neighbours { get; protected set; }

        #endregion

        #region Methods

        public void SetNeighbours(Cell[] neighbours) =>
            this.Neighbours = neighbours;

        #endregion
    }
}