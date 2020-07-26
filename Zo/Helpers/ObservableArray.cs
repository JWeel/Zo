using System;
namespace Zo.Helpers
{
    public class ObservableArray<T>
    {
        #region Constructors

        public ObservableArray() =>
            this.Content = Array.Empty<T>();

        public ObservableArray(int length) =>
            this.Content = new T[length];

        public ObservableArray(T[] array) =>
            this.Content = array;

        #endregion

        #region Members

        protected T[] Content;

        public event Action<int, T> OnChange;

        #endregion

        #region Methods

        public T this[int index]
        {
            get => this.Content[index];
            set
            {
                this.Content[index] = value;
                this.OnChange?.Invoke(index, value);
            }
        }

        #endregion
    }
}