using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        #endregion

        #region Properties

        protected TextureRepository Texture { get; }

        public Dictionary<Rgba, Cell> Cells { get; protected set; }

        public Rgba[,] RootPixelGrid { get; protected set; }

        public RegionMapper[] GeographicalRegions { get; protected set; }

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

            var rgbaByPixelPosition = new Rgba[sourceTexture.Width, sourceTexture.Height];
            var (sourceColorsByPixelIndex, pixelPositionsByRgba) = this.MapPixelPositionsByRgba(sourceTexture);
            var regionByRgba = pixelPositionsByRgba
                .WithIndex()
                .Where(x => x.Value.Rgba != default) // shaves 10 sec off load time
                .ToDictionary(x => x.Value.Rgba, x =>
                {
                    var id = x.Key;
                    var (rgba, pixelPositions) = x.Value;

                    var (maxX, maxY, minX, minY, sumX, sumY) = pixelPositions
                        .Aggregate((MaxX: 0, MaxY: 0, MinX: int.MaxValue, MinY: int.MaxValue, SumX: 0, SumY: 0),
                            (aggregate, value) =>
                                (Largest(aggregate.MaxX, value.X), Largest(aggregate.MaxY, value.Y),
                                    Smallest(aggregate.MinX, value.X), Smallest(aggregate.MinY, value.Y),
                                        aggregate.SumX + (int) value.X, aggregate.SumY + (int) value.Y));

                    var centerX = sumX / pixelPositions.Length;
                    var centerY = sumY / pixelPositions.Length;

                    var width = maxX - minX + 1 ;
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

                    var texture = this.Texture.Create(width, height, colorsByPixelIndex);
                    var outlineTexture = this.Texture.Create(width, height, outlineColorsByPixelIndex);

                    var position = new Vector2(minX, minY);
                    var center = new Vector2(centerX, centerY);
                    return new Region(id, rgba.Value.ToString(), rgba, position, center,
                        colorsByPixelIndex, texture, outlineTexture);
                });

            return new RegionMapper(rgbaByPixelPosition, regionByRgba);
        }

        // protected Region[] ElevateGeographicalRegions(Texture2D sourceTexture, Region[] previousGeographicalRegions) =>
        //     this.MapPixelPositionsByRgbaValue(sourceTexture)
        //         .Where(x => (x.Rgba != default))
        //         .Select(tuple => tuple.PixelPositions
        //             .Select(position => this.RootPixelGrid.Item(position))
        //             .Distinct()
        //             .Select(rgba => previousGeographicalRegions.First(region => region.Rgba == rgba))
        //             .ToArray()
        //             .Into(regions => new Region(tuple.Rgba.Value.ToString(), tuple.Rgba, regions, this.Texture.Create)))
        //         .ToArray();

        // protected Region[] ElevateGeographicalRegions(Texture2D sourceTexture, Region[] previousGeographicalRegions) =>
        //     this.MapPixelPositionsByRgbaValue(sourceTexture)
        //         .Where(x => (x.Rgba != default))
        //         .Select(tuple => tuple.PixelPositions
        //             .Into(positions => previousGeographicalRegions
        //                 .Where(region => positions.Any(position => region.Contains(position))))
        //             .Distinct()
        //             .ToArray()
        //             .Into(regions => new Region(tuple.Rgba.Value.ToString(), tuple.Rgba, regions, this.Texture.Create)))
        //         .ToArray();

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

        #endregion
    }
}