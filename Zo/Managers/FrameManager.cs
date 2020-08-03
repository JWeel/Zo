using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Zo.Managers
{
    public class FrameManager : DrawableGameComponent
    {
        public Vector2 Position { get; set; }
        public Vector2 Position2 { get; set; }
        public Color Color { get; set; }
        public Color Color2 { get; set; }

        protected SpriteBatch SpriteBatch { get; set; }
        protected SpriteFont Font { get; set; }

        protected int FrameRate { get; set; } = 0;
        protected int FrameCounter { get; set; } = 0;
        protected TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        public FrameManager(Game game, Vector2 position, Color color, Color color2)
            : base(game)
        {
            this.Position = position;
            this.Position2 = new Vector2(Position.X + 1, Position.Y + 1);
            this.Color = color;
            this.Color2 = color2;
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
            this.SpriteBatch.DrawString(this.Font, text, this.Position2, this.Color2);
            this.SpriteBatch.DrawString(this.Font, text, this.Position, this.Color);
            this.SpriteBatch.End();
        }
    }
}