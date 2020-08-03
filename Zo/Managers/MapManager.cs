using System.Xml.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Zo.Data;
using Zo.Enums;
using Zo.Extensions;
using Zo.Repositories;
using Zo.Types;

namespace Zo.Managers
{
    public class MapManager
    {
        #region Constants

        public const float MAX_MAP_ZOOM = 5f;
        public const float MIN_MAP_ZOOM = 1f;
        public const float BASE_MAP_ZOOM = 1.5f;
        public const float BASE_ZOOM_IN_AMOUNT = 0.25f;
        public const float BASE_ZOOM_OUT_AMOUNT = -0.25f;

        #endregion

        #region Constructors

        public MapManager(SizeManager sizes, TextureRepository texture, Action<Action> subscribeToUpdate)
        {
            subscribeToUpdate(this.UpdateState);
            sizes.OnCalculating += this.HandleSizesCalculating;
            sizes.OnCalculated += this.HandleSizesCalculated;

            this.Sizes = sizes;
            this.Texture = texture;

            this.Scale = BASE_MAP_ZOOM;
            this.VisibleBorder = true;

            this.Position = new Vector2(-100, -100);

            this.Map = new Map(this.Texture);

            this.MapType = new Cycle<MapType>(default(MapType).GetValues());
            this.Division = new Axis<Division>(default(Division).GetValues());
        }

        #endregion

        #region Properties

        public event Action OnSelected;

        public SizeManager Sizes { get; }

        public TextureRepository Texture { get; }

        public float Scale { get; set; }

        protected Vector2 LastMovePosition { get; set; }

        public Vector2 ActualPosition { get; protected set; }

        private Vector2 _position;
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                this.ActualPosition = _position + this.Sizes.BorderSizeVector; // bordersize does not have to be scaled by map scale because it is not on the map

                this.ViewCenter = new Vector2(
                    x: (this.Sizes.ActualMapWidth / 2),
                    y: (this.Sizes.ActualMapHeight / 2));

                this.ViewMapCenter = this.ViewCenter - this.ActualPosition;

