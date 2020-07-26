using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Zo.Extensions;

namespace Zo.Data
{
    public class Region
    {
        #region Constructors

        // public Region(string name, Rgba rgba, Vector2 position, Texture2D texture, Func<int, int, Texture2D> textureCreator)
        // {
        //     this.Name = name;
        //     this.Color = (Color) rgba;
        //     this.OutlineColor = this.Color.Brightened(-0.18f, alpha: 0.4f);
        //     // reason for alpha is to blend with selection -> alternatively make selection transparent?
        //     this.Rgba = rgba;
        //     this.Position = position;
        //     this.Texture = texture;

        //     this.Cells = cell;
        // }

        public Region(string name, Rgba rgba, Cell[] cells, Func<int, int, Texture2D> textureCreator)
        {
            this.Name = name;
            this.Color = (Color) rgba;
            this.OutlineColor = this.Color.Brightened(-0.18f, alpha: 0.4f);
            // reason for alpha is to blend with selection -> alternatively make selection transparent?
            this.Rgba = rgba;
            this.Cells = cells;

            if (!cells.Any())
                return;
            var (position, width, height, textureData, outlineData) = this.MergeCellTextures(cells);
            this.Position = position;
            this.ColorsByPixelIndex = textureData; 
            this.Texture = textureCreator(width, height).WithSetData(textureData);
            this.OutlineTexture = textureCreator(width, height).WithSetData(outlineData);
        }

        public Region(string name, Rgba rgba, Region[] regions, Func<int, int, Texture2D> textureCreator)
        {
            this.Name = name;
            this.Color = (Color) rgba;
            this.OutlineColor = this.Color.Brightened(-0.18f, alpha: 0.4f);
            this.Rgba = rgba;

            this.Cells = regions.SelectMany(region => region.Cells).ToArray();

            // TODO share constructors
            var (position, width, height, textureData, outlineData) = this.MergeTextures(regions);
            this.Position = position;
            this.ColorsByPixelIndex = textureData; 
            this.Texture = textureCreator(width, height).WithSetData(textureData);
            this.OutlineTexture = textureCreator(width, height).WithSetData(outlineData);
        }

        #endregion

        #region Members

        public string Name { get; }

        public Color Color { get; }

        public Color OutlineColor { get; }

        public Rgba Rgba { get; }

        public Cell[] Cells { get; }

        public Vector2 Position { get; }

        public Color[] ColorsByPixelIndex { get; }

        public Texture2D Texture { get; }

        public Texture2D OutlineTexture { get; }

        #endregion

        #region Methods

        protected (Vector2 Position, int Width, int Height, Color[] TextureData, Color[] OutlineData) MergeCellTextures(Cell[] cells)
        {
            var (maxX, maxY, minX, minY) = this.CalculateRequiredTextureSize(cells);

            var width = maxX - minX;
            var height = maxY - minY;
            var position = new Vector2(minX, minY);

            var colors1D = this.MergeCellTexturesIntoColorArray(cells, width, height, position);
            var outlineColors1D = this.CalculateTextureOutline(colors1D, width, height);

            return (position, width, height, colors1D, outlineColors1D);
        }

        protected (Vector2 Position, int Width, int Height, Color[] TextureData, Color[] OutlineData) MergeTextures(Region[] regions)
        {
            var (maxX, maxY, minX, minY) = this.CalculateRequiredTextureSize(regions);

            var width = maxX - minX;
            var height = maxY - minY;
            var position = new Vector2(minX, minY);

            var colors1D = this.MergeTexturesIntoColorArray(regions, width, height, position);
            var outlineColors1D = this.CalculateTextureOutline(colors1D, width, height);

            return (position, width, height, colors1D, outlineColors1D);
        }

        protected (int MaxX, int MaxY, int MinX, int MinY) CalculateRequiredTextureSize(Cell[] cells)
        {
            int Smallest(int left, float right) =>
                (left > right) ? (int) right : left;
            int Largest(int left, float right) =>
                (left < right) ? (int) right : left;

            return cells
                .Select(cell => (cell.Position, cell.Texture.Width, cell.Texture.Height))
                .Aggregate((MaxX: 0, MaxY: 0, MinX: int.MaxValue, MinY: int.MaxValue), (aggregate, value) =>
                    (Largest(aggregate.MaxX, value.Position.X + value.Width), Largest(aggregate.MaxY, value.Position.Y + value.Height),
                        Smallest(aggregate.MinX, value.Position.X), Smallest(aggregate.MinY, value.Position.Y)));
        }

