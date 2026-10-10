using TLinkProxyConsole.Models;
using System.Text.Json;

namespace TLinkProxyConsole.Runner
{
    internal static class ResponseComparer
    {
        private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

        /// <summary>
        /// Compares <paramref name="actualJson"/> against <paramref name="fixture"/>'s
        /// expected response using either strict or subset matching.
        /// </summary>
        /// <returns>
        /// <c>(true, null)</c> on pass; <c>(false, errorMessage)</c> on failure.
        /// </returns>
        internal static (bool Passed, string Error) Compare(TestFixture fixture, string actualJson)
        {
            JsonDocument actual;

            try
            {
                actual = JsonDocument.Parse(actualJson);
            }
            catch (JsonException ex)
            {
                return (false, $"Actual response is not valid JSON: {ex.Message}\nRaw: {actualJson}");
            }

            using (actual)
            {
                if (fixture.Conversation)
                    return ConversationCheck(actual.RootElement);

                if (fixture.NonEmptyArray)
                    return ArrayCheck(actual.RootElement);

                return fixture.StrictMatch
                    ? StrictCompare(fixture.ExpectedResponse, actual.RootElement)
                    : SubsetCompare(fixture.ExpectedResponse, actual.RootElement, "$");
            }
        }

        private static (bool, string) ConversationCheck(JsonElement root)
        {
            return HasNonEmptyReply(root)
                ? (true, null)
                : (false, "Response did not contain a non-empty conversational reply.");
        }

        private static bool HasNonEmptyReply(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty prop in element.EnumerateObject())
                    {
                        if (IsReplyKey(prop.Name) &&
                            prop.Value.ValueKind == JsonValueKind.String &&
                            !String.IsNullOrWhiteSpace(prop.Value.GetString()))
                        {
                            return true;
                        }

                        if (HasNonEmptyReply(prop.Value))
                            return true;
                    }
                    return false;

                case JsonValueKind.Array:
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        if (HasNonEmptyReply(item))
                            return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private static bool IsReplyKey(string name) =>
            name.Equals("content", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("text", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("response", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("output", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("completion", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("answer", StringComparison.OrdinalIgnoreCase);

        private static (bool, string) ArrayCheck(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Array)
                return (false, $"Expected a JSON array, got {root.ValueKind}.");

            if (root.GetArrayLength() == 0)
                return (false, "Expected a non-empty JSON array.");

            return (true, null);
        }

        private static (bool, string) StrictCompare(JsonElement expected, JsonElement actual)
        {
            string expectedCanonical = Canonicalize(expected);
            string actualCanonical = Canonicalize(actual);

            if (expectedCanonical == actualCanonical)
                return (true, null);

            return (false,
                $"Strict mismatch.\n" +
                $"Expected:\n{JsonSerializer.Serialize(expected, _writeOptions)}\n" +
                $"Actual:\n{JsonSerializer.Serialize(actual, _writeOptions)}");
        }

        /// <summary>Serialises a <see cref="JsonElement"/> to a canonical (non-indented) string for equality comparison.</summary>
        private static string Canonicalize(JsonElement element) =>
            JsonSerializer.Serialize(element, (JsonSerializerOptions)null);

        /// <summary>
        /// Recursively verifies that every property in <paramref name="expected"/> exists
        /// in <paramref name="actual"/> with an equal value.  Extra properties in
        /// <paramref name="actual"/> are ignored.
        /// </summary>
        private static (bool, string) SubsetCompare(
            JsonElement expected,
            JsonElement actual,
            string path)
        {
            if (expected.ValueKind != actual.ValueKind)
            {
                return (false,
                    $"Type mismatch at '{path}': expected {expected.ValueKind}, got {actual.ValueKind}.\n" +
                    $"Expected value: {Canonicalize(expected)}\n" +
                    $"Actual value:   {Canonicalize(actual)}");
            }

            switch (expected.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty prop in expected.EnumerateObject())
                    {
                        string childPath = $"{path}.{prop.Name}";

                        if (!actual.TryGetProperty(prop.Name, out JsonElement actualChild))
                            return (false, $"Missing property '{childPath}' in actual response.");

                        (bool ok, string err) = SubsetCompare(prop.Value, actualChild, childPath);

                        if (!ok)
                            return (false, err);
                    }
                    return (true, null);

                case JsonValueKind.Array:
                    JsonElement[] expectedItems = [.. expected.EnumerateArray()];
                    JsonElement[] actualItems = [.. actual.EnumerateArray()];

                    if (expectedItems.Length != actualItems.Length)
                    {
                        return (false,
                            $"Array length mismatch at '{path}': expected {expectedItems.Length} item(s), got {actualItems.Length}.");
                    }

                    for (int i = 0; i < expectedItems.Length; i++)
                    {
                        (bool ok, string err) = SubsetCompare(expectedItems[i], actualItems[i], $"{path}[{i}]");

                        if (!ok)
                            return (false, err);
                    }
                    return (true, null);

                default:
                    string ev = Canonicalize(expected);
                    string av = Canonicalize(actual);

                    return ev == av
                        ? (true, null)
                        : (false, $"Value mismatch at '{path}': expected {ev}, got {av}.");
            }
        }
    }
}
