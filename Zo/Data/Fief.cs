using System.Collections.Generic;

namespace Zo.Data
{
    public class Fief
    {
        #region Constructors

        public Fief()
        {
            this.Cells = new List<Cell>();
        }

        #endregion

        #region Members

        protected Region Region { get; }
        protected List<Cell> Cells { get; }

        #endregion

        #region Methods

        #endregion
    }
}