        protected (int MaxX, int MaxY, int MinX, int MinY) CalculateRequiredTextureSize(Region[] regions)
        {
            int Smallest(int left, float right) =>
                (left > right) ? (int) right : left;
            int Largest(int left, float right) =>
                (left < right) ? (int) right : left;

            return regions
                .Select(region => (region.Position, region.Texture.Width, region.Texture.Height))
                .Aggregate((MaxX: 0, MaxY: 0, MinX: int.MaxValue, MinY: int.MaxValue), (aggregate, value) =>
                    (Largest(aggregate.MaxX, value.Position.X + value.Width), Largest(aggregate.MaxY, value.Position.Y + value.Height),
                        Smallest(aggregate.MinX, value.Position.X), Smallest(aggregate.MinY, value.Position.Y)));
        }

        protected Color[] MergeCellTexturesIntoColorArray(Cell[] cells, int width, int height, Vector2 position)
        {
            var colors1D = new Color[width * height];
            var cellInfo = cells
                .Select(cell => (Offset: (cell.Position - position), cell.Texture.Width, cell.Texture.Height, Colors1D: cell.Texture.GetColorsByPixelIndex()))
                .ToArray();
            for (int index = 0; index < colors1D.Length; index++)
            {
                var x = index % width;
                var y = index / width;
                foreach (var cell in cellInfo)
                {
                    var cellX = x - (int) cell.Offset.X;
                    var cellY = y - (int) cell.Offset.Y;
                    if (cellX < 0) continue;
                    if (cellX >= cell.Width) continue;
                    if (cellY < 0) continue;
                    if (cellY >= cell.Height) continue;
                    var cellIndex = cellX + (cellY * cell.Width);
                    var cellColor = cell.Colors1D[cellIndex];
                    if (cellColor == default) continue;
                    colors1D[index] = Color.White;
                }
            }
            return colors1D;
        }

        protected Color[] MergeTexturesIntoColorArray(Region[] regions, int width, int height, Vector2 position)
        {
            var colors1D = new Color[width * height];
            var regionInfo = regions
                .Select(region => (Offset: (region.Position - position), region.Texture.Width, region.Texture.Height, Colors1D: region.ColorsByPixelIndex))
                .ToArray();
            for (int index = 0; index < colors1D.Length; index++)
            {
                var x = index % width;
                var y = index / width;
                foreach (var region in regionInfo)
                {
                    var regionX = x - (int) region.Offset.X;
                    var regionY = y - (int) region.Offset.Y;
                    if (regionX < 0) continue;
                    if (regionX >= region.Width) continue;
                    if (regionY < 0) continue;
                    if (regionY >= region.Height) continue;
                    var regionIndex = regionX + (regionY * region.Width);
                    var regionColor = region.Colors1D[regionIndex];
                    if (regionColor == default) continue;
                    colors1D[index] = Color.White;
                }
            }
            return colors1D;
        }

        protected Color[] CalculateTextureOutline(Color[] colors1D, int width, int height)
        {
            var outlineColors1D = new Color[width * height];
            for (int index = 0; index < colors1D.Length; index++)
            {
                var color = colors1D[index];
                if (color == default) continue;
                var x = index % width;
                var y = index / width;

                if (x > 0)
                {
                    int leftX = (x - 1) + (y * width);
                    var leftColor = colors1D[leftX];
                    if (leftColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
                if (x < width - 1)
                {
                    int rightX = (x + 1) + (y * width);
                    var rightColor = colors1D[rightX];
                    if (rightColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
                if (y > 0)
                {
                    int topY = x + ((y - 1) * width);
                    var topColor = colors1D[topY];
                    if (topColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
                if (y < height - 1)
                {
                    int bottomY = x + ((y + 1) * width);
                    var bottomColor = colors1D[bottomY];
                    if (bottomColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
            }

            return outlineColors1D;
        }

        #endregion
    }
}