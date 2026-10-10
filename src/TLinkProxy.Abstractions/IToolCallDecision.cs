namespace TLinkProxy.Abstractions
{
    public interface IToolCallDecision
    {
        ToolPolicyAction Action { get; init; }
        string Message { get; init; }
        string SanitizedArguments { get; init; }
    }
}
