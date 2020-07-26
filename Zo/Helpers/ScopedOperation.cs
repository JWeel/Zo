using System;

namespace Zo.Helpers
{    
    /// <summary> A wrapper of <see cref="Action"/> instances that should be performed around a scope. </summary>
    public class ScopedOperation : IScopedOperation
    {
        #region Constructors

        public ScopedOperation(Action preAction, Action postAction)
        {
            this.PreAction = preAction;
            this.PostAction = postAction;
        }

        #endregion

        #region Properties

        protected Action PreAction { get; }
        protected Action PostAction { get; }

        #endregion

        #region IScoped Implementation

        public void PreOperation() => this.PreAction?.Invoke();
        public void PostOperation() => this.PostAction?.Invoke();

        #endregion
    }
}