using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using Zo.Extensions;
using static System.Math;

namespace Zo.Managers
{
    public class PlatformManager
    {
        #region Constants

        public const float BASE_GLOBAL_SCALE = 1.5f;
        public const float MAX_GLOBAL_SCALE = 4f;
        public const float MIN_GLOBAL_SCALE = 0.5f;

        public static readonly Color BACKGROUND_COLOR = new Color(10, 10, 10);
        public static readonly Color WINDOW_COLOR = new Color(40, 40, 50);
        public static readonly Color SIDE_PANEL_COLOR = new Color(60, 60, 70);

        #endregion

        #region Constructors

        public PlatformManager(Func<GraphicsDeviceManager> graphicsDeviceFactory, Action<Action<ContentManager>> subscribeToLoad)
        {
            subscribeToLoad(this.LoadContent);

            this.ResetGlobalScale();

            this.Device = graphicsDeviceFactory();
            this.Device.HardwareModeSwitch = false;

            this.Sizes = new SizeManager();

            this.Sizes.OnCalculated += this.HandleSizesCalculated;
            this.CalculateScaledSizes();
        }

        #endregion

        #region Properties

        public SizeManager Sizes { get; protected set; }

        private float? _lastWindowedGlobalScale;
        public float GlobalScale { get; protected set; }

        public GraphicsDeviceManager Device { get; protected set; }


        /// <summary> Defines the color that is only seen in fullscreen mode when the auto-scaled resolution does not perfectly match the desired ratio of width and height. </summary>
        public Color BackgroundColor { get; protected set; }

        /// <summary> Defines the color that is used for the window borders. </summary>
        public Color WindowColor { get; protected set; }

        /// <summary> Defines the color that is used for the background of the side panel. </summary>
        public Color SidePanelColor { get; protected set; }


        public Rectangle BorderTop { get; protected set; }

        public Rectangle BorderBottom { get; protected set; }

        public Rectangle BorderLeft { get; protected set; }

        public Rectangle BorderRight { get; protected set; }

        public Rectangle PanelSeparator { get; protected set; }

        public Rectangle SidePanelBase { get; protected set; }

        public Rectangle[] Borders { get; protected set; }


        public bool IsMoving { get; set; }

        public Vector2 LastWindowPosition { get; set; }

        #endregion

        #region Public Methods

        public void ToggleFullScreen()
        {
            this.Device.ToggleFullScreen();
            if (this.Device.IsFullScreen)
            {
                _lastWindowedGlobalScale = this.GlobalScale;
                this.GlobalScale = this.CalculateFullScreenScale();
            }
            else
                this.GlobalScale = _lastWindowedGlobalScale ?? BASE_GLOBAL_SCALE;

            this.CalculateScaledSizes();
        }

        public void AddToGlobalScaleAndRescaleWindow(float amount)
        {
            this.ChangeProperty(x => x.GlobalScale, x => x.AddWithLimits(amount, MIN_GLOBAL_SCALE, MAX_GLOBAL_SCALE));
            this.CalculateScaledSizes();
        }

        public void SetGlobalScaleByDimensions(int width, int height)
        {
            var previousWidth = this.Device.PreferredBackBufferWidth;
            var previousHeight = this.Device.PreferredBackBufferHeight;
            this.Device.PreferredBackBufferWidth = width;
            this.Device.PreferredBackBufferHeight = height;
            this.Device.ApplyChanges();

            var widthDifference = Abs(previousWidth - width);
            var heightDifference = Abs(previousHeight - height);
            var windowScale = this.CalculateWindowScale(fromWidth: widthDifference > heightDifference);

            var scaleDifference = windowScale - this.GlobalScale;
            this.AddToGlobalScaleAndRescaleWindow(scaleDifference);
        }

        public void ResetGlobalScaleAndRescaleWindow()
        {
            this.ResetGlobalScale();
            this.CalculateScaledSizes();
        }

        #endregion

        #region Protected Methods

        protected void LoadContent(ContentManager content)
        {
            this.BackgroundColor = BACKGROUND_COLOR;
            this.WindowColor = WINDOW_COLOR;
            this.SidePanelColor = SIDE_PANEL_COLOR;

            this.CalculateScaledSizes();
        }

        protected void CalculateScaledSizes() =>
            this.Sizes.Calculate(this.GlobalScale, useWindowBorder: !this.Device.IsFullScreen);

        protected void ResetGlobalScale() =>
            this.GlobalScale = BASE_GLOBAL_SCALE;

        protected float CalculateFullScreenScale() =>
            this.Device.GraphicsDevice.DisplayMode.Width / (float) SizeManager.BASE_TOTAL_WIDTH;

        protected float CalculateWindowScale(bool fromWidth) =>
            (fromWidth
                ? this.Device.GraphicsDevice.Viewport.Width / (float) SizeManager.BASE_TOTAL_WIDTH
                : this.Device.GraphicsDevice.Viewport.Height / (float) SizeManager.BASE_TOTAL_HEIGHT);

        protected void HandleSizesCalculated()
        {
            if (this.Device == null)
                return;

            var doubleBorder = this.Sizes.BorderSize * 2;

            this.Device.PreferredBackBufferWidth = this.Sizes.ActualTotalWidth + doubleBorder;
            this.Device.PreferredBackBufferHeight = this.Sizes.ActualTotalHeight + doubleBorder;
            this.Device.ApplyChanges();

            var doubleBorderedTotalWidth = this.Sizes.ActualTotalWidth + doubleBorder;
            this.BorderTop = new Rectangle(x: 0, y: 0,
                width: doubleBorderedTotalWidth, height: this.Sizes.BorderSize);
            this.BorderBottom = new Rectangle(x: 0, y: this.Sizes.BorderSize + this.Sizes.ActualTotalHeight,
                width: doubleBorderedTotalWidth, height: this.Sizes.BorderSize);

            var doubleBorderedTotalHeight = this.Sizes.ActualTotalHeight + doubleBorder;
            this.BorderLeft = new Rectangle(x: 0, y: 0,
                width: this.Sizes.BorderSize, height: doubleBorderedTotalHeight);
            this.BorderRight = new Rectangle(x: this.Sizes.BorderSize + this.Sizes.ActualTotalWidth, y: 0,
                width: this.Sizes.BorderSize, height: doubleBorderedTotalHeight);

            this.PanelSeparator = new Rectangle(x: this.Sizes.BorderSize + this.Sizes.ActualMapWidth, y: 0,
                width: this.Sizes.ActualSeparatorWidth, height: this.Sizes.ActualTotalHeight + doubleBorder);

            this.Borders = new[] { this.BorderTop, this.BorderBottom, this.BorderLeft, this.BorderRight, this.PanelSeparator };

            var sidePanelStart = this.Sizes.BorderSize + this.Sizes.ActualMapWidth + this.Sizes.ActualSeparatorWidth;
            this.SidePanelBase = new Rectangle(
                x: sidePanelStart,
                y: this.Sizes.BorderSize,
                width: this.Sizes.ActualSideWidth,
                height: this.Sizes.ActualTotalHeight);

            this.Sizes.CursorInfoPosition = new Vector2(
                x: sidePanelStart + (this.Sizes.ActualSideWidth / 2),
                y: this.Sizes.BorderSize + (this.Sizes.ActualSeparatorWidth * 2));
        }

        #endregion
    }
}