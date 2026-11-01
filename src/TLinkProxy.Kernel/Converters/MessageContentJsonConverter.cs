using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel.Converters
{
    /// <summary>
    /// Deserializes an OpenAI message "content" property, which may be a string, an
    /// array of text blocks (e.g. [{ "type": "text", "text": "..." }]), or another
    /// scalar, into a single string for internal processing.
    /// </summary>
    public sealed class MessageContentJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.StartArray:
                    {
                        using JsonDocument arrayDoc = JsonDocument.ParseValue(ref reader);
                        return Flatten(arrayDoc.RootElement);
                    }

                default:
                    {
                        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                        return doc.RootElement.GetRawText();
                    }
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }

        private static string Flatten(JsonElement element)
        {
            var parts = new List<string>();

            foreach (JsonElement item in element.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.String:
                        parts.Add(item.GetString());
                        break;

                    case JsonValueKind.Object:
                        if (item.TryGetProperty("text", out JsonElement text) && text.ValueKind == JsonValueKind.String)
                        {
                            parts.Add(text.GetString());
                        }
                        else if (item.TryGetProperty("text", out text))
                        {
                            parts.Add(text.GetRawText());
                        }
                        else
                        {
                            parts.Add(item.GetRawText());
                        }
                        break;

                    case JsonValueKind.Array:
                        parts.Add(Flatten(item));
                        break;

                    case JsonValueKind.Null:
                        break;

                    default:
                        parts.Add(item.GetRawText());
                        break;
                }
            }

            return String.Concat(parts);
        }
    }
}
