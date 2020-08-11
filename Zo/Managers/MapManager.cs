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

        public const float MAX_MAP_ZOOM = 6f;
        public const float MIN_MAP_ZOOM = 1f;
        public const float BASE_MAP_ZOOM = 1.5f;
        public const float BASE_ZOOM_IN_AMOUNT = 0.25f;
        public const float BASE_ZOOM_OUT_AMOUNT = -0.25f;

        #endregion

        #region Constructors

        public MapManager(SizeManager sizes, RandomManager random, TextureRepository texture, Action<Action<GameTime>> subscribeToUpdate)
        {
            this.Sizes = sizes;
            this.Random = random;
            this.Texture = texture;

            this.Scale = BASE_MAP_ZOOM;
            this.VisibleBorder = true;

            this.Position = new Vector2(-100, -100);

            this.Map = new Map(this.Texture);

            this.MapType = new Cycle<MapType>(default(MapType).GetValues());
            this.Division = new Axis<Division>(default(Division).GetValues());

            subscribeToUpdate(this.UpdateState);
            sizes.OnCalculating += this.HandleSizesCalculating;
            sizes.OnCalculated += this.HandleSizesCalculated;
        }

        #endregion

        #region Properties

        public event Action OnSelected;

        public SizeManager Sizes { get; }

        public RandomManager Random { get; }

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

        public Fief SelectedFief { get; protected set; }

        public Region LastSelection { get; protected set; }

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

        public void SelectGeographicalRegion(Vector2 position, bool combineRegions)
        {
            var regionMapper = this.Map.GeographicalRegions[this.Division.Index];
            var rgba = regionMapper.RgbaByPixelPosition.Item(position);
            if (rgba != default)
            {
                var mappedRegion = regionMapper.RegionByRgba[rgba];
                this.LastSelection = mappedRegion;
                if (!combineRegions || (this.SelectedRegion == default))
                    this.SelectedRegion = mappedRegion;
                else
                {
                    var relativePosition = position.Floor() - this.SelectedRegion.Position;
                    if (relativePosition.LiesWithin(this.SelectedRegion.Texture.Width, this.SelectedRegion.Texture.Height)
                        && this.SelectedRegion.ColorsByPixelIndex
                            .In2D(this.SelectedRegion.Texture.Width)
                            .Any(tuple => (tuple.Position == relativePosition) && (tuple.Value != default)))
                    {
                        var splitRegion = this.Map.CreateRegionBySplitting(mappedRegion.Name, mappedRegion.Rgba, mappedRegion, this.SelectedRegion);
                        if (splitRegion.Size == 0)
                            this.SelectedRegion = default; // not strictly necessary?
                        else
                            this.SelectedRegion = splitRegion;
                    }
                    else
                        this.SelectedRegion = this.Map.CreateRegionByCombining(mappedRegion.Name, mappedRegion.Rgba, mappedRegion.YieldWith(this.SelectedRegion));
                }
            }
            else
            {
                this.SelectedRegion = default;
                this.LastSelection = default;
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

        public void SelectFief(Vector2 position, bool modifying)
        {
            if (modifying)
            {
                var regionMapper = this.Map.GeographicalRegions[this.Division.Index];
                var rgba = regionMapper.RgbaByPixelPosition.Item(position);
                if (rgba == default)
                {
                    this.SelectedFief = default;
                    this.LastSelection = default;
                }
                else
                {
                    var mappedRegion = regionMapper.RegionByRgba[rgba];

                    this.Map.Fiefs
                        .Where(fief => (fief != this.SelectedFief))
                        .Where(fief => mappedRegion.Contains(fief.Region))
                        .Each(fief => this.Map.CreateRegionBySplitting(fief.Name, fief.Rgba, mappedRegion, fief.Region)
                            .Into(fief.UpdateRegion));
                    this.Map.Fiefs.RemoveAll(fief => (fief.Region.Size == 0));

                    if (!this.SelectedFief.HasValue())
                    {
                        var fiefName = $"f-{this.FiefCounter++}";
                        var fiefRgba = this.Random.FiefRgba();
                        var fiefRegion = new Region(id: "n/a", fiefName, fiefRgba,
                            mappedRegion.Position, mappedRegion.Center, mappedRegion.ColorsByPixelIndex,
                            mappedRegion.Texture, mappedRegion.OutlineTexture, mappedRegion.CombinedTexture);

                        var fief = new Fief(fiefName, fiefRgba, fiefRegion);
                        this.Map.Fiefs.Add(fief);
                        this.SelectedFief = fief;
                    }
                    else
                    {
                        var newRegion = this.Map.CreateRegionByCombining(this.SelectedFief.Name, this.SelectedFief.Rgba, this.SelectedFief.Region.YieldWith(mappedRegion));
                        this.SelectedFief.UpdateRegion(newRegion);
                    }
                    this.SelectedRegion = this.SelectedFief.Region;
                    this.LastSelection = this.SelectedFief.Region;
                }
            }
            else
            {
                var mappedFief = this.Map.Fiefs.SingleOrDefault(fief => fief.Region.Contains(position));
                if (mappedFief == default)
                {
                    this.SelectedFief = default;
                    this.SelectedRegion = default;
                    this.LastSelection = default;
                }
                else
                {
                    this.LastSelection = mappedFief.Region;
                    this.SelectedRegion = mappedFief.Region;
                    this.SelectedFief = mappedFief;
                }
            }

            this.OnSelected?.Invoke();
        }

        public int GetFiefCount() => this.Map.Fiefs.Count;
        protected int FiefCounter = 0;

        public IEnumerable<Fief> GetFiefs() =>
            this.Map.Fiefs;

        public void RenameFief(string name)
        {
            if (name.IsNullOrWhiteSpace())
                return;
            this.SelectedFief?.UpdateName(name);
            this.SelectedRegion = this.SelectedFief?.Region;
            this.LastSelection = this.SelectedRegion;
        }

        public void RecolorFief(Rgba rgba)
        {
            if (rgba == default)
                return;
            this.SelectedFief?.UpdateRgba(rgba);
            this.SelectedRegion = this.SelectedFief?.Region;
            this.LastSelection = this.SelectedRegion;
        }

        #endregion

        #region Protected Methods

        protected void UpdateState(GameTime gameTime)
        {
        }

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

        #endregion
    }
}