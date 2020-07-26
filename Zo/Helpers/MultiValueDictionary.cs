using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Zo.Extensions;

namespace Zo.Helpers
{
    /// <summary> Represents a collection of keys and sequences of values. </summary>
    /// <remarks> When accessing values through the indexer, a default value is returned when the key is not found.
    /// <br/> This means the <see langword="+="/> operator can be used without first initializing the key.
    /// <br/> The <see cref="Contains(TKey)"/> method can be used to determine whether a key already exists. </remarks>
    [DebuggerDisplay("Count = {Count}")]
    public class MultiValueDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, MultiValue<TValue>>>, IDictionary<TKey, MultiValue<TValue>>
    {
        #region Constructors

        /// <summary> Initializes a new collection of keys and sequences of values. The instance will use the default equality comparer for the key type. </summary>
        public MultiValueDictionary() => this.Entries = new Dictionary<TKey, MultiValue<TValue>>();

        /// <summary> Initializes a new collection of keys and sequences of values. The instance will use a specified equality comparer for the key type. </summary>
        public MultiValueDictionary(IEqualityComparer<TKey> comparer) => this.Entries = new Dictionary<TKey, MultiValue<TValue>>(comparer);

        #endregion

        #region Properties

        /// <summary> Gets or sets one or more values associated with the specified key.
        /// <para/> Supports the <see langword="+="/> and <see langword="-="/> operators to add or remove elements in a sequence, and the <see langword="="/> operator to replace a sequence. </summary>
        public MultiValue<TValue> this[TKey key]
        {
            get
            {
                if (this.Entries.TryGetValue(key, out var value))
                    return value;
                else
                    return default(TValue);
            }
            set
            {
                this.Entries[key] = value;
            }
        }

        /// <summary> Contains the collection of keys and sequences of values. </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        protected Dictionary<TKey, MultiValue<TValue>> Entries { get; set; }

        /// <summary> Gets the number of keys contained in the sequence.
        /// <para/> To obtain the number of values, use <see cref="CountValues"/> instead. </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count => this.Entries.Count;

        #endregion

        #region Custom Dictionary Methods

        /// <summary> Computes the total number of values in the sequence by summing the counts of all individual value sequences. </summary>
        public int CountValues() => this.Entries.Values.Sum(x => x.Count());

        /// <summary> Determines whether the dictionary contains an entry with the specified key. </summary>
        public bool Contains(TKey key) => this.Entries.ContainsKey(key);

        #endregion

        #region ICollection and IDictionary Methods

        /// <summary> Adds the specified key and value to the dictionary. To add to an existing entry, use the indexer with <see langword="+="/> instead. </summary>
        public void Add(TKey key, MultiValue<TValue> value) => this.Entries.Add(key, value);

        /// <summary> Removes the specified key and its associated values from the dictionary. To remove a specific value, use the indexer with <see langword="-="/> instead. </summary>
        public bool Remove(TKey key) => this.Entries.Remove(key);

        /// <inheritdoc/>
        public void Clear() => this.Entries.Clear();

        /// <summary> Attempts to get the sequence of values associated with the specified key, and returns a boolean indicating whether or not the key was found. </summary>
        public bool TryGetValue(TKey key, out MultiValue<TValue> value) => this.Entries.TryGetValue(key, out value);

        #endregion

        #region IEnumerable Methods

        public IEnumerator<KeyValuePair<TKey, MultiValue<TValue>>> GetEnumerator() => this.Entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion

        #region Hidden ICollection and IDictionary Members

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection<KeyValuePair<TKey, MultiValue<TValue>>>.IsReadOnly => false;

        public ICollection<TKey> Keys => this.Entries.Keys;

        public ICollection<MultiValue<TValue>> Values => this.Entries.Values;

        void ICollection<KeyValuePair<TKey, MultiValue<TValue>>>.Add(KeyValuePair<TKey, MultiValue<TValue>> item)
            => this.Entries.Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, MultiValue<TValue>>>.Contains(KeyValuePair<TKey, MultiValue<TValue>> item)
            => this.Entries.Contains(item);

