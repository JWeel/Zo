using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Zo.Managers
{
    public class FrameManager : DrawableGameComponent
    {
        protected Vector2 Position { get; }
        protected Vector2 Position2 { get; }

        protected SpriteBatch SpriteBatch { get; set; }
        protected SpriteFont Font { get; set; }

        protected int FrameRate { get; set; }
        protected int FrameCounter { get; set; } 
        protected TimeSpan ElapsedTime { get; set; }

        public FrameManager(Game game, Vector2 position)
            : base(game)
        {
            this.Position = position;
            this.Position2 = new Vector2(Position.X + 1, Position.Y + 1);
            this.FrameRate = 0;
            this.FrameCounter = 0;
            this.ElapsedTime = TimeSpan.Zero;
        }

        public void LoadContent(ContentManager content)
        {
            this.SpriteBatch = new SpriteBatch(GraphicsDevice);
            this.Font = content.Load<SpriteFont>("Alphabet/alphabet");
        }

        public override void Update(GameTime gameTime)
        {
            this.ElapsedTime += gameTime.ElapsedGameTime;

            if (this.ElapsedTime > TimeSpan.FromSeconds(1))
            {
                this.ElapsedTime -= TimeSpan.FromSeconds(1);
                this.FrameRate = this.FrameCounter;
                this.FrameCounter = 0;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            this.FrameCounter++;

            string text = this.FrameRate.ToString();

            this.SpriteBatch.Begin();
            this.SpriteBatch.DrawString(this.Font, text, this.Position2, Color.Black);
            this.SpriteBatch.DrawString(this.Font, text, this.Position, Color.White);
            this.SpriteBatch.End();
        }
    }
}