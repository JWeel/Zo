using System.Collections.Generic;
using System.Linq;

namespace Zo.Data
{
    public class RegionMapper
    {
        #region Constructors

        public RegionMapper(Rgba[,] rgbaByPixelPosition, IReadOnlyDictionary<Rgba, Region> regionByRgba)
        {
            this.RgbaByPixelPosition = rgbaByPixelPosition;
            this.RegionByRgba = regionByRgba;
            this.Regions = regionByRgba.Values.ToArray();
        }

        #endregion

        #region Members

        public Rgba[,] RgbaByPixelPosition { get; }

        public IReadOnlyDictionary<Rgba, Region> RegionByRgba { get; }

        public Region[] Regions { get; }

        #endregion

        #region Methods

        #endregion
    }
}