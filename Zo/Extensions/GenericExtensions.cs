using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Zo.Extensions
{
    public static class GenericExtensions
    {
        public static TNext Into<T, TNext>(this T instance, Func<T, TNext> next) =>
            next(instance);

        public static void Into<T>(this T instance, Action<T> action) =>
            action(instance);

        /// <summary> Returns this instance after passing it as argument to the invocation of a given <paramref name="action"/>. </summary>
        /// <remarks> The instance will be returned even if <paramref name="action"/> is null (in which case it will not be invoked). </remarks>
        [DebuggerStepThrough]
        public static T With<T>(this T value, Action<T> action)
        {
            action?.Invoke(value);
            return value;
        }

        public static T[] IntoArray<T>(this T instance) =>
            new T[] { instance };

        public static List<T> IntoList<T>(this T instance) =>
            new List<T> { instance };

        public static HashSet<T> IntoSet<T>(this T instance) =>
            new HashSet<T> { instance };

        public static Queue<T> IntoQueue<T>(this T instance) =>
            new Queue<T>().With(queue => queue.Enqueue(instance));

        public static Stack<T> IntoStack<T>(this T instance) =>
            new Stack<T>().With(stack => stack.Push(instance));

        public static IEnumerable<KeyValuePair<int, T>> WithIndex<T>(this IEnumerable<T> source) =>
            source.Select((element, index) => KeyValuePair.Create(index, element));

        public static void Each<T>(this IEnumerable<T> source, Action<T> effect)
        {
            foreach (var element in source)
                effect(element);
        }

        public static void Each<T>(this IEnumerable<T> source, Action<T> headEffect, Action<T> tailEffect)
        {
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            headEffect?.Invoke(enumerator.Current);

            while (enumerator.MoveNext())
                tailEffect?.Invoke(enumerator.Current);
        }

        /// <summary> Projects each element of a sequence into a new form using one selector for the first element and a different one for all remaining elements. </summary>
        public static IEnumerable<TNext> Select<T, TNext>(this IEnumerable<T> source, Func<T, TNext> headSelector, Func<T, TNext> tailSelector)
        {
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                yield break;
            yield return headSelector(enumerator.Current);
            while (enumerator.MoveNext())
                yield return tailSelector(enumerator.Current);
        }

        public static IEnumerable<TNext> SelectWithPrevious<T, TNext>(this IEnumerable<T> source, Func<T, TNext> firstSelector, Func<T, TNext, TNext> withPreviousSelector)
        {
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                yield break;
            var current = firstSelector(enumerator.Current);
            yield return current;
            while (enumerator.MoveNext())
                yield return current = withPreviousSelector(enumerator.Current, current);
        }

        /// <summary> Extracts from an <see cref="Expression{}"/> pointing to a property the property name, a <see cref="Func{,}"/> for getting its value and an <see cref="Action{,}"/> for setting the value.
        /// <para/> The expression should look like <c>x =&gt; x.Property</c>. </summary>
        /// <param name="propertyExpression"> The expression that should look like <c>x =&gt; x.Property</c>. </param>
        /// <exception cref="InvalidOperationException"> Member in expression is not a property. </exception>
        /// <exception cref="MissingMethodException"> Property does not have a setter. </exception>
        public static (string PropertyName, Func<T, TProperty> PropertyGetter, Action<T, TProperty> PropertySetter)
            ExtractPropertyHandlers<T, TProperty>(this Expression<Func<T, TProperty>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            var property = memberExpression?.Member as PropertyInfo ?? throw new InvalidOperationException("Member in expression is not a property.");
            var propertyName = property.Name;
            var propertyGetter = propertyExpression.Compile();
            var propertySetMethod = property.GetSetMethod(nonPublic: true) ?? throw new MissingMethodException("Property does not have a setter.");
            var propertySetter = (Action<T, TProperty>) Delegate.CreateDelegate(typeof(Action<T, TProperty>), propertySetMethod);
            return (propertyName, propertyGetter, propertySetter);
        }

        /// <summary> Extracts from an <see cref="Expression{}"/> pointing to a property the property name, a <see cref="Func{,}"/> for getting its value and an <see cref="Action{,}"/> for setting the value.
        /// <para/> The expression should look like <c>x =&gt; x.Property</c>. </summary>
        /// <param name="propertyExpression"> The expression that should look like <c>x =&gt; x.Property</c>. </param>
        /// <exception cref="InvalidOperationException"> Member in expression is not a property. </exception>
        /// <exception cref="MissingMethodException"> Property does not have a setter. </exception>
        public static (string PropertyName, Func<T, TProperty> PropertyGetter, Action<T, TProperty> PropertySetter)
            ExtractPropertyHandlers<T, TProperty>(this T _, Expression<Func<T, TProperty>> expression) =>
            expression.ExtractPropertyHandlers();

        public static void ChangeProperty<T, TProperty>(this T instance, Expression<Func<T, TProperty>> expression, Func<TProperty, TProperty> function) =>
            expression
                .ExtractPropertyHandlers()
                .Into(x => x.PropertySetter(instance, function(x.PropertyGetter(instance))));

        public static float AddWithLowerLimit(this float source, float amount, float lowerLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit: lowerLimit);

        public static float AddWithUpperLimit(this float source, float amount, float upperLimit) =>
            source.AddWithLimitsImplementation(amount, upperLimit: upperLimit);

        public static float AddWithLimits(this float source, float amount, float lowerLimit, float upperLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit, upperLimit);

        private static float AddWithLimitsImplementation(this float source, float amount, float? lowerLimit = default, float? upperLimit = default)
        {
            var value = source + amount;
            if (lowerLimit.HasValue && (value < lowerLimit.Value))
                return lowerLimit.Value;
            if (upperLimit.HasValue && (value > upperLimit.Value))
                return upperLimit.Value;
            return value;
        }

        public static int AddWithLowerLimit(this int source, int amount, int lowerLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit: lowerLimit);

        public static int AddWithUpperLimit(this int source, int amount, int upperLimit) =>
            source.AddWithLimitsImplementation(amount, upperLimit: upperLimit);

        public static int AddWithLimits(this int source, int amount, int lowerLimit, int upperLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit, upperLimit);

        private static int AddWithLimitsImplementation(this int source, int amount, int? lowerLimit = default, int? upperLimit = default)
        {
            var value = source + amount;
            if (lowerLimit.HasValue && (value < lowerLimit.Value))
                return lowerLimit.Value;
            if (upperLimit.HasValue && (value > upperLimit.Value))
                return upperLimit.Value;
            return value;
        }

        /// <summary> Changes the reference of this variable to a provided alternate value if the original value is <see langword="null"/>. </summary>
        public static void DefaultTo<T>(this ref T? value, T alternate)
            where T : struct
        {
            if (value == null)
                value = alternate;
        }

        #region Is Null Or Empty Or WhiteSpace

        /// <summary> Indicates whether this object is <see langword="null"/>. </summary>
        public static bool IsNull<T>(this T value) where T : class => (null == value);

        /// <summary> Indicates whether this nullable struct is <see langword="null"/>. </summary>
        public static bool IsNull<T>(this T? value) where T : struct => (null == value);

        /// <summary> Indicates whether this string is <see langword="null"/> or <see cref="string.Empty"/>. </summary>
        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);

        /// <summary> Indicates whether this string is <see langword="null"/>, <see cref="string.Empty"/>, or consists only of white-space characters. </summary>
        public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        /// <summary> Indicates whether this object is not <see langword="null"/>. </summary>
        public static bool IsNotNull<T>(this T value) where T : class => (null != value);

        /// <summary> Indicates whether this nullable struct is not <see langword="null"/>. </summary>
        public static bool IsNotNull<T>(this T? value) where T : struct => (null != value);

        /// <summary> Indicates whether this string is neither <see langword="null"/> nor <see cref="string.Empty"/>. </summary>
        public static bool IsNotNullNorEmpty(this string value) => !value.IsNullOrEmpty();

        /// <summary> Indicates whether this string is not <see langword="null"/>, not <see cref="string.Empty"/>, or does not consist only of white-space characters. </summary>
        public static bool IsNotNullNorWhiteSpace(this string value) => !value.IsNullOrWhiteSpace();

        #endregion

        #region Into Enumerable (Yield)

        /// <summary> Returns this <paramref name="value"/> wrapped inside an <see cref="IEnumerable{T}"/>. </summary> 
        public static IEnumerable<T> Yield<T>(this T value) { yield return value; }

        #endregion

        #region Concat

        /// <summary> Concatenates this sequence and a specified value. </summary>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> left, T right) =>
            left.Concat(right.Yield());

        /// <summary> Creates a sequence out of the concatenation of this value and a specified sequence. </summary>
        public static IEnumerable<T> YieldWith<T>(this T left, IEnumerable<T> right) =>
            left.Yield().Concat(right);

        /// <summary> Creates a sequence out of these values. </summary>
        public static IEnumerable<T> YieldWith<T>(this T left, params T[] right) =>
            left.Yield().Concat(right);

        #endregion

        #region Except

        /// <summary> Returns elements in the sequence that are not equal to a specified value, using default equality comparison. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T except) =>
            source.Where(x => !x.Equals(except));

        /// <summary> Returns elements in the sequence that are not equal to a specified value, using specified equality comparison. </summary>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T except, IEqualityComparer<T> equalityComparer) =>
            source.Where(x => !equalityComparer.Equals(x, except));

        #endregion

        #region Case

        /// <summary> Invokes a given action only if this <paramref name="condition"/> is <see langword="true"/>. </summary>
        public static void Case(this bool condition, Action action)
        {
            if (condition) action?.Invoke();
        }

        #endregion

        #region Get Enum Values

        /// <summary> Retrieves an array of the values defined for this enum type. </summary>
        public static TEnum[] GetValues<TEnum>(this TEnum value) where TEnum : Enum =>
            (TEnum[]) Enum.GetValues(typeof(TEnum));

        #endregion

        #region Least

        public static int TakeSmallest(this int source, int other) =>
            (source > other) ? other : source;

        #endregion

        #region In

        /// <summary> Determines whether this value is equal to any parameter. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, params T[] allowedValues) =>
            value.In((IEnumerable<T>) allowedValues);

        /// <summary> Determines whether this value is equal to any value in an enumerable. </summary>
        /// <remarks> Uses <see cref="EqualityComparer{T}.Default"/> to compare values. This property returns a static readonly member, and is thus created only once. </remarks>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, IEnumerable<T> allowedValues) =>
            value.In(allowedValues, EqualityComparer<T>.Default);

        /// <summary> Determines whether this value is equal to any parameter by using a specified <see cref="IEqualityComparer{}"/>. </summary>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, IEqualityComparer<T> comparer, params T[] allowedValues) =>
            value.In(allowedValues, comparer);

        /// <summary> Determines whether this value is equal to any value in an enumerable by using a specified <see cref="IEqualityComparer{}"/>. </summary>
        /// <remarks> Uses <see cref="EqualityComparer{T}.Default"/> to compare values. This property returns a static readonly member, and is thus created only once. </remarks>
        /// <exception cref="ArgumentNullException"> Parameter <paramref name="allowedValues"/> is null. </exception>
        public static bool In<T>(this T value, IEnumerable<T> allowedValues, IEqualityComparer<T> comparer) =>
            allowedValues.Contains(value, comparer);

        #endregion

        #region Join

        /// <summary> Concatenates the members of the <see cref="string"/> collection, using the specified separator between each member. </summary>
        public static string Join(this IEnumerable<string> strings, string separator) =>
            string.Join(separator, strings);

        #endregion

        #region As Key

        public static TValue AsKey<TKey, TValue>(this TKey key, IReadOnlyDictionary<TKey, TValue> dictionary) =>
            dictionary[key];

        #endregion

        #region Has Value

        public static bool HasValue<T>(this T value) where T : class =>
            (value != null);

        #endregion

        #region Remove Last

        public static void RemoveLast<T>(this IList<T> source)
        {
            if (source.Count == 0)
                return;
            source.RemoveAt(source.Count - 1);
        }

        #endregion
    }
}