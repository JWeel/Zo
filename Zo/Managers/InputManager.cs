using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using Zo.Enums;
using Zo.Extensions;

namespace Zo.Managers
{
    public class InputManager
    {
        #region Constructors

        public InputManager(SizeManager sizes, Action<Action> subscribeToUpdate)
        {
            subscribeToUpdate(this.UpdateState);
        }

        #endregion

        #region Properties

        protected KeyboardState LastKeyboardState { get; set; }

        public KeyboardState CurrentKeyboardState { get; protected set; }

        protected MouseState LastMouseState { get; set; }

        public MouseState CurrentMouseState { get; protected set; }

        #endregion

        #region Public Methods

        public bool KeyPressed(Keys key) =>
            this.LastKeyboardState.IsKeyUp(key)
                && this.CurrentKeyboardState.IsKeyDown(key);

        public bool KeysPressed(params Keys[] keys) => keys.All(this.KeyPressed);

        public bool KeysPressedAny(params Keys[] keys) => keys.Any(this.KeyPressed);

        public bool KeyDown(Keys key) => this.CurrentKeyboardState.IsKeyDown(key);

        public bool KeysDown(params Keys[] keys) => keys.All(this.KeyDown);

        public bool KeysDownAny(params Keys[] keys) => keys.Any(this.KeyDown);

        public bool MousePressed(MouseButton button) =>
            !this.LastMouseState.GetButtonState(button).IsPressed()
                && this.CurrentMouseState.GetButtonState(button).IsPressed();

        public bool MouseReleased(MouseButton button) =>
            !this.LastMouseState.GetButtonState(button).IsReleased()
                && this.CurrentMouseState.GetButtonState(button).IsReleased();

        public bool MouseScrolled() =>
            (this.LastMouseState.ScrollWheelValue != this.CurrentMouseState.ScrollWheelValue);

        public bool MouseScrolledUp() =>
            this.CurrentMouseState.ScrollWheelValue > this.LastMouseState.ScrollWheelValue;

        public bool MouseScrolledDown() =>
            this.CurrentMouseState.ScrollWheelValue < this.LastMouseState.ScrollWheelValue;

        #endregion

        #region Protected Methods

        protected void UpdateState()
        {
            this.LastKeyboardState = this.CurrentKeyboardState;
            this.CurrentKeyboardState = Keyboard.GetState();

            this.LastMouseState = this.CurrentMouseState;
            this.CurrentMouseState = Mouse.GetState();
        }

        #endregion
    }
}