namespace TLinkProxy.Services;

/// <summary>
/// Result of a trim pass, including which rules actually fired.
/// </summary>
public sealed class PromptTrimResult
{
    public string Body { get; init; } = String.Empty;
    public int OriginalLength { get; init; }
    public int TrimmedLength { get; init; }
    public int BytesSaved => OriginalLength - TrimmedLength;
    public List<string> AppliedRules { get; init; } = [];
}