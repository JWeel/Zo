using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Zo.Enums;
using Zo.Extensions;
using Zo.Repositories;

namespace Zo.Data
{
    public class Map
    {
        #region Constructors

        public Map(TextureRepository texture)
        {
            this.Texture = texture;
            this.Fiefs = new List<Fief>();
        }

        #endregion

        #region Properties

        protected TextureRepository Texture { get; }

        public Dictionary<Rgba, Cell> Cells { get; protected set; }

        public Rgba[,] RootPixelGrid { get; protected set; }

        public RegionMapper[] GeographicalRegions { get; protected set; }

        // dunno if right spot
        public List<Fief> Fiefs { get; }

        #endregion

        #region Public Methods

        public void LoadContent(ContentManager content)
        {
            this.DefineGeographicalRoot(this.Texture.GeographicalRegion0);
            this.DefineGeographicalRegions(this.Texture.GeographicalRegions);
        }

        #endregion

        #region Protected Methods

        protected void DefineGeographicalRegions(Texture2D[] sourceTextures) =>
            this.GeographicalRegions = sourceTextures
                .Select(this.MapGeographicalRegions)
                .ToArray();

        protected RegionMapper MapGeographicalRegions(Texture2D sourceTexture)
        {
            int Smallest(float left, float right) =>
                (int) ((left > right) ? right : left);
            int Largest(float left, float right) =>
                (int) ((left < right) ? right : left);

            var prefix = ((Division) char.GetNumericValue(sourceTexture.Name.Last())).ToString().Substring(0, 1).ToLower();
            var rgbaByPixelPosition = new Rgba[sourceTexture.Width, sourceTexture.Height];
            var (sourceColorsByPixelIndex, pixelPositionsByRgba) = this.MapPixelPositionsByRgba(sourceTexture);
            var regionByRgba = pixelPositionsByRgba
                .WithIndex()
                .Where(x => x.Value.Rgba != default) // shaves 10 sec off load time
                .ToDictionary(x => x.Value.Rgba, x =>
                {
                    var id = $"{prefix}-{x.Key}";
                    var (rgba, pixelPositions) = x.Value;

                    var (maxX, maxY, minX, minY, sumX, sumY) = pixelPositions
                        .Aggregate((MaxX: 0, MaxY: 0, MinX: int.MaxValue, MinY: int.MaxValue, SumX: 0, SumY: 0),
                            (aggregate, value) =>
                                (Largest(aggregate.MaxX, value.X), Largest(aggregate.MaxY, value.Y),
                                    Smallest(aggregate.MinX, value.X), Smallest(aggregate.MinY, value.Y),
                                        aggregate.SumX + (int) value.X, aggregate.SumY + (int) value.Y));

                    var centerX = sumX / pixelPositions.Length;
                    var centerY = sumY / pixelPositions.Length;

                    var width = maxX - minX + 1;
                    var height = maxY - minY + 1;
                    var colorsByPixelIndex = new Color[width * height];
                    for (int i = minX; i <= maxX; i++)
                    {
                        for (int j = minY; j <= maxY; j++)
                        {
                            var color = sourceColorsByPixelIndex.Item(new Vector2(i, j), sourceTexture.Width);
                            if (color == (Color) rgba)
                            {
                                int index = (i - minX) + ((j - minY) * width);
                                colorsByPixelIndex[index] = Color.White;
                                rgbaByPixelPosition[i, j] = rgba;
                            }
                        }
                    }
                    var outlineColorsByPixelIndex = this.CalculateOutline(colorsByPixelIndex, width, height);
                    var combinedColorsByPixelIndex = this.CombineOutline(colorsByPixelIndex, outlineColorsByPixelIndex);

                    var texture = this.Texture.Create(width, height, colorsByPixelIndex);
                    var outlineTexture = this.Texture.Create(width, height, outlineColorsByPixelIndex);
                    var combinedTexture = this.Texture.Create(width, height).WithSetData(combinedColorsByPixelIndex);

                    var position = new Vector2(minX, minY);
                    var center = new Vector2(centerX, centerY);
                    return new Region(id, rgba.Value.ToString(), rgba, position, center,
                        colorsByPixelIndex, texture, outlineTexture, combinedTexture);
                });

            return new RegionMapper(rgbaByPixelPosition, regionByRgba);
        }

        protected Color[] CombineOutline(Color[] colorsByPixelIndex, Color[] outlineColorsByPixelIndex)
        {
            var combinedColorsByPixelIndex = new Color[colorsByPixelIndex.Length];
            colorsByPixelIndex.CopyTo(combinedColorsByPixelIndex, index: 0);

            for (var i = 0; i < colorsByPixelIndex.Length; i++)
            {
                if (outlineColorsByPixelIndex[i] != default)
                    combinedColorsByPixelIndex[i] = new Color(219, 219, 219, 255);
                else if (colorsByPixelIndex[i] != default)
                    combinedColorsByPixelIndex[i] = Color.White;
            }
            return combinedColorsByPixelIndex;
        }

