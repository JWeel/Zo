using System;
using Microsoft.Xna.Framework;
using static System.Math;

namespace Zo.Managers
{
    public class SizeManager
    {
        #region Constants

        public const int BASE_MAP_WIDTH = 720;
        public const int BASE_MAP_HEIGHT = 480;
        public const int BASE_SIDE_WIDTH = 200;
        public const int BASE_WINDOW_BORDER = 8;
        public const int BASE_SEPARATOR_WIDTH = 4;

        public const int BASE_TOTAL_WIDTH = BASE_MAP_WIDTH + BASE_SEPARATOR_WIDTH + BASE_SIDE_WIDTH;
        public const int BASE_TOTAL_HEIGHT = BASE_MAP_HEIGHT;

        public const int BASE_SIDE_TEXT_OFFSET = 10;

        #endregion

        #region Constructors

        public SizeManager()
        {
        }

        #endregion

        #region Data Members

        public event Action OnCalculating;
        public event Action OnCalculated;

        public Vector2 MapOrigin { get; set; }

        public Vector2 SidePosition { get; set; }

        public Vector2 SideTextPosition { get; set; }

        public Vector2 CursorInfoPosition { get; set; }

        public int BorderSize { get; private set; }
        public Vector2 BorderSizeVector => new Vector2(this.BorderSize);

        public int ActualTotalHeight { get; protected set; }

        public float ActualLetterScale { get; protected set; }

        public Vector2 ActualLetterSpacing { get; protected set; }

        public int ActualMapWidth { get; protected set; }

        public int ActualSideWidth { get; protected set; }

        public int ActualSeparatorWidth { get; protected set; }

        public int ActualMapHeight { get; protected set; }

        public int ActualTotalWidth { get; protected set; }

        public Vector2 ActualMapSize => new Vector2(this.ActualMapWidth, this.ActualMapHeight);

        public Vector2 ActualTotalSize => new Vector2(this.ActualTotalWidth, this.ActualTotalHeight);

        public Vector2 ActualMapCenter { get; protected set; }

        public Vector2 ActualTotalCenter { get; protected set; }

        public Rectangle ActualMapRectangle { get; protected set; }

        #endregion

        #region Public Methods

        public void Calculate(float scale, bool useWindowBorder)
        {
            this.OnCalculating?.Invoke();

            var useBorderFactor = (useWindowBorder ? 1 : 0);

            this.BorderSize = BASE_WINDOW_BORDER * useBorderFactor;

            this.ActualMapWidth = (int) Ceiling(BASE_MAP_WIDTH * scale);
            this.ActualSideWidth = (int) Ceiling(BASE_SIDE_WIDTH * scale);
            this.ActualSeparatorWidth = (int) Ceiling(BASE_SEPARATOR_WIDTH * scale);
            this.ActualTotalWidth = this.ActualMapWidth + this.ActualSideWidth + this.ActualSeparatorWidth;

            this.ActualMapHeight = (int) Ceiling(BASE_MAP_HEIGHT * scale);
            this.ActualTotalHeight = this.ActualMapHeight;

            this.ActualMapCenter = new Vector2(this.ActualMapWidth / 2, this.ActualMapHeight / 2);
            this.ActualTotalCenter = new Vector2(this.ActualTotalWidth / 2, this.ActualTotalHeight / 2);

            this.ActualMapRectangle = new Rectangle(this.BorderSize, this.BorderSize, this.ActualMapWidth, this.ActualMapHeight);

            this.ActualLetterScale = scale;
            this.ActualLetterSpacing = new Vector2(
                x: (int) Ceiling((TextManager.BASE_LETTER_WIDTH + TextManager.BASE_LETTER_X_DISTANCE) * scale),
                y: (int) Ceiling(TextManager.BASE_LETTER_Y_DISTANCE * scale));

            this.MapOrigin = new Vector2(this.BorderSize);
            this.SidePosition = new Vector2(this.BorderSize + this.ActualMapWidth + this.ActualSeparatorWidth, this.BorderSize);
            this.SideTextPosition = this.SidePosition + (new Vector2(BASE_SIDE_TEXT_OFFSET, 2 * BASE_SIDE_TEXT_OFFSET) * scale);

            this.OnCalculated?.Invoke();
        }

        #endregion
    }
}