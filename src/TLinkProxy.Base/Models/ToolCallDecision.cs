using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Models
{
    public sealed class ToolCallDecision : IToolCallDecision
    {
        public ToolPolicyAction Action { get; init; }

        public string Message { get; init; } = String.Empty;

        public string SanitizedArguments { get; init; } = String.Empty;
    }
}
