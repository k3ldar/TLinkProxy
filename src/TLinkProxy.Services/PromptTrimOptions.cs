namespace TLinkProxy.Services;

/// <summary>
/// Configuration for prompt trimming. All collections are case-insensitive
/// where sensible; regexes use the supplied RegexOptions.
/// </summary>
public sealed class PromptTrimOptions
{
    /// <summary>Tag names whose entire &lt;tag&gt;...&lt;/tag&gt; region is removed.</summary>
    public List<string> RemoveBlocks { get; init; } = [];

    /// <summary>Literal substrings removed wherever they appear.</summary>
    public List<string> RemoveTextPatterns { get; init; } = [];

    /// <summary>Regex patterns removed wherever they match.</summary>
    public List<string> RemoveRegexPatterns { get; init; } = [];

    /// <summary>Tag names that must never be removed regardless of other settings.</summary>
    public List<string> NeverRemoveBlocks { get; init; } =
    [
        "environment_limitations",
        "prohibited_actions"
    ];

    /// <summary>Collapse runs of 3+ blank lines down to a single blank line after trimming.</summary>
    public bool CollapseBlankLines { get; init; } = true;

    /// <summary>Trim trailing whitespace on every line.</summary>
    public bool TrimLineEnds { get; init; } = true;

    public static PromptTrimOptions Default => new()
    {
        RemoveBlocks =
        [
            "version_information",
            "model_information",
            "system_notifications",
            "code_search_tools",
            "task_completion"
        ],
        RemoveTextPatterns =
        [
            "Version number:",
            "Remember that your output will be displayed on a command line interface.",
            "Your job is to perform the task the user requested.",
            "You do not need to make additional tool calls to verify this.",
            "Your goal is to deliver complete, working solutions."
        ]
    };
}
