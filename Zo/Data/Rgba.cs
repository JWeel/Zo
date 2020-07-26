using System;
using Microsoft.Xna.Framework;

namespace Zo.Data
{
    /// <summary> Color encoded in 32-bit RGBA color model. </summary>
    public struct Rgba
    {
        #region Constants

        /// <summary> Highest 0-indexed value of encoded color. </summary>
        private const byte COLOR_DEPTH = byte.MaxValue;

        private const float MAX_VALUE_FLOAT = 1f;

        private const string MAX_VALUE_HEX = "FF";
        
        private const string HEX_FORMAT = "X8";

        #endregion

        #region Constructors

        public Rgba(uint value) => this.Value = value;

        public Rgba(uint r, uint g, uint b, uint a = COLOR_DEPTH)
        : this(r << 24 | g << 16 | b << 8 | a)
        {
        }

        public Rgba(float r, float g, float b, float a = MAX_VALUE_FLOAT)
        : this((uint) r * COLOR_DEPTH, (uint) g * COLOR_DEPTH, (uint) b * COLOR_DEPTH, (uint) a * COLOR_DEPTH)
        {
        }

        public Rgba(string hex)
        : this(uint.Parse((hex.Length == 6) ? hex + MAX_VALUE_HEX : hex, System.Globalization.NumberStyles.HexNumber))
        {
        }

        public Rgba(Color color)
        : this(color.R, color.G, color.B, color.A)
        {
        }

        #endregion

        #region Properties

        /// <summary> The encoded value of a color. </summary>
        public uint Value { get; }

        /// <summary> The R value (Red) of the RGBA encoded color. </summary>
        public uint Red => this.Value >> 24 & COLOR_DEPTH;

        /// <summary> The G value (Green) of the RGBA encoded color. </summary>
        public uint Green => this.Value >> 16 & COLOR_DEPTH;

        /// <summary> The B value (Blue) of the RGBA encoded color. </summary>
        public uint Blue => this.Value >> 8 & COLOR_DEPTH;

        /// <summary> The A value (Alpha) Value the RGBA encoded color. </summary>
        public uint Alpha => this.Value & COLOR_DEPTH;

        #endregion

        #region Operators

        public static bool operator ==(Rgba left, Rgba right) => (left.Value == right.Value);
        public static bool operator !=(Rgba left, Rgba right) => (left.Value != right.Value);

        public static explicit operator Rgba(uint i) => new Rgba(i);
        public static explicit operator Color(Rgba d) => new Color((int) d.Red, (int) d.Green, (int) d.Blue, (int) d.Alpha);

        #endregion

        #region Overriden Methods

        public override string ToString() => this.Value.ToString(HEX_FORMAT);

        public override bool Equals(object obj) => ((obj is Rgba rgba) && (rgba == this));

        public override int GetHashCode() => this.Value.GetHashCode();

        #endregion
    }
}