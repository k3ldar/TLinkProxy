using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel.Converters
{

    public class ArrayOrStringJsonConverter : JsonConverter<string[]>
    {
        public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Single string value
                return [reader.GetString()];
            }
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Array of strings; only top-level elements are considered so that nested
                // arrays and objects do not leak their inner tokens into the result.
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;
                List<string> list = [];

                foreach (JsonElement element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        list.Add(element.GetString());
                    }
                    else if (element.ValueKind == JsonValueKind.Null)
                    {
                        list.Add(null);
                    }
                }
                return [.. list];
            }

            throw new JsonException($"Unexpected token type when parsing 'type' field: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else if (value.Length == 1)
            {
                // Write as single string for cleaner output
                writer.WriteStringValue(value[0]);
            }
            else
            {
                // Write as array
                writer.WriteStartArray();
                foreach (string item in value)
                {
                    writer.WriteStringValue(item);
                }
                writer.WriteEndArray();
            }
        }
    }
}
