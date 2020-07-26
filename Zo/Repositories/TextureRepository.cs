using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Zo.Extensions;

namespace Zo.Repositories
{
    public class TextureRepository
    {
        #region Constructors

        public TextureRepository(GraphicsDevice graphicsDevice)
        {
            this.GraphicsDevice = graphicsDevice;
        }

        #endregion

        #region Properties

        protected GraphicsDevice GraphicsDevice { get; }

        public Texture2D MapBackground { get; protected set; }

        public Texture2D GeographicalRegion0 { get; protected set; }
        public Texture2D GeographicalRegion1 { get; protected set; }
        public Texture2D GeographicalRegion2 { get; protected set; }
        public Texture2D GeographicalRegion3 { get; protected set; }
        public Texture2D GeographicalRegion4 { get; protected set; }
        public Texture2D GeographicalRegion5 { get; protected set; }
        public Texture2D GeographicalRegion6 { get; protected set; }
        public Texture2D[] GeographicalRegions { get; protected set; }

        public Texture2D GeographicalBorder0 { get; protected set; }
        public Texture2D GeographicalBorder1 { get; protected set; }
        public Texture2D GeographicalBorder2 { get; protected set; }
        public Texture2D GeographicalBorder3 { get; protected set; }
        public Texture2D GeographicalBorder4 { get; protected set; }
        public Texture2D GeographicalBorder5 { get; protected set; }
        public Texture2D GeographicalBorder6 { get; protected set; }
        public Texture2D[] GeograhicalBorders { get; protected set; }

        public Texture2D OuterBorder { get; protected set; }

        public Texture2D Blank { get; protected set; }

        #endregion

        #region Public Methods

        public void LoadContent(ContentManager content)
        {
            this.MapBackground = content.Load<Texture2D>("Land/background");

            this.GeographicalRegions = new[]
            {
                this.GeographicalRegion0 = content.Load<Texture2D>("Area/a0"),
                this.GeographicalRegion1 = content.Load<Texture2D>("Area/a1"),
                this.GeographicalRegion2 = content.Load<Texture2D>("Area/a2"),
                this.GeographicalRegion3 = content.Load<Texture2D>("Area/a3"),
                this.GeographicalRegion4 = content.Load<Texture2D>("Area/a4"),
                this.GeographicalRegion5 = content.Load<Texture2D>("Area/a5"),
                this.GeographicalRegion6 = content.Load<Texture2D>("Area/a6"),
            };
            this.GeograhicalBorders = new[]
            {
                this.GeographicalBorder0 = content.Load<Texture2D>("Border/a0c"),
                this.GeographicalBorder1 = content.Load<Texture2D>("Border/a1c"),
                this.GeographicalBorder2 = content.Load<Texture2D>("Border/a2c"),
                this.GeographicalBorder3 = content.Load<Texture2D>("Border/a3c"),
                this.GeographicalBorder4 = content.Load<Texture2D>("Border/a4c"),
                this.GeographicalBorder5 = content.Load<Texture2D>("Border/a5c"),
                this.GeographicalBorder6 = content.Load<Texture2D>("Border/a6c"),
            };

            this.OuterBorder = content.Load<Texture2D>("Border/outer");

            this.Blank = this.Create(1, 1).With(tex => tex.SetData(Color.White.IntoArray()));
        }

        public Texture2D Create(int width, int height) =>
            new Texture2D(this.GraphicsDevice, width, height);

        #endregion
    }
}