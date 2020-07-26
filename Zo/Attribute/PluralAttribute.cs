using System;

namespace Zo.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class PluralAttribute : Attribute
    {
        #region Constructors

        public PluralAttribute(string word)
        {
            this.Word = word;
        }

        #endregion

        #region Properties

        public string Word { get; protected set; }

        #endregion
    }
}