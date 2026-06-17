using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Json;

namespace Core.Tests
{
    public class CoreJsonTest
    {
        public enum Phase
        {
            Planning,
            InProgress,
            Done,
        }

        private sealed class Poco
        {
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
            public decimal Price { get; set; }
            public Phase Phase { get; set; }
            public decimal? Stop { get; set; }
            public DateTimeOffset OpenedAt { get; set; }
        }

        private static Poco SamplePoco() => new()
        {
            Name = "AAPL",
            Count = 42,
            Price = 123.456789m,
            Phase = Phase.InProgress,
            Stop = null,
            OpenedAt = new DateTimeOffset(2026, 6, 16, 9, 30, 0, TimeSpan.FromHours(-5)),
        };

        private static void AssertPocoEqual(Poco expected, Poco? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Name, actual!.Name);
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(expected.Price, actual.Price);
            Assert.Equal(expected.Phase, actual.Phase);
            Assert.Equal(expected.Stop, actual.Stop);
            Assert.Equal(expected.OpenedAt, actual.OpenedAt);
            Assert.Equal(expected.OpenedAt.Offset, actual.OpenedAt.Offset);
        }

        [Fact]
        public void Default_RoundTrips_RepresentativePoco()
        {
            Poco original = SamplePoco();

            string json = JsonSerializer.Serialize(original, CoreJson.Default);
            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            AssertPocoEqual(original, result);
        }

        [Fact]
        public void Indented_RoundTrips_RepresentativePoco()
        {
            Poco original = SamplePoco();

            string json = JsonSerializer.Serialize(original, CoreJson.Indented);
            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Indented);

