using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using Zo.Extensions;

namespace Zo.Managers
{
    /// <summary> Keeps track of application framerate. </summary>
    public class FrameManager
    {
        #region Constants

        private static readonly TimeSpan ONE_SECOND = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructors

        public FrameManager(SizeManager sizes, Action<Action<ContentManager>> subscribeToLoad, Action<Action<GameTime>> subscribeToUpdate, Action<Action<SpriteBatch>> subscribeToDraw)
        {
            subscribeToLoad(this.LoadContent);
            subscribeToUpdate(this.UpdateState);
            subscribeToDraw(this.DrawState);

            this.Sizes = sizes;
            this.FrameRate = 0;
            this.FrameCounter = 0;
            this.ElapsedTime = TimeSpan.Zero;
        }

        #endregion

        #region Properties
        protected SizeManager Sizes { get; }

        protected int FrameRate { get; set; }
        protected int FrameCounter { get; set; }
        protected TimeSpan ElapsedTime { get; set; }

        protected SpriteFont Font { get; set; }

        #endregion

        #region Methods

        protected void LoadContent(ContentManager content)
        {
            this.Font = content.Load<SpriteFont>("Alphabet/alphabet");
        }

        protected void UpdateState(GameTime gameTime)
        {
            this.ElapsedTime += gameTime.ElapsedGameTime;
            if (this.ElapsedTime < ONE_SECOND)
                return;

            this.ElapsedTime -= ONE_SECOND;
            this.FrameRate = this.FrameCounter;
            this.FrameCounter = 0;
        }

        protected void DrawState(SpriteBatch spriteBatch)
        {
            this.FrameCounter++;
            string text = this.FrameRate.ToString();
            spriteBatch.DrawText(this.Font, text, this.Sizes.BorderSizeVector, Color.White, scale: 1f, depth: 0.9f);
        }

        #endregion
    }
}