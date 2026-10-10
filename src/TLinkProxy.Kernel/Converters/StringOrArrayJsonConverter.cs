using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Kernel.Converters
{
    public class StringOrArrayJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                // Parse the whole array then take the first element
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement arr = doc.RootElement;

                if (arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
                {
                    JsonElement first = arr[0];
                    return first.ValueKind == JsonValueKind.String ? first.GetString() : first.ToString();
                }

                return String.Empty;
            }

            // Fallback: parse the current token and return its raw text (works on older frameworks)
            using JsonDocument fallbackDoc = JsonDocument.ParseValue(ref reader);
            return fallbackDoc.RootElement.GetRawText();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}