            AssertPocoEqual(original, result);
        }

        [Fact]
        public void DefaultAndIndented_ProduceEqualObjectGraphs()
        {
            Poco original = SamplePoco();

            string compact = JsonSerializer.Serialize(original, CoreJson.Default);
            string indented = JsonSerializer.Serialize(original, CoreJson.Indented);

            // Cross-parse: each preset can read what the other wrote, to the same graph.
            Poco? fromCompact = JsonSerializer.Deserialize<Poco>(compact, CoreJson.Indented);
            Poco? fromIndented = JsonSerializer.Deserialize<Poco>(indented, CoreJson.Default);

            AssertPocoEqual(original, fromCompact);
            AssertPocoEqual(original, fromIndented);
        }

        [Theory]
        [InlineData("0.1")]
        [InlineData("0.3")]
        [InlineData("79228162514264337593543950335")] // decimal.MaxValue
        [InlineData("0.0000000000000000000000000001")] // smallest positive decimal
        [InlineData("123.456789")]
        public void Default_PreservesDecimalPrecisionExactly(string literal)
        {
            decimal value = decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
            var original = new Poco { Price = value };

            string json = JsonSerializer.Serialize(original, CoreJson.Default);
            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            Assert.NotNull(result);
            Assert.Equal(value, result!.Price);
            // No double-rounding: the raw token must carry the exact digits, not a binary approximation.
            Assert.Contains(value.ToString(System.Globalization.CultureInfo.InvariantCulture), json);
        }

        [Fact]
        public void Decimal_AdditionEdgeCase_IsNotRoundedThroughDouble()
        {
            // 0.1 + 0.2 == 0.3 exactly in decimal (it is NOT in double).
            var original = new Poco { Price = 0.1m + 0.2m };

            string json = JsonSerializer.Serialize(original, CoreJson.Default);
            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            Assert.NotNull(result);
            Assert.Equal(0.3m, result!.Price);
            Assert.Contains("0.3", json);
            Assert.DoesNotContain("0.30000000", json);
        }

        [Fact]
        public void Enum_Serializes_AsPascalCaseMemberName()
        {
            var original = new Poco { Phase = Phase.InProgress };

            string json = JsonSerializer.Serialize(original, CoreJson.Default);

            Assert.Contains("\"InProgress\"", json);
            Assert.DoesNotContain("\"inProgress\"", json);
            Assert.DoesNotContain("\"phase\":1", json); // not a numeric enum
        }

        [Theory]
        [InlineData("planning", Phase.Planning)]
        [InlineData("PLANNING", Phase.Planning)]
        [InlineData("inProgress", Phase.InProgress)]
        [InlineData("InProgress", Phase.InProgress)]
        public void Enum_Deserializes_CaseInsensitively(string token, Phase expected)
        {
            string json = $"{{\"phase\":\"{token}\"}}";

            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            Assert.NotNull(result);
            Assert.Equal(expected, result!.Phase);
        }

        [Fact]
        public void PropertyNames_Emit_AsCamelCase()
        {
            string json = JsonSerializer.Serialize(SamplePoco(), CoreJson.Default);

            Assert.Contains("\"name\"", json);
            Assert.Contains("\"openedAt\"", json);
            Assert.DoesNotContain("\"Name\"", json);
            Assert.DoesNotContain("\"OpenedAt\"", json);
        }

        [Fact]
        public void PropertyNames_Read_CaseInsensitively()
        {
            // PascalCase property names (as an older writer may have emitted) must still bind.
            string json = "{\"Name\":\"MSFT\",\"Count\":7,\"Price\":1.5,\"Phase\":\"Done\"}";

            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            Assert.NotNull(result);
            Assert.Equal("MSFT", result!.Name);
            Assert.Equal(7, result.Count);
            Assert.Equal(1.5m, result.Price);
            Assert.Equal(Phase.Done, result.Phase);
        }

        [Fact]
        public void DateTimeOffset_RoundTrips_AsIso8601_PreservingInstant()
        {
            var original = new Poco
            {
                OpenedAt = new DateTimeOffset(2026, 6, 16, 14, 30, 0, TimeSpan.Zero),
            };

            string json = JsonSerializer.Serialize(original, CoreJson.Default);
            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            Assert.Contains("2026-06-16T14:30:00", json);
            Assert.NotNull(result);
            // Same instant in UTC regardless of how the offset is represented.
            Assert.Equal(original.OpenedAt.ToUniversalTime(), result!.OpenedAt.ToUniversalTime());
        }

        [Fact]
        public void DateTimeOffset_PreservesNonZeroOffset()
        {
            var original = new Poco
            {
                OpenedAt = new DateTimeOffset(2026, 6, 16, 9, 30, 0, TimeSpan.FromHours(-5)),
            };

            string json = JsonSerializer.Serialize(original, CoreJson.Default);
            Poco? result = JsonSerializer.Deserialize<Poco>(json, CoreJson.Default);

            Assert.NotNull(result);
            Assert.Equal(original.OpenedAt, result!.OpenedAt);
            Assert.Equal(TimeSpan.FromHours(-5), result.OpenedAt.Offset);
        }

        [Fact]
        public void Indented_ProducesWhitespace_DefaultDoesNot()
        {
            Poco poco = SamplePoco();

            string compact = JsonSerializer.Serialize(poco, CoreJson.Default);
            string indented = JsonSerializer.Serialize(poco, CoreJson.Indented);

            Assert.DoesNotContain("\n", compact);
            Assert.Contains("\n", indented);

            // Despite the whitespace difference, both parse to the same graph.
            AssertPocoEqual(poco, JsonSerializer.Deserialize<Poco>(compact, CoreJson.Default));
            AssertPocoEqual(poco, JsonSerializer.Deserialize<Poco>(indented, CoreJson.Default));
        }

        [Fact]
        public void Default_IsReadOnly()
        {
            Assert.True(CoreJson.Default.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => CoreJson.Default.WriteIndented = true);
            Assert.Throws<InvalidOperationException>(
                () => CoreJson.Default.Converters.Add(new JsonStringEnumConverter()));
        }

        [Fact]
        public void Indented_IsReadOnly()
        {
            Assert.True(CoreJson.Indented.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => CoreJson.Indented.WriteIndented = false);
        }

        [Fact]
        public void CreateOptions_ReturnsMutableCopy_WithSamePolicy()
        {
            JsonSerializerOptions copy = CoreJson.CreateOptions();

            Assert.False(copy.IsReadOnly);
            Assert.False(copy.WriteIndented);
            Assert.Equal(JsonNamingPolicy.CamelCase, copy.PropertyNamingPolicy);
            Assert.True(copy.PropertyNameCaseInsensitive);

            // The same policy still round-trips a representative POCO.
            Poco original = SamplePoco();
            Poco? result = JsonSerializer.Deserialize<Poco>(
                JsonSerializer.Serialize(original, copy), copy);
            AssertPocoEqual(original, result);
        }

        // A converter that uppercases strings, used to prove the copy-and-extend path is honored.
        private sealed class ShoutingStringConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => reader.GetString() ?? string.Empty;

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.ToUpperInvariant());
        }

        [Fact]
        public void CopyAndExtend_UsesAddedConverter_WithoutAffectingSharedDefault()
        {
            JsonSerializerOptions extended = CoreJson.CreateOptions();
            extended.Converters.Add(new ShoutingStringConverter());

            var poco = new Poco { Name = "aapl" };

            string extendedJson = JsonSerializer.Serialize(poco, extended);
            string defaultJson = JsonSerializer.Serialize(poco, CoreJson.Default);

            Assert.Contains("AAPL", extendedJson); // custom converter applied
            Assert.Contains("aapl", defaultJson);  // shared Default untouched
            Assert.DoesNotContain("AAPL", defaultJson);
        }

        [Fact]
        public void ExistingOnDiskShape_DeserializesSuccessfully()
        {
            // Represents a file written earlier: camelCase property names, PascalCase enum string.
            const string onDisk =
                "{\"name\":\"AAPL\",\"count\":42,\"price\":123.456789,\"phase\":\"InProgress\"," +
                "\"stop\":null,\"openedAt\":\"2026-06-16T09:30:00-05:00\"}";

            Poco? result = JsonSerializer.Deserialize<Poco>(onDisk, CoreJson.Default);

            Assert.NotNull(result);
            Assert.Equal("AAPL", result!.Name);
            Assert.Equal(42, result.Count);
            Assert.Equal(123.456789m, result.Price);
            Assert.Equal(Phase.InProgress, result.Phase);
            Assert.Null(result.Stop);
            Assert.Equal(
                new DateTimeOffset(2026, 6, 16, 9, 30, 0, TimeSpan.FromHours(-5)),
                result.OpenedAt);
        }
    }
}
