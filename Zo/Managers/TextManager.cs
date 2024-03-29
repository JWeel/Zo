using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Zo.Managers
{
    public class TextManager
    {
        #region Constants

        // all  unused
        public const int BASE_LETTER_WIDTH = 6;
        public const int BASE_LETTER_X_DISTANCE = 2;
        public const int BASE_LETTER_Y_DISTANCE = 12;
        public const int BASE_LETTER_HEIGHT = 8;

        #endregion

        #region Constructors

        public TextManager(SizeManager sizes, Action<Action<ContentManager>> subscribeToLoad, Action<Action<GameTime>> subscribeToUpdate)
        {
            subscribeToLoad(this.LoadContent);
            subscribeToUpdate(this.UpdateState);
        }

        #endregion

        #region Properties

        protected SizeManager Sizes { get; }

        public SpriteFont Font { get; protected set; }

        private Vector2? _characterSize;
        public Vector2 CharacterSize
        {
            get
            {
                if (!_characterSize.HasValue)
                {
                    _characterSize = this.Font.MeasureString("X");
                }
                return _characterSize.Value;
            }
        }

        #endregion

        #region Public Methods

        #endregion

        #region Protected Methods

        protected void LoadContent(ContentManager content)
        {
            this.Font = content.Load<SpriteFont>("Alphabet/alphabet");
        }

        protected void UpdateState(GameTime gameTime)
        {
        }

        #endregion
    }
}
