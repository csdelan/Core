using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Core.Persistence
{
    /// <summary>
    /// Central, register-once MongoDB serialization setup shared by every <see cref="MongoDocumentStore{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Entities are typically defined in external packages that cannot be decorated with Bson attributes,
    /// so all mapping is configured here instead: enums as strings (to match a JSON
    /// <c>JsonStringEnumConverter</c>), <see cref="decimal"/> as <see cref="BsonType.Decimal128"/>
    /// (critical for money fields), and <see cref="DateTimeOffset"/> in a representation that round-trips
    /// its offset.
    /// </para>
    /// <para>
    /// Registration is idempotent and must happen before any (de)serialization. <see cref="EnsureRegistered"/>
    /// installs the global conventions and serializers once; <see cref="RegisterIdMap{T}"/> maps a chosen
    /// member to <c>_id</c> once per type. Both are safe to call repeatedly.
    /// </para>
    /// </remarks>
    public static class MongoConventions
    {
        private static int _registered;
        private static readonly object MapLock = new();
        private static readonly HashSet<Type> MappedTypes = [];

        /// <summary>
        /// Installs the shared conventions and serializers. Safe to call multiple times; only the first
        /// call has an effect.
        /// </summary>
        public static void EnsureRegistered()
        {
            if (Interlocked.CompareExchange(ref _registered, 1, 0) != 0)
                return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true),
            };
            ConventionRegistry.Register("CorePersistenceConventions", pack, _ => true);

            // decimal -> Decimal128 so money fields are exact (never binary double).
            BsonSerializer.TryRegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.TryRegisterSerializer(
                typeof(decimal?),
                new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

            // DateTimeOffset as [ticks, offsetMinutes] preserves the original offset on round-trip.
            BsonSerializer.TryRegisterSerializer(typeof(DateTimeOffset), new DateTimeOffsetSerializer(BsonType.Array));
            BsonSerializer.TryRegisterSerializer(
                typeof(DateTimeOffset?),
                new NullableSerializer<DateTimeOffset>(new DateTimeOffsetSerializer(BsonType.Array)));
        }

        /// <summary>
        /// Maps the member selected by <paramref name="idMember"/> to <c>_id</c> for type
        /// <typeparamref name="T"/>, storing it as a string so the generic string-keyed filters used by
        /// <see cref="MongoDocumentStore{T}"/> always match.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="idMember">An expression selecting the id member (e.g. <c>x =&gt; x.Symbol</c>).</param>
        /// <remarks>
        /// Idempotent per type and a no-op if a class map for <typeparamref name="T"/> is already registered
        /// (so a hand-written map takes precedence). Driven by the same expression the runtime key selector
        /// is compiled from, guaranteeing the upsert filter and the stored <c>_id</c> agree.
        /// </remarks>
        public static void RegisterIdMap<T>(Expression<Func<T, object>> idMember)
        {
            ArgumentNullException.ThrowIfNull(idMember);
            EnsureRegistered();

            Type type = typeof(T);
            lock (MapLock)
            {
                if (MappedTypes.Contains(type))
                    return;

                if (BsonClassMap.IsClassMapRegistered(type))
                {
                    MappedTypes.Add(type);
                    return;
                }

                string memberName = DocumentKey.MemberName(idMember);
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    BsonMemberMap? idMap = cm.GetMemberMap(memberName);
                    if (idMap is not null)
                    {
                        cm.SetIdMember(idMap);
                        idMap.SetElementName("_id");
                        if (idMap.MemberType != typeof(string) &&
                            idMap.GetSerializer() is IRepresentationConfigurable representable)
                        {
                            idMap.SetSerializer(representable.WithRepresentation(BsonType.String));
                        }
                    }
                });

                MappedTypes.Add(type);
            }
        }
    }
}
