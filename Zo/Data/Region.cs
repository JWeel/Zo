using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Zo.Extensions;
using System.Collections.Generic;

namespace Zo.Data
{
    public class Region
    {
        #region Constructors

        public Region(int id, string name, Rgba rgba, Vector2 position, Vector2 center, Color[] colorsByPixelIndex, Texture2D texture, Texture2D outlineTexture)
        {
            this.Id = id;
            this.Name = name;
            this.Color = (Color) rgba;
            this.OutlineColor = this.Color.Brightened(-0.18f, alpha: 0.4f);
            this.Rgba = rgba;
            this.Position = position;
            this.Center = center;
            this.Size = colorsByPixelIndex.Count(x => (x != default));
            this.ColorsByPixelIndex = colorsByPixelIndex;
            this.Texture = texture;
            this.OutlineTexture = outlineTexture;
        }

        // TODO move to factory, move merge methods outside
        public Region(string name, Rgba rgba, IEnumerable<Region> regions, Func<int, int, Texture2D> textureCreator)
        {
            this.Name = name;
            this.Color = (Color) rgba;
            this.OutlineColor = this.Color.Brightened(-0.18f, alpha: 0.4f);
            this.Rgba = rgba;

            var (position, center, width, height, textureData, outlineData) = this.MergeTextures(regions);
            this.Position = position;
            this.Center = center;
            this.Size = textureData.Count(x => (x != default));
            this.ColorsByPixelIndex = textureData;
            this.Texture = textureCreator(width, height).WithSetData(textureData);
            this.OutlineTexture = textureCreator(width, height).WithSetData(outlineData);
        }

        // TODO move to factory, move merge methods outside
        public Region(string name, Rgba rgba, Region exclusionRegion, Region sourceRegion, Func<int, int, Texture2D> textureCreator)
        {
            this.Name = name;
            this.Color = (Color) rgba;
            this.OutlineColor = this.Color.Brightened(-0.18f, alpha: 0.4f);
            this.Rgba = rgba;

            var colorsByPixelIndex = this.GetColorsByPixelIndexWithoutRegion(sourceRegion, exclusionRegion);
            var outlineColorsByPixelIndex = this.CalculateTextureOutline(colorsByPixelIndex, sourceRegion.Texture.Width, sourceRegion.Texture.Height);
            this.Position = sourceRegion.Position;
            this.Center = sourceRegion.Center;
            this.Size = colorsByPixelIndex.Count(x => (x != default));
            this.ColorsByPixelIndex = colorsByPixelIndex;
            this.Texture = textureCreator(sourceRegion.Texture.Width, sourceRegion.Texture.Height).WithSetData(colorsByPixelIndex);
            this.OutlineTexture = textureCreator(sourceRegion.Texture.Width, sourceRegion.Texture.Height).WithSetData(outlineColorsByPixelIndex);
        }

        #endregion

        #region Members

        public int Id { get; }

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

        #endregion

        #region Methods

        // no one using this anymore
        public bool Contains(Vector2 position) =>
            position.LiesWithin(this.Position, this.Texture.Width, this.Texture.Height) &&
                (this.ColorsByPixelIndex.Item(position - this.Position, this.Texture.Width) != default);

        protected (Vector2 Position, Vector2 Center, int Width, int Height, Color[] TextureData, Color[] OutlineData) MergeTextures(IEnumerable<Region> regionEnumerable)
        {
            var regions = regionEnumerable.ToArray();
            var (maxX, maxY, minX, minY, sumX, sumY) = this.CalculateRequiredTextureSize(regions);

            var width = maxX - minX;
            var height = maxY - minY;
            var position = new Vector2(minX, minY);
            var center = new Vector2(sumX / regions.Length, sumY / regions.Length);

            var colorsByPixelIndex = this.MergeTexturesIntoColorArray(regions, width, height, position);
            var outlineColors1D = this.CalculateTextureOutline(colorsByPixelIndex, width, height);

            return (position, center, width, height, colorsByPixelIndex, outlineColors1D);
        }

        protected (int MaxX, int MaxY, int MinX, int MinY, int SumX, int SumY) CalculateRequiredTextureSize(Region[] regions)
        {
            int Smallest(int left, float right) =>
                (left > right) ? (int) right : left;
            int Largest(int left, float right) =>
                (left < right) ? (int) right : left;

            return regions
                .Select(region => (region.Position, region.Texture.Width, region.Texture.Height))
                .Aggregate((MaxX: 0, MaxY: 0, MinX: int.MaxValue, MinY: int.MaxValue, SumX: 0, SumY: 0), (aggregate, value) =>
                    (Largest(aggregate.MaxX, value.Position.X + value.Width), Largest(aggregate.MaxY, value.Position.Y + value.Height),
                        Smallest(aggregate.MinX, value.Position.X), Smallest(aggregate.MinY, value.Position.Y),
                            aggregate.SumX + (int) value.Position.X, aggregate.SumY + (int) value.Position.Y));
        }

        protected Color[] MergeTexturesIntoColorArray(Region[] regions, int width, int height, Vector2 position)
        {
            var colorsByPixelIndex = new Color[width * height];
            var regionInfo = regions
                .Select(region => (Offset: (region.Position - position), region.Texture.Width, region.Texture.Height, Colors1D: region.ColorsByPixelIndex))
                .ToArray();
            for (int index = 0; index < colorsByPixelIndex.Length; index++)
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
                    colorsByPixelIndex[index] = Color.White;
                }
            }
            return colorsByPixelIndex;
        }

        // TODO: remove -> it is duplicated in Map.cs
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

        protected Color[] GetColorsByPixelIndexWithoutRegion(Region sourceRegion, Region exclusionRegion)
        {
            var withoutColorsByPixelIndex = new Color[sourceRegion.ColorsByPixelIndex.Length];
            sourceRegion.ColorsByPixelIndex.CopyTo(withoutColorsByPixelIndex, index: 0);

            var relativePosition = sourceRegion.Position - exclusionRegion.Position;
            for (int index = 0; index < exclusionRegion.ColorsByPixelIndex.Length; index++)
            {
                if (exclusionRegion.ColorsByPixelIndex[index] != default)
                {
                    var x = index % exclusionRegion.Texture.Width;
                    var y = index / exclusionRegion.Texture.Width;
                    var position = new Vector2(x, y);
                    var positionOnSource = position - relativePosition;
                    if (!positionOnSource.LiesWithin(sourceRegion.Texture.Width, sourceRegion.Texture.Height)) continue;
                    
                    var sourceIndex = (int) positionOnSource.X + (int) positionOnSource.Y * sourceRegion.Texture.Width;
                    // if ((sourceIndex < 0) || (sourceIndex >= withoutColorsByPixelIndex.Length)) continue;
                    withoutColorsByPixelIndex[sourceIndex] = default;
                }
            }
            return withoutColorsByPixelIndex;
        }

        #endregion
    }
}