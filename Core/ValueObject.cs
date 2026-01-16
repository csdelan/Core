namespace Core
{
    /// <summary>
    /// Base class for implementing value objects with structural equality and comparison support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value object is an immutable object defined not by its identity but by its structural equality.
    /// Two value objects are equal if all their equality components are equal.
    /// </para>
    /// <para>
    /// Derived classes must implement <see cref="GetEqualityComponents"/> to define which properties
    /// contribute to equality and comparison operations.
    /// </para>
    /// <para>
    /// This implementation handles equality, hashing, and comparison, including support for
    /// ORM proxies (Entity Framework Core and NHibernate).
    /// </para>
    /// <para>
    /// For more information, see: https://enterprisecraftsmanship.com/posts/value-object-better-implementation/
    /// </para>
    /// </remarks>
    [Serializable]
    public abstract class ValueObject : IComparable, IComparable<ValueObject>
    {
        /// <summary>
        /// Cached hash code to avoid recalculating on subsequent calls.
        /// </summary>
        private int? _cachedHashCode;

        /// <summary>
        /// Returns the equality components that define equality for this value object.
        /// </summary>
        /// <returns>
        /// An enumerable of objects that represent the components used for equality comparison.
        /// Two value objects are equal if their equality components are equal.
        /// </returns>
        /// <remarks>
        /// Derived classes must implement this method to define which properties or values
        /// constitute this value object's identity.
        /// </remarks>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// Determines whether this value object is equal to the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this value object.</param>
        /// <returns>
        /// <c>true</c> if the specified object is a value object of the same type and has
        /// equivalent equality components; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method compares value objects by their equality components rather than by reference.
        /// It also handles ORM proxies to ensure proper comparison of proxied instances.
        /// </remarks>
        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            if (GetUnproxiedType(this) != GetUnproxiedType(obj))
                return false;

            var valueObject = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for this value object based on its equality components.
        /// </returns>
        /// <remarks>
        /// The hash code is calculated from the equality components using the polynomial
        /// rolling hash algorithm and is cached for performance.
        /// Equal value objects are guaranteed to have equal hash codes.
        /// </remarks>
        public override int GetHashCode()
        {
            if (!_cachedHashCode.HasValue)
            {
                _cachedHashCode = GetEqualityComponents()
                    .Aggregate(1, (current, obj) =>
                    {
                        unchecked
                        {
                            return current * 23 + (obj?.GetHashCode() ?? 0);
                        }
                    });
            }

            return _cachedHashCode.Value;
        }

        /// <summary>
        /// Compares this instance with a specified object.
        /// </summary>
        /// <param name="obj">
        /// An object to compare with this instance, or <c>null</c>.
        /// </param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared.
        /// Less than zero: This instance precedes <paramref name="obj"/> in the sort order.
        /// Zero: This instance has the same position in the sort order as <paramref name="obj"/>.
        /// Greater than zero: This instance follows <paramref name="obj"/> in the sort order.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If <paramref name="obj"/> is <c>null</c>, this method returns a value greater than zero.
        /// </para>
        /// <para>
        /// If the objects are of different types, they are ordered by their type names.
        /// If the objects are of the same type, their equality components are compared sequentially.
        /// </para>
        /// <para>
        /// This method handles ORM proxies to ensure proper comparison of proxied instances.
        /// </para>
        /// </remarks>
        public int CompareTo(object? obj)
        {
            if (obj == null) return 1; // or any other value that makes sense in your context

            Type thisType = GetUnproxiedType(this);
            Type otherType = GetUnproxiedType(obj);

            if (thisType != otherType)
                return string.Compare(thisType.ToString(), otherType.ToString(), StringComparison.Ordinal);

            var other = (ValueObject)obj;

            object[] components = GetEqualityComponents().ToArray();
            object[] otherComponents = other.GetEqualityComponents().ToArray();

            for (int i = 0; i < components.Length; i++)
            {
                int comparison = CompareComponents(components[i], otherComponents[i]);
                if (comparison != 0)
                    return comparison;
            }

            return 0;
        }

        /// <summary>
        /// Compares two equality components for ordering.
        /// </summary>
        /// <param name="object1">The first object to compare.</param>
        /// <param name="object2">The second object to compare.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared.
        /// Less than zero: <paramref name="object1"/> precedes <paramref name="object2"/> in the sort order.
        /// Zero: <paramref name="object1"/> has the same position as <paramref name="object2"/>.
        /// Greater than zero: <paramref name="object1"/> follows <paramref name="object2"/> in the sort order.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <c>null</c> values are considered less than non-<c>null</c> values.
        /// Two <c>null</c> values are considered equal.
        /// </para>
        /// <para>
        /// If both objects implement <see cref="IComparable"/>, their <see cref="IComparable.CompareTo"/>
        /// method is used for comparison. Otherwise, objects are compared by equality.
        /// </para>
        /// </remarks>
        private int CompareComponents(object object1, object object2)
        {
            if (object1 is null && object2 is null)
                return 0;

            if (object1 is null)
                return -1;

            if (object2 is null)
                return 1;

            if (object1 is IComparable comparable1 && object2 is IComparable comparable2)
                return comparable1.CompareTo(comparable2);

            return object1.Equals(object2) ? 0 : -1;
        }

        /// <summary>
        /// Compares this instance with another value object.
        /// </summary>
        /// <param name="other">The value object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared.
        /// Less than zero: This instance precedes <paramref name="other"/> in the sort order.
        /// Zero: This instance has the same position in the sort order as <paramref name="other"/>.
        /// Greater than zero: This instance follows <paramref name="other"/> in the sort order.
        /// </returns>
        /// <remarks>
        /// This method provides type-safe comparison for value objects.
        /// </remarks>
        public int CompareTo(ValueObject? other)
        {
            return CompareTo(other as object);
        }

        /// <summary>
        /// Determines whether two specified value objects are equal.
        /// </summary>
        /// <param name="a">The first value object to compare.</param>
        /// <param name="b">The second value object to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="a"/> and <paramref name="b"/> are equal; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This operator uses the <see cref="Equals(object)"/> method for comparison,
        /// properly handling <c>null</c> values.
        /// </remarks>
        public static bool operator ==(ValueObject a, ValueObject b)
        {
            if (a is null && b is null)
                return true;

            if (a is null || b is null)
                return false;

            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether two specified value objects are not equal.
        /// </summary>
        /// <param name="a">The first value object to compare.</param>
        /// <param name="b">The second value object to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="a"/> and <paramref name="b"/> are not equal; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This operator is the logical negation of the <see cref="operator ==(ValueObject, ValueObject)"/> operator.
        /// </remarks>
        public static bool operator !=(ValueObject a, ValueObject b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Gets the unproxied type of an object, removing ORM proxy types.
        /// </summary>
        /// <param name="obj">The object to get the unproxied type for.</param>
        /// <returns>
        /// The unproxied type of the object. If the object is a proxy instance created by
        /// Entity Framework Core or NHibernate, returns the base type; otherwise, returns the object's type.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is useful when comparing objects that may have been proxied by an ORM
        /// to ensure that the comparison is based on the actual type rather than the proxy type.
        /// </para>
        /// <para>
        /// Handles proxies from Entity Framework Core (Castle.Proxies prefix) and NHibernate (Proxy postfix).
        /// </para>
        /// </remarks>
        internal static Type GetUnproxiedType(object obj)
        {
            const string EFCoreProxyPrefix = "Castle.Proxies.";
            const string NHibernateProxyPostfix = "Proxy";

            Type type = obj.GetType();
            string typeString = type.ToString();

            if (typeString.Contains(EFCoreProxyPrefix) || typeString.EndsWith(NHibernateProxyPostfix))
                return type.BaseType ?? type;

            return type;
        }
    }
}
