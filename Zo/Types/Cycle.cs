using Zo.Extensions;

namespace Zo.Types
{
    public class Cycle<T>
    {
        #region Constructors

        public Cycle(params T[] values)
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

        // (++this.Index >= this.Values.Length).Case(() => this.Index = 0);
        // ^ this is nice but race condition when Index is accessed after increment before reset
        public void Advance()
        {
            var index = this.Index + 1;
            if (index >= this.Values.Length)
                index = 0;
            this.Index = index;
        }

        public void Reverse()
        {
            var index = this.Index - 1;
            if (index < 0)
                index = this.Values.Length - 1;
            this.Index = index;
        }

        public void Reset() =>
            this.Index = 0;

        #endregion
    }
}