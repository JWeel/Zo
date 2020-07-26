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

        public static readonly Cell EMPTY_CELL = new Cell(-1, default, default, default, string.Empty, 0, default);
        public static readonly Region EMPTY_REGION = new Region(string.Empty, default, Array.Empty<Cell>(), default);

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

        protected event Action OnSelected;

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

        protected Map Map { get; set; }
        public Dictionary<Rgba, Cell> Cells => this.Map.Cells;

        public Region SelectedRegion { get; protected set; }

        protected Vector2 LastRelativePosition { get; set; }

        protected Cycle<MapType> MapType { get; }

        protected Axis<Division> Division { get; }

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
            var rgba = (Rgba) this.Map.RootPixelGrid.Item(position);
            if (rgba != default)
            {
                var cell = this.Map.Cells[rgba];

                // should store Color[] used for generating texture in region
                // then using that and position can calculate if relative index is non default
                // in which case thats the region we want
                // and no reason to check all regions either, just selectedregion
                // does require still mapping from position -> ?? (rgba) -> ?? (cell) -> region

                // so yeah this would work but its slow as fuck
                // should just go back to cell selection for now which at least is more reliable even if its bad on large
                // can still try if getting rid of getdata calls in region would help
                // that is, dont use cell method when basing new region on existing region, but use region methods
                // also, could store colorsbypixel array in cell too so never need to do getdata

                // its a lot faster now without the getdata !!!
                // so now have to add removal which is more tricky

                if (this.SelectedRegion == null)
                {
                    this.SelectedRegion = this.Map.GeographicalRegions[this.Division.Index]
                        .First(x => x.Cells.Any(cell => cell.Rgba == rgba));
                }
                else
                {
                    var relativePosition = position.Round() - this.SelectedRegion.Position;
                    if ((relativePosition.X < 0) ||
                        (relativePosition.Y < 0) ||
                        (relativePosition.X > this.SelectedRegion.Texture.Width) ||
                        (relativePosition.Y > this.SelectedRegion.Texture.Height))
                    {
                        // if adding
                        var newSelection = this.Map.GeographicalRegions[this.Division.Index]
                            .First(x => x.Cells.Any(cell => cell.Rgba == rgba));
                        this.SelectedRegion = new Region(newSelection.Name, newSelection.Rgba, 
                            newSelection.YieldWith(this.SelectedRegion).ToArray(), this.Texture.Create);

                        // else deselect
                    }
                    else
                    {
                        var color = this.SelectedRegion.ColorsByPixelIndex
                            .In2D(this.SelectedRegion.Texture.Width)
                            .FirstOrDefault(tuple => (tuple.Position == relativePosition))
                            .Value;
                        if (color != default)
                        {
                            // remove current from selected region
                        }
                        else
                        {
                            var newSelection = this.Map.GeographicalRegions[this.Division.Index]
                                .First(x => x.Cells.Any(cell => cell.Rgba == rgba));
                            this.SelectedRegion = new Region(newSelection.Name, newSelection.Rgba, 
                                newSelection.YieldWith(this.SelectedRegion).ToArray(), this.Texture.Create);
                        }
                    }
                }

                // if (!this.SelectedRegions.Contains(selectedRegion))
                //     this.SelectedRegions.Add(selectedRegion);
                // else
                //     this.DeselectRegion(selectedRegion);

                // for (int index = 0; index < this.Map.GeographicalRegions.Length && index != this.Division.Index; index++)
                // {
                //     this.Map.GeographicalRegions[index]
                //         .Where(x => x.Cells.Intersect(current.Cells).Any())
                //         .Where(this.SelectedRegions.Contains)
                //         .Each(this.DeselectRegion);
                // }
            }
            else
                this.SelectedRegion = default;
            //     this.DeselectAll();
            this.OnSelected?.Invoke();
        }

        // public void DeselectRegion(Region region) =>
        //     this.SelectedRegions.Remove(region);

        // public void DeselectAll() =>
        //     this.SelectedRegions.Clear();

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
            this.Map.GeographicalRegions[this.Division.Index];

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

        #endregion
    }
}