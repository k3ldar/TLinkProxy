using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Models
{

    public sealed class ToolInvocation : IToolInvocation
    {
        public string Name { get; set; } = String.Empty;

        public string Arguments { get; set; } = String.Empty;

        public ToolInvocationOutcome Outcome { get; set; } = ToolInvocationOutcome.Allowed;
    }
}