                var size = 3;
                this.ViewCenterRectangle = new Rectangle(
                    (this.ViewCenter - new Vector2(size / 2) + this.Sizes.BorderSizeVector).ToPoint(),
                    new Point(size));
            }
        }

        public Vector2 ViewCenter { get; protected set; }
        public Rectangle ViewCenterRectangle { get; protected set; }

        public Vector2 ViewMapCenter { get; protected set; }

        public bool IsMoving { get; protected set; }

        // outer map border
        public bool VisibleBorder { get; set; }

        public bool VisibleLabel { get; set; }

        protected Map Map { get; set; }

        public Region SelectedRegion { get; protected set; }

        public Region LastSelectedRegion { get; protected set; }

        protected Vector2 LastRelativePosition { get; set; }

        protected Cycle<MapType> MapType { get; }

        public Axis<Division> Division { get; }

        #endregion

        #region Public Methods

        public void LoadContent(ContentManager content)
        {
            this.Map.LoadContent(content);
        }

        public string Debug { get; protected set; } = string.Empty;

        public void Zoom(float amount) => this.Zoom(amount, this.ViewCenter);

        /// <summary> Zooms the world centered around coordinates. </summary>
        public void Zoom(float amount, Vector2 zoomOrigin)
        {
            var previousScale = this.Scale;
            this.ChangeProperty(x => x.Scale, prop => prop.AddWithLimits(amount, MIN_MAP_ZOOM, MAX_MAP_ZOOM));
            if (previousScale == this.Scale)
                return;

            var previousScaleMapSizes = this.Sizes.ActualMapSize / previousScale;
            var newScaleMapSizes = this.Sizes.ActualMapSize / this.Scale;
            var difference = previousScaleMapSizes - newScaleMapSizes;

            // the / 2 is because we are moving half of the difference in pixels to adjust for where is being zoomed
            // honestly no idea why we need to also do x scale x previousscale
            var adjustedDifference = difference / 2 * this.Scale * previousScale;

            var zoomOffsetFromCenter = zoomOrigin - this.ViewCenter;
            var ratio = zoomOffsetFromCenter / this.Sizes.ActualMapSize;

            // since ratio is below/above 0 we need to add 1 in order to multiple by it
            var adjustedScaledRatio = ratio * this.Scale + new Vector2(1);

            // finally we need to multiply by -1 because the map position moves in negative direction
            var offset = -adjustedDifference * adjustedScaledRatio;

            this.Debug = string.Empty;
            // this.Debug += $"{this.ActualPosition.PrintInt()}{Environment.NewLine}";
            // this.Debug += $"{previousScaleMapSizes.PrintInt()}{Environment.NewLine}";
            // this.Debug += $"{newScaleMapSizes.PrintInt()}{Environment.NewLine}";
            // this.Debug += $"{difference.PrintInt()}{Environment.NewLine}";
            // this.Debug += $"{adjustedDifference.PrintInt()}{Environment.NewLine}";
            // this.Debug += $"{zoomOffsetFromCenter.PrintInt()}{Environment.NewLine}";
            // this.Debug += $"{ratio.PrintFloat()}{Environment.NewLine}";
            // this.Debug += $"{adjustedScaledRatio.PrintFloat()}{Environment.NewLine}";
            // this.Debug += $"{offset.PrintInt()}{Environment.NewLine}";
            this.Move(offset);
        }

        public void StartMoving(Vector2 movePosition)
        {
            this.IsMoving = true;
            this.LastMovePosition = movePosition;
        }

        public void StopMoving()
        {
            this.IsMoving = false;
        }

        public void MoveByPosition(Vector2 movePosition)
        {
            this.Move(movePosition - this.LastMovePosition);
            this.LastMovePosition = movePosition;
        }

        public void Move(Vector2 movement) =>
            this.Move(movement.X, movement.Y);

        public void Move(float movementX, float movementY) =>
            this.Move((int) movementX, (int) movementY);

        public void Move(int movementX, int movementY)
        {
            var minX = (int) (this.Sizes.ActualMapWidth * this.Scale) - this.Sizes.ActualMapWidth;
            var minY = (int) (this.Sizes.ActualMapHeight * this.Scale) - this.Sizes.ActualMapHeight;

            var newX = this.Position.X.AddWithLimits(movementX, -minX, 0);
            var newY = this.Position.Y.AddWithLimits(movementY, -minY, 0);
            this.Position = new Vector2(newX, newY);
        }

        public Action<Action> SubscribeToSelect =>
            action => this.OnSelected += action;

        public void SelectGeographicalRegion(Vector2 position)
        {
            var regionMapper = this.Map.GeographicalRegions[this.Division.Index];
            var rgba = regionMapper.RgbaByPixelPosition.Item(position);
            if (rgba != default)
            {
                var mappedRegion = regionMapper.RegionByRgba[rgba];
                this.LastSelectedRegion = mappedRegion;
                if (this.SelectedRegion == default)
                    this.SelectedRegion = mappedRegion;
                else
                {
                    var relativePosition = position.Floor() - this.SelectedRegion.Position;
                    if (relativePosition.LiesWithin(this.SelectedRegion.Texture.Width, this.SelectedRegion.Texture.Height)
                        && this.SelectedRegion.ColorsByPixelIndex
                            .In2D(this.SelectedRegion.Texture.Width)
                            .Any(tuple => (tuple.Position == relativePosition) && (tuple.Value != default)))
                    {
                        var withRemovedRegion = this.DisjoinRegions(mappedRegion.Name, mappedRegion.Rgba, mappedRegion, this.SelectedRegion);
                        if (withRemovedRegion.Size == 0)
                            this.SelectedRegion = default; // not strictly necessary but makes texture smaller
                        else
                            this.SelectedRegion = withRemovedRegion;
                    }
                    else
                        this.SelectedRegion = this.CombineRegions(mappedRegion.Name, mappedRegion.Rgba, mappedRegion.YieldWith(this.SelectedRegion));
                }
            }
            else
            {
                this.SelectedRegion = default;
                this.LastSelectedRegion = default;
            }
            this.OnSelected?.Invoke();
        }

        public void CycleMapType() =>
            this.MapType.Advance();

        public MapType GetMapType() =>
            this.MapType.Value;

        public void RaiseDivision() =>
            this.Division.Raise();

        public void LowerDivision() =>
            this.Division.Lower();

        public Division GetDivision() =>
            this.Division.Value;

        public Region[] GetGeographicalRegions() =>
            this.Map.GeographicalRegions[this.Division.Index].Regions;

        public Region[] GetGeographicalRegions(int index) =>
            this.Map.GeographicalRegions[index].Regions;

        #endregion

        #region Protected Methods

        protected void UpdateState() { }

        protected void HandleSizesCalculating()
        {
            this.LastRelativePosition = this.Position / this.Sizes.ActualMapSize;
        }

        protected void HandleSizesCalculated()
        {
            var newRelativePosition = this.Position / this.Sizes.ActualMapSize;
            var difference = (this.LastRelativePosition - newRelativePosition) * this.Sizes.ActualMapSize;
            this.Move(difference);
        }

        // TODO move these methods to map.cs
        protected Region CombineRegions(string name, Rgba rgba, IEnumerable<Region> regions)
        {
            var id = "n/a";
            var (position, center, width, height, colorsByPixelIndex, outlineColorsByPixelIndex) = this.MergeTextures(regions);
            var combinedColorsByPixelIndex = this.CombineOutline(colorsByPixelIndex, outlineColorsByPixelIndex);

            var texture = this.Texture.Create(width, height).WithSetData(colorsByPixelIndex);
            var outlineTexture = this.Texture.Create(width, height).WithSetData(outlineColorsByPixelIndex);
            var combinedTexture = this.Texture.Create(width, height).WithSetData(combinedColorsByPixelIndex);

            return new Region(id, name, rgba, position, center, colorsByPixelIndex, texture, outlineTexture, combinedTexture);
        }

        protected Region DisjoinRegions(string name, Rgba rgba, Region exclusionRegion, Region sourceRegion)
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