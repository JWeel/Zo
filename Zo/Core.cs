﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using Zo.Enums;
using Zo.Extensions;
using Zo.Helpers;
using Zo.Managers;
using Zo.Repositories;
using Zo.Types;

namespace Zo
{
    public class Core : Game
    {
        #region Constants

        private const string CONTENT_ROOT_DIRECTORY = "Content";

        #endregion

        #region Constructors

        public Core()
        {
            // GraphicsDeviceManager must be set up in this ctor, else InvalidOperationException: No Graphics Device Service 
            // this is because Program calls {Game}.Run() which calls DoInitialize() which calls get_GraphicsDevice()
            this.Platform = new PlatformManager(() => new GraphicsDeviceManager(this));
            this.Texture = new TextureRepository(this.GraphicsDevice);
        }

        #endregion

        #region Data Members

        protected event Action OnUpdate;

        protected PlatformManager Platform { get; set; }

        protected TextureRepository Texture { get; set; }

        protected InputManager Input { get; set; }

        protected TextManager Text { get; set; }

        protected MapManager Map { get; set; }

        protected AnimationManager Animation { get; set; }

        protected SpriteBatch SpriteBatch { get; set; }

        protected int AreaIndex { get; set; } = -1;

        protected Cycle<Division> BorderCycle { get; } =
            new Cycle<Division>(((Division) (-1)).YieldWith((default(Division).GetValues())).ToArray());

        #endregion

        #region Overriden Methods

        protected override void Initialize()
        {
            this.Input = new InputManager(this.Platform.Sizes, subscription => this.OnUpdate += subscription);
            this.Text = new TextManager(this.Platform.Sizes, subscription => this.OnUpdate += subscription);
            this.Map = new MapManager(this.Platform.Sizes, this.Texture, subscription => this.OnUpdate += subscription);
            this.Animation = new AnimationManager(subscription => this.OnUpdate += subscription, this.Map.SubscribeToSelect);

            // if GameWindow.AllowUserResizing is set in our ctor, 
            //      1 the GameWindow.ClientSizeChanged event gets raised
            //      2 position of window is not centered
            // therefore we put it here
            // GameWindow.IsBorderless can be put in our ctor just fine, but is here for consistency.
            this.Window.AllowUserResizing = true;
            this.Window.IsBorderless = true;
            this.Window.ClientSizeChanged += this.OnWindowResize;

            base.Initialize(); // sets up this.GraphicsDevice and calls this.LoadContent
        }

