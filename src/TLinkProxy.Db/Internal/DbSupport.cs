using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Db.Internal
{
    /// <summary>Shared JSON serialisation used to persist complex columns (tags, per-model stats, tool invocations).</summary>
    internal static class DbJson
    {
        public static readonly JsonSerializerOptions Options = CreateOptions();

        public static string Serialize<T>(T value)
        {
            return value == null ? null : JsonSerializer.Serialize(value, Options);
        }

        public static T Deserialize<T>(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, Options);
        }

        private static JsonSerializerOptions CreateOptions()
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }

    /// <summary>Round-trip helpers for storing <see cref="DateTime"/> values as sortable, timezone-aware strings.</summary>
    internal static class DbTime
    {
        private const string RoundTripFormat = "o";
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Format(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return utc.ToString(RoundTripFormat, Invariant);
        }

        public static DateTime Parse(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            DateTime parsed = DateTime.Parse(value, Invariant, DateTimeStyles.RoundtripKind);
            return parsed.Kind == DateTimeKind.Utc ? parsed : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }
    }
}
