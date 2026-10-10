using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Configuration
{
    public sealed class LowercaseEnumJsonConverter : JsonConverter<ModelKind>
    {
        public override ModelKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new JsonException(Constants.EnumValueRequiredMessage);
                }

                foreach (string name in Enum.GetNames<ModelKind>())
                {
                    if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name.ToLowerInvariant(), value.Trim(), StringComparison.Ordinal))
                    {
                        return (ModelKind)Enum.Parse(typeof(ModelKind), name);
                    }
                }

                throw new JsonException($"'{value}' is not a valid value for enum '{typeof(ModelKind).Name}'.");
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long numericValue))
            {
                return (ModelKind)numericValue;
            }

            throw new JsonException($"Unexpected token while reading enum '{typeof(ModelKind).Name}'.");
        }

        public override void Write(Utf8JsonWriter writer, ModelKind value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToLowerInvariant());
        }
    }

    public sealed class StrictIntJsonConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value))
            {
                return value;
            }

            throw new JsonException(Constants.IntegerValueRequiredMessage);
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public sealed class StrictDoubleJsonConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out double value))
            {
                return value;
            }

            throw new JsonException(Constants.FloatingPointValueRequiredMessage);
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public sealed class StrictBoolJsonConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            throw new JsonException(Constants.BooleanValueRequiredMessage);
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
