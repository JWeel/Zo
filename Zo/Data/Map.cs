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

        public Region[][] GeographicalRegions { get; protected set; }

        #endregion

        #region Public Methods

        public void LoadContent(ContentManager content)
        {
            this.DefineGeographicalRoot(this.Texture.GeographicalRegion0);
            this.DefineGeographicalRegions(this.Texture.GeographicalRegions);
            // TODO ^ change to use region instead of cell
            // make first one copy everything from root
            // then all after copy everything from index-1
            // then we can get rid of region constructor taking cells
            // then get rid of cells property if come up with way to map to region

            // var stateColor = new Rgba("00A0A0");
            // States = new Dictionary<Dye, State>();
            // States[stateColor] = new State(stateColor, "Yes", new List<Cell> { Cells[new Dye("E6DC64")], Cells[new Dye("D6907F")], Cells[new Dye("857DD5")] });
            // //States[stateColor] = new State(stateColor, "Yes", new List<Cell> { Cells[new Dye("AD73A7")], Cells[new Dye("97AED8")], Cells[new Dye("6C8D7C")], Cells[new Dye("696694")], Cells[new Dye("90B7E1")] });
            // //States[stateColor] = new State(stateColor, "Yes", Cells.Values.ToList() );
            // States[stateColor].Cells.Remove(Cells[default]);
        }

        #endregion

        #region Protected Methods

        protected void DefineGeographicalRegions(Texture2D[] sourceTextures)
        {
            var enumerator = sourceTextures.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            this.GeographicalRegions = new Region[sourceTextures.Length][];

            var index = 0;
            this.GeographicalRegions[index++] = this.Cells.Values
                .Select(cell => new Region(cell.Name, cell.Rgba, cell.IntoArray(), this.Texture.Create))
                .ToArray();

            while (enumerator.MoveNext())
                this.GeographicalRegions[index++] = this.GetGeographicalRegion((Texture2D) enumerator.Current);

            // this.GeographicalRegions = sourceTextures
            //     .Select(this.GetGeographicalRegion)
            //     .ToArray();
        }

        protected Region[] GetGeographicalRegion(Texture2D sourceTexture) =>
            this.MapPixelPositionsByRgbaValue(sourceTexture)
                .Where(x => (x.Rgba != default))
                .Select(tuple => tuple.PixelPositions
                    .Select(position => this.RootPixelGrid.Item(position))
                    .Distinct()
                    .Select(rgba => this.Cells[rgba])
                    .ToArray()
                    .Into(cells => new Region(tuple.Rgba.Value.ToString(), tuple.Rgba, cells, this.Texture.Create)))
                .ToArray();

        protected void DefineGeographicalRoot(Texture2D sourceTexture)
        {
            var colors1D = sourceTexture.GetColorsByPixelIndex();
            var pixelPositionsByRgbaValues = this.MapPixelPositionsByRgbaValue(colors1D, sourceTexture.Width);

            var cells = this.MapCellsByRgba(pixelPositionsByRgbaValues);

            // for Cell (geographical0 only)
            this.AssignRootCellData(colors1D, sourceTexture.Width, sourceTexture.Height, pixelPositionsByRgbaValues,
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

        protected (Rgba Rgba, Vector2[] PixelPositions)[] MapPixelPositionsByRgbaValue(Texture2D texture) =>
            texture.GetColorsByPixelIndex()
                .Into(colors1D => this.MapPixelPositionsByRgbaValue(colors1D, texture.Width));

        protected (Rgba Rgba, Vector2[] PixelPositions)[] MapPixelPositionsByRgbaValue(Color[] pixelColors, int divisor) =>
            pixelColors
                .Select((color, index) => (Color: color, X: index % divisor, Y: index / divisor))
                .Select(tuple => (Rgba: new Rgba(tuple.Color), Position: new Vector2(tuple.X, tuple.Y)))
                .GroupBy(tuple => tuple.Rgba)
                .Select(group => (Rgba: group.Key, PixelPositions: group.Select(x => x.Position).ToArray()))
                .ToArray();

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
                    var cell1D = new Color[width * height];
                    for (int i = leftmost; i < leftmost + width; i++)
                    {
                        for (int j = topmost; j < topmost + height; j++)
                        {
                            int index = (i - leftmost) + ((j - topmost) * width);

                            if (pixelPositions.Any(xy => xy.X == i && xy.Y == j))
                                cell1D[index] = Color.White;
                        }
                    }
                    var texture = this.Texture.Create(width, height);
                    texture.SetData(cell1D);

                    return new Cell(id, pair.Rgba, new Vector2(centerX, centerY), new Vector2(leftmost, topmost),
                        pair.Rgba.Value.ToString(), pixelPositions.Length, texture); //, neighboursByCell[rgba]);
                });

        #endregion
    }
}