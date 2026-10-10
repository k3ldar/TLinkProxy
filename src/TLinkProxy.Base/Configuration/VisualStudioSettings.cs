using System.Text.Json;
using System.Text.Json.Serialization;

namespace TLinkProxy.Base.Configuration
{
    /// <summary>
    /// Persisted settings controlling how prompts originating from Visual Studio
    /// are trimmed and augmented before being forwarded to a backend.
    /// </summary>
    public sealed class VisualStudioSettings
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>Master switch enabling prompt processing for Visual Studio requests.</summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>Tag names whose entire &lt;tag&gt;...&lt;/tag&gt; region is removed.</summary>
        [JsonPropertyName("removeBlocks")]
        public List<string> RemoveBlocks { get; set; } = [];

        /// <summary>Literal substrings removed wherever they appear.</summary>
        [JsonPropertyName("removeTextPatterns")]
        public List<string> RemoveTextPatterns { get; set; } = [];

        /// <summary>Regex patterns removed wherever they match.</summary>
        [JsonPropertyName("removeRegexPatterns")]
        public List<string> RemoveRegexPatterns { get; set; } = [];

        /// <summary>Trim trailing whitespace on every line.</summary>
        [JsonPropertyName("trimLineEnds")]
        public bool TrimLineEnds { get; set; } = true;

        /// <summary>Collapse runs of 3+ blank lines down to a single blank line.</summary>
        [JsonPropertyName("collapseBlankLines")]
        public bool CollapseBlankLines { get; set; } = true;

        /// <summary>Additional developer instructions appended when not already present in the prompt.</summary>
        [JsonPropertyName("copilotInstructions")]
        public List<string> CopilotInstructions { get; set; } = [];

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, SerializerOptions);
        }

        public static VisualStudioSettings FromJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                return new VisualStudioSettings();
            }

            try
            {
                return JsonSerializer.Deserialize<VisualStudioSettings>(json, SerializerOptions) ?? new VisualStudioSettings();
            }
            catch (JsonException)
            {
                return new VisualStudioSettings();
            }
        }

        /// <summary>Joins a list of values into a single newline-delimited string.</summary>
        public static string JoinLines(IEnumerable<string> values)
        {
            if (values == null)
            {
                return String.Empty;
            }

            return String.Join(Environment.NewLine, values.Where(value => !String.IsNullOrWhiteSpace(value)));
        }

        /// <summary>Splits a newline-delimited string into trimmed, non-empty, de-duplicated entries.</summary>
        public static List<string> SplitLines(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            return [.. value.Split([Constants.CrlfNewLine, Constants.NewLine, Constants.CarriageReturn], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)];
        }
    }
}
