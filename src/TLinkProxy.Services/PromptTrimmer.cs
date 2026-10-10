using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TLinkProxy.Services;

/// <summary>
/// Trims an OpenAI-format chat payload by removing configured blocks
/// and text patterns from message content.
/// </summary>
public sealed class PromptTrimmer
{
    private const string JsonContentTypeToken = "application/json";
    private const string RoleSystem = "system";
    private const string ContentPropertyName = "content";
    private const string MessagesPropertyName = "messages";
    private const string BlankLinePattern = @"(\r?\n){3,}";

    private readonly PromptTrimOptions _options;
    private readonly List<Regex> _blockRegexes = [];
    private readonly List<Regex> _textRegexes = [];

    public PromptTrimmer(PromptTrimOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        BuildRegexes();
    }

    /// <summary>
    /// Trims the supplied OpenAI-format JSON body. Non-JSON or bodies
    /// without a messages array are returned unchanged.
    /// </summary>
    public PromptTrimResult Trim(string body, string contentType)
    {
        if (String.IsNullOrWhiteSpace(body))
        {
            return new PromptTrimResult { Body = body ?? String.Empty };
        }

        if (String.IsNullOrWhiteSpace(contentType)
            || !contentType.Contains(JsonContentTypeToken, StringComparison.OrdinalIgnoreCase))
        {
            return new PromptTrimResult { Body = body };
        }

        JsonNode root;
        try
        {
            root = JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return new PromptTrimResult { Body = body };
        }

        if (root is not JsonObject rootObject
            || rootObject[MessagesPropertyName] is not JsonArray messages)
        {
            return new PromptTrimResult { Body = body };
        }

        List<string> applied = [];

        foreach (JsonNode message in messages)
        {
            if (message is not JsonObject messageObject)
            {
                continue;
            }

            if (messageObject[ContentPropertyName] is not JsonValue contentValue
                || !contentValue.TryGetValue(out string content)
                || String.IsNullOrEmpty(content))
            {
                continue;
            }

            string trimmed = TrimContent(content, applied);
            messageObject[ContentPropertyName] = trimmed;
        }

        string result = rootObject.ToJsonString(TLinkProxy.Base.Constants.JsonRequestOptions);

        return new PromptTrimResult
        {
            Body = result,
            OriginalLength = body.Length,
            TrimmedLength = result.Length,
            AppliedRules = applied
        };
    }

    /// <summary>
    /// Applies block removals, literal removals and regex removals to a single string.
    /// </summary>
    public string TrimContent(string content, List<string> appliedRules)
    {
        ArgumentNullException.ThrowIfNull(content);
        string working = content;

        foreach (Regex blockRegex in _blockRegexes)
        {
            Match match = blockRegex.Match(working);
            if (match.Success)
            {
                appliedRules?.Add($"block:{blockRegex.ToString()}");
                working = blockRegex.Replace(working, String.Empty);
            }
        }

        foreach (string literal in _options.RemoveTextPatterns)
        {
            if (String.IsNullOrEmpty(literal) || !working.Contains(literal, StringComparison.Ordinal))
            {
                continue;
            }

            appliedRules?.Add($"text:{literal}");
            working = working.Replace(literal, String.Empty, StringComparison.Ordinal);
        }

        foreach (Regex textRegex in _textRegexes)
        {
            Match match = textRegex.Match(working);
            if (match.Success)
            {
                appliedRules?.Add($"regex:{textRegex}");
                working = textRegex.Replace(working, String.Empty);
            }
        }

        if (_options.TrimLineEnds)
        {
            working = TrimEachLine(working);
        }

        if (_options.CollapseBlankLines)
        {
            working = Regex.Replace(working, BlankLinePattern, Environment.NewLine + Environment.NewLine);
        }

        return working.Trim();
    }

    private void BuildRegexes()
    {
        HashSet<string> never = new(_options.NeverRemoveBlocks, StringComparer.OrdinalIgnoreCase);

        foreach (string tag in _options.RemoveBlocks)
        {
            if (String.IsNullOrWhiteSpace(tag) || never.Contains(tag))
            {
                continue;
            }

            string escaped = Regex.Escape(tag);
            string pattern = $"<{escaped}>.*?</{escaped}>";
            _blockRegexes.Add(new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase));
        }

        foreach (string pattern in _options.RemoveRegexPatterns)
        {
            if (String.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            _textRegexes.Add(new Regex(pattern, RegexOptions.Singleline));
        }
    }

    private static string TrimEachLine(string value)
    {
        string[] lines = value.Split('\n');
        StringBuilder builder = new(value.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            builder.Append(lines[i].TrimEnd('\r').TrimEnd());

            if (i < lines.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }
}