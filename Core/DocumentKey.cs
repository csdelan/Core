using System.Globalization;
using System.Linq.Expressions;

namespace Core
{
    /// <summary>
    /// Helpers that turn an id-member expression into the two things a document store needs — a runtime
    /// key selector and the member's name — from a single source, plus key validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A document store must agree on a single notion of "the id". For MongoDB this is doubly true: the
    /// value used to build the upsert filter (<c>_id == x</c>) must match the member mapped to <c>_id</c>
    /// during serialization, or upserts silently become duplicate inserts. Deriving both the runtime
    /// selector (<see cref="SelectorFromExpression{T}"/>) and the member name (<see cref="MemberName{T}"/>)
    /// from the same <see cref="Expression"/> makes that disagreement impossible by construction.
    /// </para>
    /// </remarks>
    public static class DocumentKey
    {
        /// <summary>
        /// Compiles an id-member expression into a selector that extracts the string key from an entity.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="idMember">
        /// An expression selecting the id member, e.g. <c>x =&gt; x.Symbol</c> or <c>x =&gt; x.TradeId</c>.
        /// Non-string members are converted using the invariant culture.
        /// </param>
        /// <returns>A selector that returns the entity's key as a string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="idMember"/> is <c>null</c>.</exception>
        public static Func<T, string> SelectorFromExpression<T>(Expression<Func<T, object>> idMember)
        {
            ArgumentNullException.ThrowIfNull(idMember);

            Func<T, object> compiled = idMember.Compile();
            return entity =>
            {
                object? value = compiled(entity);
                return value switch
                {
                    null => string.Empty,
                    string s => s,
                    IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                    _ => value.ToString() ?? string.Empty,
                };
            };
        }

        /// <summary>
        /// Extracts the CLR member name targeted by an id-member expression.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="idMember">An expression selecting the id member.</param>
        /// <returns>The name of the property or field the expression points at.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="idMember"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// The expression body is not a simple member access (e.g. it is a method call or a computed value).
        /// </exception>
        public static string MemberName<T>(Expression<Func<T, object>> idMember)
        {
            ArgumentNullException.ThrowIfNull(idMember);

            // A value-type member is wrapped in a Convert(...) to object; unwrap it first.
            Expression body = idMember.Body;
            if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
                body = unary.Operand;

            if (body is MemberExpression member)
                return member.Member.Name;

            throw new ArgumentException(
                $"Expression '{idMember}' must select a single property or field (e.g. x => x.Id).",
                nameof(idMember));
        }

        /// <summary>
        /// Validates a resolved key, returning it unchanged when valid.
        /// </summary>
        /// <param name="key">The key to validate.</param>
        /// <param name="paramName">The calling parameter name, for diagnostics.</param>
        /// <returns>The validated, non-empty key.</returns>
        /// <exception cref="ArgumentException"><paramref name="key"/> is <c>null</c>, empty, or whitespace.</exception>
        public static string Validate(string? key, string paramName = "id")
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A document key must be a non-empty, non-whitespace string.", paramName);

            return key;
        }
    }
}
