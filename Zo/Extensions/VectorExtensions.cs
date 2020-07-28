using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zo.Extensions
{
    public static class VectorExtensions
    {
        public static string PrintInt(this Vector2 value) =>
            $"({value.X:0},{value.Y:0})";

        public static string PrintFloat(this Vector2 value) =>
            $"({value.X:0.00},{value.Y:0.00})";

        public static T Item<T>(this T[,] array, Vector2 value) =>
            array[(int) value.X, (int) value.Y];

        public static T Item<T>(this T[] array, Vector2 value, int width) =>
            array[(int) value.X + ((int) value.Y) * width];

        public static Vector2 Floor(this Vector2 source) =>
            new Vector2((int) source.X, (int) source.Y);

        public static IEnumerable<(Vector2 Position, T Value)> In2D<T>(this T[] source, int divisor) =>
            source.Select((value, index) => (Position: new Vector2(index % divisor, index / divisor), Value: value));

        // TODO come up with better name. IsInside, Inside, ContainedIn, BoundBy
        public static bool LiesWithin(this Vector2 position, int width, int height) =>
            position.LiesWithin(default, width, height);

        public static bool LiesWithin(this Vector2 position, Vector2 offset, int width, int height) =>
            !((position.X < offset.X) || (position.Y < offset.Y) || (position.X >= width + offset.X) || (position.Y >= height + offset.Y));
    }
}