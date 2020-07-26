using System;
using System.Collections.Generic;
using System.Linq;
using Zo.Extensions;

namespace Zo.Helpers
{
    /// <summary> Provides a mechanism for performing operations before and after a <see langword="using"/> scope. </summary>
    public class Scope : IDisposable
    {
        #region Constructors

        /// <summary> Initializes a new instance with an arbitrary number of operations to perform around a <see langword="using"/> scope. </summary>
        public Scope(params IScopedOperation[] scopes)
        {
            this.Operations = new List<IScopedOperation>();
            try
            {
                scopes.Each(scope => scope.With(this.Operations.Add).PreOperation());
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        /// <summary> Initializes a new instance with actions to perform before and after a <see langword="using"/> scope. </summary>
        public Scope(Action preOperation, Action postOperation)
        : this(new ScopedOperation(preOperation, postOperation)) { }

        #endregion

        #region Properties

        // cant be List<> because List has a stupid void method called Reverse....
        protected IList<IScopedOperation> Operations { get; }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this.Operations.Reverse().Each(scope => scope.PostOperation());
        }

        #endregion
    }
}