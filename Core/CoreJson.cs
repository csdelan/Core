using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Json
{
    /// <summary>
    /// The single, canonical source of <see cref="JsonSerializerOptions"/> for the TradingSystem
    /// ecosystem. Every layer that reads or writes JSON — on-disk config, the JSON document store, ZMQ
    /// wire payloads — shares these frozen instances so that enum, decimal, and date formatting can never
    /// silently drift between producers and consumers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Canonical policy (the authoritative contract other layers must mirror).</b> The options are based
    /// on <see cref="JsonSerializerDefaults.Web"/> and then constrained as follows:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Property names:</b> camelCase on write, case-insensitive on read (both Web defaults). This
    /// round-trips files that were already written with plain Web defaults, so existing on-disk data keeps
    /// deserializing.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Enums:</b> serialized as strings via <see cref="JsonStringEnumConverter"/>, with member names
    /// preserved exactly as authored (PascalCase). No camelCase naming policy is applied to enum values,
    /// because existing data on disk holds PascalCase enum strings and changing that would break reads.
    /// Reads are case-insensitive (e.g. <c>"planning"</c> deserializes to <c>Planning</c>).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Decimal:</b> handled as full-precision <see cref="decimal"/>, never routed through
    /// <see cref="double"/>. These values represent money and quantities and must stay lossless. (A future
    /// Core.Persistence BSON convention maps this to <c>Decimal128</c>; keeping the JSON side exact is what
    /// lets the two agree.)
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>DateTimeOffset / DateTime:</b> ISO-8601 round-trip (the System.Text.Json default). Callers are
    /// expected to store UTC instants; this pairs with the codebase standardizing on a single UTC clock.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Numbers:</b> the Web default <see cref="JsonNumberHandling.AllowReadingFromString"/> is kept, so
    /// numbers quoted as strings still read.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Nulls, trailing commas, comments:</b> System.Text.Json defaults are kept (no deviations).
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <b>Immutability.</b> <see cref="Default"/> and <see cref="Indented"/> are frozen with
    /// <see cref="JsonSerializerOptions.MakeReadOnly(bool)"/>, so callers cannot mutate shared state and
    /// System.Text.Json can cache the type metadata. To add a converter or otherwise tweak the policy, take
    /// a mutable copy with <see cref="CreateOptions"/> (or <c>new JsonSerializerOptions(CoreJson.Default)</c>)
    /// and modify that copy.
    /// </para>
    /// <para>
    /// <b>Usage.</b> Use <see cref="Default"/> for wire and storage (compact, machine-read). Use
    /// <see cref="Indented"/> for human-edited config files such as <c>system-config.json</c>. Use
    /// <see cref="CreateOptions"/> when you need to extend the policy with an extra converter.
    /// </para>
    /// </remarks>
    public static class CoreJson
    {
        /// <summary>
        /// The canonical options for wire and storage: compact (no indentation). This is the forward-looking
        /// default, because most JSON in the system is machine-read. Frozen and safe to share.
        /// </summary>
        public static JsonSerializerOptions Default { get; } = BuildPolicy(writeIndented: false);

        /// <summary>
        /// The canonical options for human-edited config files: identical policy to <see cref="Default"/>
        /// but with <see cref="JsonSerializerOptions.WriteIndented"/> set, so the output is pretty-printed.
        /// Frozen and safe to share.
        /// </summary>
        public static JsonSerializerOptions Indented { get; } = BuildPolicy(writeIndented: true);

        /// <summary>
        /// Returns a fresh, <b>mutable</b> copy of the canonical (compact) policy that callers can extend —
        /// for example by adding a converter — without touching the shared frozen instances.
        /// </summary>
        /// <returns>A new <see cref="JsonSerializerOptions"/> carrying the canonical policy.</returns>
        /// <remarks>
        /// Equivalent to <c>new JsonSerializerOptions(CoreJson.Default)</c>. Set
        /// <see cref="JsonSerializerOptions.WriteIndented"/> on the copy if you also need indentation.
        /// </remarks>
        public static JsonSerializerOptions CreateOptions() => new(Default);

        /// <summary>
        /// Builds the canonical policy and freezes it. Both presets flow through here so they share a single
        /// definition of the policy and differ only in whitespace.
        /// </summary>
        private static JsonSerializerOptions BuildPolicy(bool writeIndented)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = writeIndented,
                // Enums as strings with member names preserved as-authored (PascalCase); reads are
                // case-insensitive. A null naming policy means "do not rename enum members".
                Converters = { new JsonStringEnumConverter() },
            };

            // Freeze so callers cannot mutate shared state and STJ can cache metadata. Populate the default
            // reflection-based resolver since this options set is used without source generation.
            options.MakeReadOnly(populateMissingResolver: true);
            return options;
        }
    }
}