        protected override void LoadContent()
        {
            this.Content.RootDirectory = CONTENT_ROOT_DIRECTORY;
            this.SpriteBatch = new SpriteBatch(this.GraphicsDevice);

            this.Platform.LoadContent(this.Content);
            this.Texture.LoadContent(this.Content);
            this.Text.LoadContent(this.Content);

            this.Map.LoadContent(this.Content);
            // base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // TODO confirmation
            if (this.Input.KeyPressed(Keys.Escape))
                Exit();

            // call subscribing managers
            this.OnUpdate?.Invoke();

            #region Full Screen

            if (this.Input.KeyPressed(Keys.Enter) && this.Input.KeysDownAny(Keys.LeftAlt, Keys.RightAlt))
            {
                using (this.OnWindowResizeDelayedResubscriptionScope())
                {
                    this.Platform.ToggleFullScreen();
                }
            }

            #endregion

            #region Rescale Window By Keyboard

            // do we even want this
            if (!this.Platform.Device.IsFullScreen)
            {
                if (this.Input.KeyPressed(Keys.OemPeriod))
                    this.Platform.AddToGlobalScaleAndRescaleWindow(.25f);
                if (this.Input.KeyPressed(Keys.OemComma))
                    this.Platform.AddToGlobalScaleAndRescaleWindow(-.25f);
            }

            #endregion

            #region Mouse Visibility

            this.IsMouseVisible = !this.Platform.IsMoving && !this.Map.IsMoving && !this.Input.KeyDown(Keys.LeftShift);

            #endregion

            #region Move Window

            if (this.Platform.IsMoving && this.Input.CurrentMouseState.RightButton.IsReleased())
                this.Platform.IsMoving = false;

            if (!this.Platform.IsMoving
                && !this.Platform.Device.IsFullScreen
                && this.Input.CurrentMouseState.RightButton.IsPressed()
                && this.Platform.Device.GraphicsDevice.Viewport.Bounds.Contains(this.Input.CurrentMouseState))
            {
                this.Platform.IsMoving = true;
                this.Platform.LastWindowPosition = this.Input.CurrentMouseState.ToVector2();
            }

            if (this.Platform.IsMoving)
                this.Window.Position = new Point(
                    this.Window.Position.X - (int) this.Platform.LastWindowPosition.X + this.Input.CurrentMouseState.X,
                    this.Window.Position.Y - (int) this.Platform.LastWindowPosition.Y + this.Input.CurrentMouseState.Y);

            #endregion

            #region Zoom Map

            if (this.Input.MouseScrolled() && this.Platform.Sizes.ActualMapRectangle.Contains(this.Input.CurrentMouseState))
                this.Map.Zoom(this.Input.MouseScrolledUp() ? MapManager.BASE_ZOOM_IN_AMOUNT : MapManager.BASE_ZOOM_OUT_AMOUNT,
                    zoomOrigin: this.Input.CurrentMouseState.ToVector2());

            if (this.Input.KeyPressed(Keys.OemCloseBrackets))
                this.Map.Zoom(MapManager.BASE_ZOOM_IN_AMOUNT, zoomOrigin: this.Input.CurrentMouseState.ToVector2());

            if (this.Input.KeyPressed(Keys.OemOpenBrackets))
                this.Map.Zoom(MapManager.BASE_ZOOM_OUT_AMOUNT, zoomOrigin: this.Input.CurrentMouseState.ToVector2());

            if (this.Input.KeyPressed(Keys.OemPlus))
                this.Map.Zoom(MapManager.BASE_ZOOM_IN_AMOUNT);

            if (this.Input.KeyPressed(Keys.OemMinus))
                this.Map.Zoom(MapManager.BASE_ZOOM_OUT_AMOUNT);

            #endregion

            #region Move Map

            if (this.Map.IsMoving)
            {
                this.Map.MoveByPosition(this.Input.CurrentMouseState.ToVector2());
                if (this.Input.CurrentMouseState.MiddleButton.IsReleased())
                    this.Map.StopMoving();
            }

            if (!this.Map.IsMoving
            && this.Input.MousePressed(MouseButton.Middle)
            && this.Platform.Sizes.ActualMapRectangle.Contains(this.Input.CurrentMouseState))
                this.Map.StartMoving(this.Input.CurrentMouseState.ToVector2());

            if (this.Input.KeyDown(Keys.Left))
                this.Map.Move(10f, 0);
            if (this.Input.KeyDown(Keys.Right))
                this.Map.Move(-10f, 0);
            if (this.Input.KeyDown(Keys.Up))
                this.Map.Move(0, 10f);
            if (this.Input.KeyDown(Keys.Down))
                this.Map.Move(0, -10f);

            #endregion

            #region Selection

            if (this.Input.MousePressed(MouseButton.Left) && this.Platform.Sizes.ActualMapRectangle.Contains(this.Input.CurrentMouseState))
            {
                // get point relative to position of scaled image
                var relativePosition = (this.Input.CurrentMouseState.ToVector2() - this.Map.ActualPosition) / this.Map.Scale / this.Platform.GlobalScale;

                switch (this.Map.GetMapType())
                {
                    case MapType.Political:
                        break;
                    case MapType.Natural:
                        break;
                    case MapType.Geographical:
                        this.Map.SelectGeographicalRegion(relativePosition);
                        break;
                }
            }

            #endregion

            #region State Management

            // if (this.Input.KeyPressed(Keys.Z))
            //     IncrementStateIndex();

            if (this.Input.KeyPressed(Keys.J))
                this.ChangeProperty(x => x.AreaIndex, x => x.AddWithUpperLimit(1, 6));
            if (this.Input.KeyPressed(Keys.K))
                this.ChangeProperty(x => x.AreaIndex, x => x.AddWithLowerLimit(-1, -1));
            if (this.Input.KeyPressed(Keys.N))
                this.BorderCycle.Advance();
            if (this.Input.KeyPressed(Keys.M))
                this.BorderCycle.Reverse();

            if (this.Input.KeyPressed(Keys.U))
            {
                this.ChangeProperty(x => x.AreaIndex, x => x.AddWithUpperLimit(1, 6));
                this.BorderCycle.Advance();
            }
            if (this.Input.KeyPressed(Keys.I))
            {
                this.ChangeProperty(x => x.AreaIndex, x => x.AddWithLowerLimit(-1, -1));
                this.BorderCycle.Reverse();
            }

            if (this.Input.KeyPressed(Keys.L))
                this.Map.VisibleBorder = !this.Map.VisibleBorder;

            if (this.Input.KeyPressed(Keys.D8))
                this.Map.CycleMapType();

            if (this.Input.KeyPressed(Keys.D9))
                this.Map.LowerDivision();
            if (this.Input.KeyPressed(Keys.D0))
                this.Map.RaiseDivision();

            #endregion

            #region Reset

            if (this.Input.KeyPressed(Keys.R) && !this.Platform.Device.IsFullScreen)
            {
                this.Platform.ResetGlobalScaleAndRescaleWindow();
                this.Window.Position = new Point(
                    x: (this.Platform.Device.GraphicsDevice.Adapter.CurrentDisplayMode.Width / 2)
                        - (this.Platform.Device.PreferredBackBufferWidth / 2),
                    y: (this.Platform.Device.GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2)
                        - (this.Platform.Device.PreferredBackBufferHeight / 2));
                this.Map.Zoom(-10f);
            }

            #endregion
        }

        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Platform.BackgroundColor);

