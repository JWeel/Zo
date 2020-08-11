using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Zo.Extensions;

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

        public Rgba Rgba { get;protected set; }

        public Color Color { get;protected set; }

        protected List<Cell> Cells { get; }

        #endregion

        #region Methods

        public void UpdateRegion(Region region)
        {
            this.Region = region;
        }

        public void UpdateName(string name)
        {
            this.Name = name;
            this.Region = this.Region.CopyWith((x => x.Name, name));
        }

        public void UpdateRgba(Rgba rgba)
        {
            this.Rgba = rgba;
            this.Color = (Color) rgba;
            this.Region = this.Region.CopyWith((x => x.Rgba, rgba), (x => x.Color, (Color) rgba));
        }

        #endregion
    }
}