using Zo.Extensions;

namespace Zo.Types
{
    public class Axis<T>
    {
        #region Constructors

        public Axis(params T[] values)
        {
            this.Index = 0;
            this.Values = values;
        }

        #endregion

        #region Properties

        protected T[] Values { get; set; }

        public int Index { get; protected set; }

        public T Value =>
            this.Values[this.Index];

        #endregion

        #region Methods

        public void Raise()
        {
            var index = this.Index + 1;
            if (index >= this.Values.Length)
                index = this.Values.Length - 1;
            this.Index = index;
        }

        public void Lower()
        {
            var index = this.Index - 1;
            if (index < 0)
                index = 0;
            this.Index = index;
        }

        #endregion
    }
}