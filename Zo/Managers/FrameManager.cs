using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using Zo.Extensions;

namespace Zo.Managers
{
    public class FrameManager
    {
        protected SizeManager Sizes { get; }

        protected int FrameRate { get; set; }
        protected int FrameCounter { get; set; }
        protected TimeSpan ElapsedTime { get; set; }

        protected SpriteFont Font { get; set; }

        public FrameManager(SizeManager sizes, Action<Action<GameTime>> subscribeToUpdate, Action<Action<SpriteBatch>> subscribeToDraw)
        {
            subscribeToUpdate(this.UpdateState);
            subscribeToDraw(this.DrawState);

            this.Sizes = sizes;
            this.FrameRate = 0;
            this.FrameCounter = 0;
            this.ElapsedTime = TimeSpan.Zero;
        }

        public void LoadContent(ContentManager content)
        {
            this.Font = content.Load<SpriteFont>("Alphabet/alphabet");
        }

        public void UpdateState(GameTime gameTime)
        {
            this.ElapsedTime += gameTime.ElapsedGameTime;

            if (this.ElapsedTime > TimeSpan.FromSeconds(1))
            {
                this.ElapsedTime -= TimeSpan.FromSeconds(1);
                this.FrameRate = this.FrameCounter;
                this.FrameCounter = 0;
            }
        }

        public void DrawState(SpriteBatch spriteBatch)
        {
            this.FrameCounter++;
            string text = this.FrameRate.ToString();
            spriteBatch.DrawText(this.Font, text, this.Sizes.BorderSizeVector, Color.White, scale: 1f, depth: 0.9f);
        }
    }
}