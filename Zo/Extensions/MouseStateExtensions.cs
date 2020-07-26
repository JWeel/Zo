using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using Zo.Enums;

namespace Zo.Extensions
{
    public static class MouseStateExtensions
    {
        /// <summary> Returns the <see cref="ButtonState"/> corresponding to a given <see cref="MouseButton"/>. </summary>
        public static ButtonState GetButtonState(this MouseState ms, MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return ms.LeftButton;
                case MouseButton.Middle:
                    return ms.MiddleButton;
                case MouseButton.Right:
                    return ms.RightButton;
                case MouseButton.Four:
                    return ms.XButton1;
                case MouseButton.Five:
                    return ms.XButton2;
                default:
                    throw new ArgumentException(nameof(button));
            }
        }

        /// <summary> Determines whether the button is pressed. </summary>
        public static bool IsPressed(this ButtonState buttonState) =>
            (buttonState == ButtonState.Pressed);

        /// <summary> Determines whether the button is released. </summary>
        public static bool IsReleased(this ButtonState buttonState) =>
            (buttonState == ButtonState.Released);

        public static Point ToPoint(this MouseState mouseState) =>
            new Point(mouseState.X, mouseState.Y);

        public static Vector2 ToVector2(this MouseState mouseState) =>
            new Vector2(mouseState.X, mouseState.Y);
    }
}