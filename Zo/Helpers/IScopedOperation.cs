namespace Zo.Helpers
{
    /// <summary> Defines two methods that should be performed around a scope. </summary>
    public interface IScopedOperation
    {
        #region Methods

        void PreOperation();
        void PostOperation();

        #endregion
    }
}