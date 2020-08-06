using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Zo.Data
{
    public class Fief
    {
        #region Constructors

        public Fief(string name, Rgba rgba, Region region)
        {
            this.Name = name;
            this.Region = region;
            this.Rgba = rgba;
            this.Color = (Color) rgba;
            
            this.Cells = new List<Cell>();
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