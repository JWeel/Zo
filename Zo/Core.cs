using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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

        protected event Action<GameTime> OnUpdate;

        protected event Action<SpriteBatch> OnDraw;

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

        protected FrameManager Frame { get; set; }

        protected InputSource InputSource { get; set; }

        // yuck
        protected bool RecentlyFinishedTyping { get; set; }

        #endregion

        #region Overriden Methods

        protected override void Initialize()
        {
            this.Input = new InputManager(this.Platform.Sizes, subscription => this.OnUpdate += subscription);
            this.Text = new TextManager(this.Platform.Sizes, subscription => this.OnUpdate += subscription);
            this.Map = new MapManager(this.Platform.Sizes, this.Texture, subscription => this.OnUpdate += subscription);
            this.Animation = new AnimationManager(subscription => this.OnUpdate += subscription, subscription => this.Map.OnSelected += subscription);// this.Map.SubscribeToSelect);

            this.Frame = new FrameManager(this.Platform.Sizes, subscription => this.OnUpdate += subscription, subscription => this.OnDraw += subscription);

            // if GameWindow.AllowUserResizing is set in our ctor, 
            //      1 the GameWindow.ClientSizeChanged event gets raised
            //      2 position of window is not centered
            // therefore we put it here
            // GameWindow.IsBorderless can be put in our ctor just fine, but is here for consistency.
            this.Window.AllowUserResizing = true;
            this.Window.IsBorderless = true;
            this.Window.ClientSizeChanged += this.OnWindowResize;

            this.Window.TextInput += this.OnTyping;

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
            this.Frame.LoadContent(this.Content);
            // base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // TODO confirmation
            if (this.Input.KeyPressed(Keys.Escape))
                this.Exit();

            // call subscribing managers
            this.OnUpdate?.Invoke(gameTime);

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
            if (!this.Platform.Device.IsFullScreen && !this.Input.TypingEnabled)
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

            if (!this.Input.TypingEnabled)
            {
                if (this.Input.KeyPressed(Keys.OemCloseBrackets))
                    this.Map.Zoom(MapManager.BASE_ZOOM_IN_AMOUNT, zoomOrigin: this.Input.CurrentMouseState.ToVector2());

                if (this.Input.KeyPressed(Keys.OemOpenBrackets))
                    this.Map.Zoom(MapManager.BASE_ZOOM_OUT_AMOUNT, zoomOrigin: this.Input.CurrentMouseState.ToVector2());

                if (this.Input.KeyPressed(Keys.OemPlus))
                    this.Map.Zoom(MapManager.BASE_ZOOM_IN_AMOUNT);

                if (this.Input.KeyPressed(Keys.OemMinus))
                    this.Map.Zoom(MapManager.BASE_ZOOM_OUT_AMOUNT);
            }

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

            if (!this.Input.TypingEnabled)
            {
                if (this.Input.KeyDown(Keys.Left))
                    this.Map.Move(10f, 0);
                if (this.Input.KeyDown(Keys.Right))
                    this.Map.Move(-10f, 0);
                if (this.Input.KeyDown(Keys.Up))
                    this.Map.Move(0, 10f);
                if (this.Input.KeyDown(Keys.Down))
                    this.Map.Move(0, -10f);
            }

            #endregion

            #region Selection

            if (this.Input.MousePressed(MouseButton.Left) && this.Platform.Sizes.ActualMapRectangle.Contains(this.Input.CurrentMouseState))
            {
                // get point relative to position of scaled image
                var relativePosition = (this.Input.CurrentMouseState.ToVector2() - this.Map.ActualPosition) / this.Map.Scale / this.Platform.GlobalScale;

                switch (this.Map.GetMapType())
                {
                    case MapType.Political:
                        this.Map.SelectFief(relativePosition, modifying: this.Input.KeysDownAny(Keys.LeftControl, Keys.RightControl));
                        break;
                    case MapType.Natural:
                        break;
                    case MapType.Geographical:
                        this.Map.SelectGeographicalRegion(relativePosition, combineRegions: this.Input.KeysDownAny(Keys.LeftControl, Keys.RightControl));
                        break;
                }

                if (this.Input.TypingEnabled)
                {
                    // conditionally unflag
                    this.Input.TypingEnabled = false;
                }
            }

            #endregion

            #region State Management

            if (!this.Input.TypingEnabled)
            {
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

                if (this.Input.KeyPressed(Keys.G))
                    this.Map.VisibleLabel = !this.Map.VisibleLabel;

                if (this.Input.KeyPressed(Keys.D8))
                    this.Map.CycleMapType();

                if (this.Input.KeyPressed(Keys.D9))
                    this.Map.LowerDivision();
                if (this.Input.KeyPressed(Keys.D0))
                    this.Map.RaiseDivision();
            }

            #endregion

            #region Reset

            if (this.Input.KeyPressed(Keys.R) && !this.Platform.Device.IsFullScreen && !this.Input.TypingEnabled)
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

            #region Enable Typing

            if (this.RecentlyFinishedTyping)
                this.RecentlyFinishedTyping = false;
            else if (!this.Input.TypingEnabled)
            {
                if (this.Map.SelectedFief.HasValue() && this.Input.KeyPressed(Keys.Enter))
                {
                    this.Input.TypingEnabled = true;
                    this.InputSource = InputSource.FiefName;
                }
            }

            #endregion
        }

        protected override void Draw(GameTime gameTime)
        {
            // clears the backbuffer, giving the GPU a reliable internal state to work with
            this.GraphicsDevice.Clear(Platform.BackgroundColor);

            // PointClamp: scaling uses nearest pixel instead of blurring
            // BlendState.NonPremultiplied, AlphaBlend
            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointWrap);

            this.OnDraw?.Invoke(this.SpriteBatch);

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
                    this.Map.GetFiefs()
                        .Each(fief =>
                        {
                            this.SpriteBatch.DrawAt(
                                   texture: fief.Region.CombinedTexture,
                                   position: (fief.Region.Position * actualMapScale) + this.Map.ActualPosition,
                                   color: fief.Color,
                                   scale: actualMapScale,
                                   depth: 0.3f
                               );
                            if (this.Map.VisibleLabel)
                                this.DrawText(fief.Name, (fief.Region.Center * actualMapScale) + this.Map.ActualPosition, depth: 0.45f, center: true);
                        });
                    // this.Map.GetGeographicalRegions()
                    //     .Each(region => this.SpriteBatch.DrawAt(
                    //             texture: region.OutlineTexture,
                    //             position: (region.Position * actualMapScale) + this.Map.ActualPosition,
                    //             color: new Color(50, 50, 50, 40),
                    //             scale: actualMapScale,
                    //             depth: 0.3f
                    //         ));
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
                        .Each(region =>
                        {
                            this.SpriteBatch.DrawAt(
                                texture: region.CombinedTexture,
                                position: (region.Position * actualMapScale) + this.Map.ActualPosition,
                                color: region.Color,
                                scale: actualMapScale,
                                depth: 0.1f
                            );
                            if (this.Map.VisibleLabel)
                                this.DrawText(region.Id, (region.Center * actualMapScale) + this.Map.ActualPosition, depth: 0.45f, center: true);
                        });
                    break;
            }

            // if (this.Map.SelectedRegion.HasValue)
            if (this.Map.SelectedRegion != default)
                this.SpriteBatch.DrawAt(
                    texture: this.Map.SelectedRegion.OutlineTexture,
                    position: (this.Map.SelectedRegion.Position * actualMapScale) + this.Map.ActualPosition,
                    color: this.Animation.SelectionColor.Value,
                    scale: actualMapScale,
                    depth: 0.35f
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
                + $"Mouse Map Pos {((this.Input.CurrentMouseState.ToVector2() - this.Map.ActualPosition) / this.Map.Scale / this.Platform.GlobalScale).PrintInt()}"
                + Environment.NewLine
                + $"Fiefs {this.Map.GetFiefCount()}"
                // + $"View Center {this.Map.ViewCenter.PrintInt()}"
                // + Environment.NewLine
                // + $"Map Center {this.Map.ViewMapCenter.PrintInt()}"
                // + Environment.NewLine
                // + $"Border {this.Platform.Sizes.BorderSize}"
                // + Environment.NewLine
                // + Environment.NewLine + this.Platform.Device.GraphicsDevice.DisplayMode.Width
                // + Environment.NewLine + this.Platform.Device.GraphicsDevice.DisplayMode.Height
                // + Environment.NewLine + this.Platform.Device.GraphicsDevice.DisplayMode.Width / (float) SizeManager.BASE_TOTAL_WIDTH
                // + Environment.NewLine + this.Platform.Device.GraphicsDevice.DisplayMode.Height / (float) SizeManager.BASE_TOTAL_HEIGHT
                // + Environment.NewLine + "keysup:" + this.Input.KeysUp(Keys.LeftControl, Keys.RightControl)
                + Environment.NewLine + "Selected: " + this.Map.SelectedFief?.Name
                + Environment.NewLine + "Typing: " + (this.Input.TypingEnabled ? new string(this.Input.TypedCharacters.ToArray()) : "false")
                , this.Platform.Sizes.SideTextPosition);

            this.DrawText(this.Map.Debug,
                this.Platform.Sizes.SideTextPosition + new Vector2(0, this.Platform.Sizes.ActualMapHeight * 2 / 5));


            // if (this.Map.LastSelectedRegion.HasValue)
            if (this.Map.LastSelection != default)
                this.DrawText($"Selected: {this.Map.LastSelection.Name}"
                    + Environment.NewLine + $"id: {this.Map.LastSelection.Id}"
                    + Environment.NewLine + $"rgba: {this.Map.LastSelection.Rgba}"
                    + Environment.NewLine + $"position: {this.Map.LastSelection.Position.PrintInt()}"
                    + Environment.NewLine + $"center: {this.Map.LastSelection.Center.PrintInt()}"
                    + Environment.NewLine + $"size: {this.Map.LastSelection.Size}"
                    // + Environment.NewLine + $"neighbours: {region.Neighbours.Length}"
                    , this.Platform.Sizes.SideTextPosition + new Vector2(0, this.Platform.Sizes.ActualMapHeight * 52 / 100));


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

        protected void DrawText(string text, Vector2 position, Color? color = default, float? scale = default, float? depth = default, bool center = false)
        {
            if (text.IsNullOrWhiteSpace())
                return;

            color.DefaultTo(Color.FloralWhite);
            depth.DefaultTo(0.9f);
            scale.DefaultTo(this.Platform.GlobalScale);

            if (center)
                position -= new Vector2((this.Text.CharacterSize.X + this.Text.Font.Spacing) * text.Length / 1.5f, this.Text.CharacterSize.Y / 1.5f);

            this.SpriteBatch.DrawText(this.Text.Font, text, position, color.Value, scale.Value, depth.Value);
        }

        #endregion

        #region Event Methods

        protected void OnTyping(object sender, TextInputEventArgs e)
        {
            if (!this.Input.TypingEnabled)
                return;

            if (e.Key == Keys.Enter)
            {
                var typed = new string(this.Input.TypedCharacters.ToArray());
                this.Input.TypedCharacters.Clear();

                switch (this.InputSource)
                {
                    case InputSource.None:
                        Console.WriteLine("Invalid typing state.");
                        break;
                    case InputSource.FiefName:
                        this.Map.RenameFief(typed);
                        break;
                }

                this.Input.TypingEnabled = false;
                this.RecentlyFinishedTyping = true;
            }
            else
            {
                if (e.Key == Keys.Back)
                    this.Input.TypedCharacters.RemoveLast();
                else
                    this.Input.TypedCharacters.Add(e.Character);
            }
        }

        // this.Window.ClientSizeChanged gets called 3 times after resizing the window ??!!
        // EVEN if we unsubscribe in the method and not resubscribe until disposal ??!!
        // so instead we can delay resubscribing until the next Update call
        // but if resizing is slow (Update gets called before resizing is finished), then it resubscribes too early
        // so instead to waiting for another update call, we add a minimum frame count
        // TODO figure out why delay works but frame count doesnt
        protected void OnWindowResize(object sender, EventArgs e)
        {
            using var scope = this.OnWindowResizeDelayedResubscriptionScope();
            this.Platform.SetGlobalScaleByDimensions(this.Window.ClientBounds.Width, this.Window.ClientBounds.Height);
        }

        protected bool _canDelayResubscription = true;
        protected Scope OnWindowResizeDelayedResubscriptionScope()
        {
            Action preOperation = () => this.Window.ClientSizeChanged -= this.OnWindowResize;
            Action postOperation = () => _canDelayResubscription.Case(() =>
                {
                    _canDelayResubscription = false;
                    Task.Delay(500).ContinueWith(_ => this.ResubscribeOnWindowResize(default));
                });
            return new Scope(preOperation, postOperation);
        }

        protected void ResubscribeOnWindowResize(GameTime gameTime)
        {
            this.Window.ClientSizeChanged += this.OnWindowResize;
            this.OnUpdate -= this.ResubscribeOnWindowResize;
            _canDelayResubscription = true;
        }

        // protected Scope OnWindowResizeDelayedResubscriptionScope()
        // {
        //     Action preOperation = () => this.Window.ClientSizeChanged -= this.OnWindowResize;
        //     Action postOperation = () => this.OnUpdate += this.ResubscribeOnWindowResize;
        //     return new Scope(preOperation, postOperation);
        // }

        // protected int _resubscribeOnWindowResizeFrameCounter;
        // protected void ResubscribeOnWindowResize(GameTime gameTime)
        // {
        //     if (_resubscribeOnWindowResizeFrameCounter++ < 1000)
        //         return;

        //     _resubscribeOnWindowResizeFrameCounter = 0;
        //     this.Window.ClientSizeChanged += this.OnWindowResize;
        //     this.OnUpdate -= this.ResubscribeOnWindowResize;
        // }

        #endregion
    }
}