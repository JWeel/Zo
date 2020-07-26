using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Zo.Types
{
    public class Button
    {
        #region Constructors

        public Button(Vector2 position, Action onClick)
        {
            this.Position = position;
            this.OnClick = onClick;
        }

        #endregion

        #region Properties

        public Vector2 Position { get; protected set; }

        public Action OnClick { get; protected set; }

        public Texture2D Texture { get; protected set; }

        #endregion

        #region Methods

        public void Click() =>
            this.OnClick?.Invoke();

        #endregion
    }
}