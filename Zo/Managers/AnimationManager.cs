using Microsoft.Xna.Framework;
using System;
using System.Threading;
using Zo.Data;
using Zo.Types;

namespace Zo.Managers
{
    public class AnimationManager
    {
        #region Constants

        private const int BLINK_INTERVAL = 450;

        private static readonly Rgba SELECTION_RGBA = new Rgba(250, 250, 250, 180);
        private static readonly Color SELECTION_COLOR = (Color) SELECTION_RGBA;

        #endregion

        #region Constructors

        public AnimationManager(Action<Action<GameTime>> subscribeToUpdate, Action<Action> subscribeToSelect)
        {
            subscribeToUpdate(this.HandleOnUpdate);
            subscribeToSelect(this.HandleOnSelect);
            this.SelectionColor = new Cycle<Color>(SELECTION_COLOR);//, Color.Transparent);
            this.AnimationTimer = new Timer(_ => this.SelectionColor.Advance(), state: null, dueTime: 0, period: BLINK_INTERVAL);
        }

        #endregion

        #region Properties

        protected Timer AnimationTimer { get; set; }

        public Cycle<Color> SelectionColor { get; protected set; }

        #endregion

        #region Protected Methods

        protected void HandleOnUpdate(GameTime gameTime)
        {
        }

        protected void HandleOnSelect()
        {
            // delay next callback by interval amount
            this.AnimationTimer.Change(dueTime: BLINK_INTERVAL, period: BLINK_INTERVAL);
            this.SelectionColor.Reset();
        }

        #endregion
    }
}