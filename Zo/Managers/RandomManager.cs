using Microsoft.Xna.Framework;
using System;
using Zo.Data;

namespace Zo.Managers
{
    public class RandomManager
    {
        #region Constructors

        public RandomManager(SizeManager sizes, Action<Action<GameTime>> subscribeToUpdate)
        {
            this.Sizes = sizes;
            this.Random = new Random();

            subscribeToUpdate(this.UpdateState);
        }

        #endregion

        #region Properties

        protected SizeManager Sizes { get; }

        protected Random Random { get; }

        #endregion

        #region Public Methods

        public Rgba FiefRgba() =>
            new Rgba
            (
                r: (uint) this.Random.Next(35, 230),
                g: (uint) this.Random.Next(35, 230),
                b: (uint) this.Random.Next(35, 230),
                a: 235u
            );

        public Rgba CellRgba() =>
            new Rgba
            (
                r: (uint) this.Random.Next(100, 240),
                g: (uint) this.Random.Next(100, 240),
                b: (uint) this.Random.Next(100, 240)
            );

        #endregion

        #region Protected Methods

        protected void UpdateState(GameTime gameTime)
        {
        }

        #endregion
    }
}