        void ICollection<KeyValuePair<TKey, MultiValue<TValue>>>.CopyTo(KeyValuePair<TKey, MultiValue<TValue>>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, MultiValue<TValue>>>) this.Entries).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<TKey, MultiValue<TValue>>>.Remove(KeyValuePair<TKey, MultiValue<TValue>> item)
            => this.Entries.Remove(item.Key);

        bool IDictionary<TKey, MultiValue<TValue>>.ContainsKey(TKey key) => this.Contains(key);

        #endregion
    }

    /// <summary> Provides a mechanism for working with multiple values. </summary>
    public class MultiValue<TValue> : IEnumerable<TValue>
    {
        #region Constructors

        /// <summary> Creates an instance encapsulating a sequence with one element. </summary>
        protected MultiValue(TValue value) => this.Values = value.Yield();

        /// <summary> Creates an instance encapsulating a sequence of arbitrary size. </summary>
        protected MultiValue(IEnumerable<TValue> collection) => this.Values = collection;

        #endregion

        #region Properties

        /// <summary> A sequence of values of arbitrary size. </summary>
        protected IEnumerable<TValue> Values { get; set; }

        #endregion

        #region +/- Operators

        /// <summary> Adds the given value to the sequence. </summary>
        public static MultiValue<TValue> operator +(MultiValue<TValue> left, TValue right)
            => new MultiValue<TValue>(left.Values.Concat(right));

        /// <summary> Adds the given values to the sequence. </summary>
        public static MultiValue<TValue> operator +(MultiValue<TValue> left, IEnumerable<TValue> right)
            => new MultiValue<TValue>(left.Values.Concat(right));

        /// <summary> Adds the given value(s) to the sequence. </summary>
        public static MultiValue<TValue> operator +(MultiValue<TValue> left, MultiValue<TValue> right)
            => new MultiValue<TValue>(left.Values.Concat(right.Values));

        /// <summary> Removes all elements from the sequence that match the given value using default equality comparison.
        /// <para/> If no matches are found, the sequence remains unchanged. </summary>
        public static MultiValue<TValue> operator -(MultiValue<TValue> left, TValue right)
            => new MultiValue<TValue>(left.Values.Except(right));

        /// <summary> Removes all elements from the sequence that match the given values using default equality comparison.
        /// <para/> If no matches are found, the sequence remains unchanged. </summary>
        public static MultiValue<TValue> operator -(MultiValue<TValue> left, IEnumerable<TValue> right)
            => new MultiValue<TValue>(left.Values.Except(right));

        /// <summary> Removes all elements from the sequence that match the given value(s) using default equality comparison.
        /// <para/> If no matches are found, the sequence remains unchanged. </summary>
        public static MultiValue<TValue> operator -(MultiValue<TValue> left, MultiValue<TValue> right)
            => new MultiValue<TValue>(left.Values.Except(right));

        #endregion

        #region Implicit Conversion

        /// <summary> Defines an implicit conversion of a list of <typeparamref name="TValue"/> to <see cref="MultiValue{}"/>. </summary>
        public static implicit operator MultiValue<TValue>(List<TValue> list)
            => new MultiValue<TValue>(list);

        // cannot do IEnumerable because interfaces are not allowed in user-defined conversions
        /// <summary> Defines an implicit conversion of <typeparamref name="TValue"/>[] to <see cref="MultiValue{}"/>. </summary>
        public static implicit operator MultiValue<TValue>(TValue[] array)
            => new MultiValue<TValue>(array);

        /// <summary> Defines an implicit conversion of <typeparamref name="TValue"/> to <see cref="MultiValue{}"/>. </summary>
        public static implicit operator MultiValue<TValue>(TValue value)
            => new MultiValue<TValue>(value);

        #endregion

        #region IEnumerable Methods

        public IEnumerator<TValue> GetEnumerator() => this.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion
    }
}