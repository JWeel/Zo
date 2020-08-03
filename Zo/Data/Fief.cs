using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Zo.Data
{
    public class Fief
    {
        #region Constructors

        public Fief(string name, Region region)
        {
            this.Name = name;
            this.Region = region;
            this.Cells = new List<Cell>();

            var random = new Random();
            var randomRgba = new Rgba((uint) random.Next(35, 230), (uint) random.Next(35, 230), (uint) random.Next(35, 230), 235u);
            this.Rgba = randomRgba;
            this.Color = (Color) randomRgba;
        }

        #endregion

        #region Members

        public string Name { get; protected set; }

        public Region Region { get; protected set; }

        public Rgba Rgba { get; }

        public Color Color { get; }

        protected List<Cell> Cells { get; }

        #endregion

        #region Methods

        public void UpdateRegion(Region region)
        {
            this.Region = region;
        }

        #endregion
    }
}