        protected void DefineGeographicalRoot(Texture2D sourceTexture)
        {
            var (colorsByPixelIndex, pixelPositionsByRgbaValues) = this.MapPixelPositionsByRgba(sourceTexture);

            var cells = this.MapCellsByRgba(pixelPositionsByRgbaValues);

            // for Cell (geographical0 only)
            this.AssignRootCellData(colorsByPixelIndex, sourceTexture.Width, sourceTexture.Height, pixelPositionsByRgbaValues,
                out var localPixelGrid, out var neighboursByRgba);
            var emptyNeighboours = new HashSet<Rgba>();
            cells.Values
                .Each(cell => cell
                    .SetNeighbours(neighboursByRgba
                        .GetValueOrDefault(cell.Rgba, emptyNeighboours)
                        .Select(rgba => cells[rgba])
                        .ToArray()));
            this.Cells = cells;
            this.RootPixelGrid = localPixelGrid;
        }

        protected (Color[] colorsByPixelIndex, (Rgba Rgba, Vector2[] PixelPositions)[] PixelPositionsByRgba) MapPixelPositionsByRgba(Texture2D texture) =>
            texture
                .GetColorsByPixelIndex()
                .Into(colorsByPixelIndex => colorsByPixelIndex
                    .Select((color, index) => (Color: color, X: index % texture.Width, Y: index / texture.Width))
                    .Select(tuple => (Rgba: new Rgba(tuple.Color), Position: new Vector2(tuple.X, tuple.Y)))
                    .GroupBy(tuple => tuple.Rgba)
                    .Select(group => (Rgba: group.Key, PixelPositions: group.Select(x => x.Position).ToArray()))
                    .ToArray()
                    .Into(pixelPositionsByRgba => (colorsByPixelIndex, pixelPositionsByRgba)));

        protected void AssignRootCellData(Color[] pixelColors, int width, int height,
            (Rgba Rgba, Vector2[] PixelPositions)[] pixelPositionsByRgbaValues,
            out Rgba[,] pixelGrid, out Dictionary<Rgba, HashSet<Rgba>> neighboursByRgba)
        {
            var localPixelGrid = new Rgba[width, height];
            var localNeighboursByRgba = pixelPositionsByRgbaValues.ToDictionary(x => x.Rgba, _ => new HashSet<Rgba>());
            pixelPositionsByRgbaValues
                .Each(pair => pair.PixelPositions
                    .Each(position =>
                    {
                        var x = (int) position.X;
                        var y = (int) position.Y;
                        localPixelGrid[x, y] = pair.Rgba;

                        CheckForNeighbourIf((x > 0), pair.Rgba, () => new Rgba(pixelColors[x - 1 + y * width]));
                        CheckForNeighbourIf((x < width - 1), pair.Rgba, () => new Rgba(pixelColors[x + 1 + y * width]));
                        CheckForNeighbourIf((y > 0), pair.Rgba, () => new Rgba(pixelColors[x + (y - 1) * width]));
                        CheckForNeighbourIf((y > height - 1), pair.Rgba, () => new Rgba(pixelColors[x + (y + 1) * width]));
                    }));

            neighboursByRgba = localNeighboursByRgba;
            pixelGrid = localPixelGrid;

            void CheckForNeighbourIf(bool condition, Rgba rgba, Func<Rgba> neighbourRgbaGetter)
            {
                if (!condition) return;
                var neighbour = neighbourRgbaGetter();
                if ((neighbour != default(Rgba)) && (neighbour != rgba))
                    localNeighboursByRgba[rgba].Add(neighbour);
            }
        }

        protected Dictionary<Rgba, Cell> MapCellsByRgba((Rgba Rgba, Vector2[] PixelPositions)[] pixelPositionsByRgbaValues) =>
            pixelPositionsByRgbaValues
                .Where(x => (x.Rgba != default))
                .WithIndex()
                .ToDictionary(x => x.Value.Rgba, x =>
                {
                    var id = x.Key;
                    var pair = x.Value;

                    var pixelPositions = pair.PixelPositions;
                    var centerX = pixelPositions.Sum(xy => xy.X) / pixelPositions.Length;
                    var centerY = pixelPositions.Sum(xy => xy.Y) / pixelPositions.Length;

                    var leftmost = (int) pixelPositions.Min(xy => xy.X);
                    var topmost = (int) pixelPositions.Min(xy => xy.Y);
                    var rightmost = (int) pixelPositions.Max(xy => xy.X);
                    var bottommost = (int) pixelPositions.Max(xy => xy.Y);

                    var width = rightmost - leftmost + 1;
                    var height = bottommost - topmost + 1;
                    var colorsByPixelIndex = new Color[width * height];
                    for (int i = leftmost; i < leftmost + width; i++)
                    {
                        for (int j = topmost; j < topmost + height; j++)
                        {
                            int index = (i - leftmost) + ((j - topmost) * width);

                            if (pixelPositions.Any(xy => xy.X == i && xy.Y == j))
                                colorsByPixelIndex[index] = Color.White;
                        }
                    }
                    var outlineColorsByPixelIndex = this.CalculateOutline(colorsByPixelIndex, width, height);

                    var texture = this.Texture.Create(width, height, colorsByPixelIndex);
                    var outlineTexture = this.Texture.Create(width, height, outlineColorsByPixelIndex);

                    return new Cell(id, pair.Rgba, new Vector2(centerX, centerY), new Vector2(leftmost, topmost),
                        pair.Rgba.Value.ToString(), pixelPositions.Length, colorsByPixelIndex, texture, outlineTexture); //, neighboursByCell[rgba]);
                });