            // if (this.SpriteBatch == null)
            //     return;

            // PointClamp: scaling uses nearest pixel instead of blurring
            // BlendState.NonPremultiplied, AlphaBlend
            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap);

            var actualMapScale = this.Map.Scale * this.Platform.GlobalScale;

            this.SpriteBatch.DrawAt(
                texture: this.Texture.MapBackground,
                position: this.Map.ActualPosition,
                scale: actualMapScale,
                depth: 0f
            );

            switch (this.Map.GetMapType())
            {
                case MapType.Political:
                    break;
                case MapType.Natural:
                    if (this.Map.VisibleBorder)
                        this.SpriteBatch.DrawAt(
                            texture: this.Texture.OuterBorder,
                            position: this.Map.ActualPosition,
                            scale: actualMapScale,
                            color: new Color(0.2f, 0.2f, 0.2f, 0.5f),
                            depth: 0.3f
                        );
                    // dunno if it fits here
                    if (this.BorderCycle.Index > 0)
                        this.SpriteBatch.DrawAt(
                            texture: this.Texture.GeograhicalBorders[this.BorderCycle.Index - 1],
                            position: this.Map.ActualPosition,
                            scale: actualMapScale,
                            color: new Color(0.2f, 0.2f, 0.2f, 0.25f),
                            depth: 0.3f
                        );
                    break;
                case MapType.Geographical:
                    this.Map.GetGeographicalRegions()
                        .Each(region => this.SpriteBatch.DrawAt(
                                texture: region.Texture,
                                position: (region.Position * actualMapScale) + this.Map.ActualPosition,
                                color: region.Color,
                                scale: actualMapScale,
                                depth: 0.1f
                            ));
                    this.Map.GetGeographicalRegions()
                        .Each(region => this.SpriteBatch.DrawAt(
                                texture: region.OutlineTexture,
                                position: (region.Position * actualMapScale) + this.Map.ActualPosition,
                                color: region.OutlineColor,
                                scale: actualMapScale,
                                depth: 0.3f
                            ));
                    break;
            }

            if (this.Map.SelectedRegion != null)
                this.SpriteBatch.DrawAt(
                    texture: this.Map.SelectedRegion.Texture,
                    position: (this.Map.SelectedRegion.Position * actualMapScale) + this.Map.ActualPosition,
                    color: this.Animation.SelectionColor.Value,
                    scale: actualMapScale,
                    depth: 0.2f
                );

            this.SpriteBatch.DrawTo(
                texture: this.Texture.Blank,
                destinationRectangle: this.Platform.SidePanelBase,
                color: this.Platform.SidePanelColor,
                depth: 0.5f
            );
            this.Platform.Borders.Each(side =>
                this.SpriteBatch.DrawTo(
                    texture: this.Texture.Blank,
                    destinationRectangle: side,
                    color: this.Platform.WindowColor,
                    depth: 0.5f
                ));


            var debug = string.Empty;
            var a = new Vector2(
                x: (this.Map.ActualPosition.X / (-this.Map.Scale + this.Platform.GlobalScale)),
                y: (this.Map.ActualPosition.Y / (-this.Map.Scale + this.Platform.GlobalScale)));
            var b = new Vector2(
                x: (this.Map.Sizes.ActualMapWidth / this.Map.Scale / 2),
                y: (this.Map.Sizes.ActualMapHeight / this.Map.Scale / 2));
            debug += a.PrintInt() + Environment.NewLine + b.PrintInt();

            var loc = this.Map.ViewCenterRectangle.Location - new Point(this.Map.ViewCenterRectangle.Width / 2, this.Map.ViewCenterRectangle.Height / 2);
            debug += Environment.NewLine + "loc: " + loc.X + "," + loc.Y;
            debug += Environment.NewLine + (loc.X / this.Map.Scale) + "," + (loc.Y / this.Map.Scale);
            debug += Environment.NewLine + "---";

            var relativeMouse = (this.Input.CurrentMouseState.ToVector2() - new Vector2(this.Platform.Sizes.BorderSize));
            this.DrawText(
                $"Global Scale {this.Platform.GlobalScale}"
                + Environment.NewLine
                + $"Map {this.Platform.Sizes.ActualMapRectangle.X},{this.Platform.Sizes.ActualMapRectangle.Y}, {this.Platform.Sizes.ActualMapRectangle.X + this.Platform.Sizes.ActualMapRectangle.Width},{this.Platform.Sizes.ActualMapRectangle.Y + this.Platform.Sizes.ActualMapRectangle.Height}"
                + Environment.NewLine
                + $"Map Scale {this.Map.Scale}"
                + Environment.NewLine
                + $"Map Pos {this.Map.ActualPosition.PrintInt()}"
                + Environment.NewLine
                + $"Mouse Pos {relativeMouse.PrintInt()}"
                + Environment.NewLine
                + $"View Center {this.Map.ViewCenter.PrintInt()}"
                + Environment.NewLine
                + $"Map Center {this.Map.ViewMapCenter.PrintInt()}"
                + Environment.NewLine
                + $"Border {this.Platform.Sizes.BorderSize}"
                + Environment.NewLine
                + debug
                , this.Platform.Sizes.SideTextPosition);

            this.DrawText(this.Map.Debug,
                this.Platform.Sizes.SideTextPosition + new Vector2(0, this.Platform.Sizes.ActualMapHeight * 2 / 5));


            // cell selection text should work by Stack
            if (this.Map.SelectedRegion != null)
                this.DrawText($"Selected: {this.Map.SelectedRegion.Name}"
                    + Environment.NewLine + $"rgba: {this.Map.SelectedRegion.Rgba}"
                    // + Environment.NewLine + $"center: ({region.Center.PrintInt()})"
                    // + Environment.NewLine + $"size: {region.Size}"
                    // + Environment.NewLine + $"neighbours: {region.Neighbours.Length}"
                    , this.Platform.Sizes.SideTextPosition + new Vector2(0, this.Platform.Sizes.ActualMapHeight * 3 / 5));


            this.DrawText($"Map Type: {this.Map.GetMapType()}",
                this.Platform.Sizes.SideTextPosition + new Vector2(0, this.Platform.Sizes.ActualMapHeight * 73 / 100));

            this.DrawText($"Division: {this.Map.GetDivision()}",
                this.Platform.Sizes.SideTextPosition + new Vector2(0, this.Platform.Sizes.ActualMapHeight * 77 / 100));


            this.DrawText($"x{this.Input.CurrentMouseState.X} y{this.Input.CurrentMouseState.Y}",
                this.Platform.Sizes.CursorInfoPosition);


            this.SpriteBatch.DrawTo(
                texture: this.Texture.Blank,
                destinationRectangle: this.Map.ViewCenterRectangle,
                color: Color.Black,
                depth: 0.91f
            );
            this.SpriteBatch.DrawAt(
                texture: this.Texture.Blank,
                position: this.Map.ViewCenter + new Vector2(this.Platform.Sizes.BorderSize),
                color: Color.WhiteSmoke,
                scale: 1f,
                depth: 0.92f
            );
            this.SpriteBatch.DrawAt(
                texture: this.Texture.Blank,
                position: relativeMouse + new Vector2(this.Platform.Sizes.BorderSize),
                color: Color.DeepPink,
                scale: 2f, // 2f so its easier to spot
                depth: 0.93f
            );

            this.SpriteBatch.End();
        }

        #endregion

        #region Helper Methods

        public void DrawText(string text, Vector2 position, Color? color = default) =>
            this.DrawText(text, position.X, position.Y, color);

        public void DrawText(string text, float x, float y, Color? color = default) =>
            this.DrawText(text, (int) x, (int) y, color);

        protected void DrawText(string text, int x, int y, Color? color = default)
        {
            if (text.IsNullOrWhiteSpace())
                return;
            color.DefaultTo(Color.LightGoldenrodYellow);

            text.Split(Environment.NewLine)
                .WithIndex()
                .Each(group => group.Value
                    .WithIndex()
                    .Each(pair =>
                    {
                        if (!this.Text.Supports(pair.Value) && pair.Value != TextManager.NEWLINE_CHARACTER)
                        {
                            // Console.WriteLine($"Unsupported character at index {pair.Key}: {pair.Value} ({(int) pair.Value})");
                            return;
                        }
                        var actualX = (pair.Key * this.Platform.Sizes.ActualLetterSpacing.X) + x;
                        var actualY = (group.Key * this.Platform.Sizes.ActualLetterSpacing.Y) + y;
                        this.SpriteBatch.DrawAt(
                            texture: this.Text[pair.Value],
                            position: new Vector2(actualX, actualY),
                            scale: this.Platform.Sizes.ActualLetterScale,
                            color: color.Value,
                            depth: 0.9f
                        );
                    }));
        }

        #endregion

        #region Event Methods

        // this.Window.ClientSizeChanged gets called 3 times after resizing the window ??!!
        // EVEN if we unsubscribe in the method and not resubscribe until disposal ??!!
        // so instead we delay resubscribing until the next Update call
        protected void OnWindowResize(object sender, EventArgs e)
        {
            using var scope = this.OnWindowResizeDelayedResubscriptionScope();
            this.Platform.SetGlobalScaleByDimensions(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
        }

        protected Scope OnWindowResizeDelayedResubscriptionScope()
        {
            Action preOperation = () => this.Window.ClientSizeChanged -= this.OnWindowResize;
            Action postOperation = () => this.OnUpdate += this.ResubscribeOnWindowResize;
            return new Scope(preOperation, postOperation);
        }

        protected void ResubscribeOnWindowResize()
        {
            this.Window.ClientSizeChanged += this.OnWindowResize;
            this.OnUpdate -= this.ResubscribeOnWindowResize;
        }

        #endregion
    }
}