        protected Color[] CalculateOutline(Color[] colorsByPixelIndex, int width, int height)
        {
            var outlineColors1D = new Color[width * height];
            for (int index = 0; index < colorsByPixelIndex.Length; index++)
            {
                var color = colorsByPixelIndex[index];
                if (color == default) continue;
                var x = index % width;
                var y = index / width;

                if (x > 0)
                {
                    int leftX = (x - 1) + (y * width);
                    var leftColor = colorsByPixelIndex[leftX];
                    if (leftColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;

                if (x < width - 1)
                {
                    int rightX = (x + 1) + (y * width);
                    var rightColor = colorsByPixelIndex[rightX];
                    if (rightColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;

                if (y > 0)
                {
                    int topY = x + ((y - 1) * width);
                    var topColor = colorsByPixelIndex[topY];
                    if (topColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;

                if (y < height - 1)
                {
                    int bottomY = x + ((y + 1) * width);
                    var bottomColor = colorsByPixelIndex[bottomY];
                    if (bottomColor != color)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
            }
            return outlineColors1D;
        }

        public Region CombineRegions(string name, Rgba rgba, IEnumerable<Region> regions)
        {
            var id = "n/a";
            var (position, center, width, height, colorsByPixelIndex, outlineColorsByPixelIndex) = this.MergeTextures(regions);
            var combinedColorsByPixelIndex = this.CombineOutline(colorsByPixelIndex, outlineColorsByPixelIndex);

            var texture = this.Texture.Create(width, height).WithSetData(colorsByPixelIndex);
            var outlineTexture = this.Texture.Create(width, height).WithSetData(outlineColorsByPixelIndex);
            var combinedTexture = this.Texture.Create(width, height).WithSetData(combinedColorsByPixelIndex);

            return new Region(id, name, rgba, position, center, colorsByPixelIndex, texture, outlineTexture, combinedTexture);
        }

        public Region DisjoinRegions(string name, Rgba rgba, Region exclusionRegion, Region sourceRegion)
        {
            var id = "n/a";
            var colorsByPixelIndex = this.GetColorsByPixelIndexWithoutRegion(sourceRegion, exclusionRegion);
            var outlineColorsByPixelIndex = this.CalculateTextureOutline(colorsByPixelIndex, sourceRegion.Texture.Width, sourceRegion.Texture.Height);
            var combinedColorsByPixelIndex = this.CombineOutline(colorsByPixelIndex, outlineColorsByPixelIndex);
            var position = sourceRegion.Position;
            var center = sourceRegion.Center;

            var texture = this.Texture.Create(sourceRegion.Texture.Width, sourceRegion.Texture.Height).WithSetData(colorsByPixelIndex);
            var outlineTexture = this.Texture.Create(sourceRegion.Texture.Width, sourceRegion.Texture.Height).WithSetData(outlineColorsByPixelIndex);
            var combinedTexture = this.Texture.Create(sourceRegion.Texture.Width, sourceRegion.Texture.Height).WithSetData(combinedColorsByPixelIndex);

            return new Region(id, name, rgba, position, center, colorsByPixelIndex, texture, outlineTexture, combinedTexture);
        }

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
                    if (leftColor == default)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
                if (x < width - 1)
                {
                    int rightX = (x + 1) + (y * width);
                    var rightColor = colors1D[rightX];
                    if (rightColor == default)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
                if (y > 0)
                {
                    int topY = x + ((y - 1) * width);
                    var topColor = colors1D[topY];
                    if (topColor == default)
                        outlineColors1D[index] = Color.White;
                }
                else
                    outlineColors1D[index] = Color.White;
                if (y < height - 1)
                {
                    int bottomY = x + ((y + 1) * width);
                    var bottomColor = colors1D[bottomY];
                    if (bottomColor == default)
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
                    withoutColorsByPixelIndex[sourceIndex] = default;
                }
            }
            return withoutColorsByPixelIndex;
        }

        #endregion
    }
}