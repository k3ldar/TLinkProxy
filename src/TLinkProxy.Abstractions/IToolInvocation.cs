namespace TLinkProxy.Abstractions
{
    public interface IToolInvocation
    {
        string Arguments { get; set; }
        string Name { get; set; }
        ToolInvocationOutcome Outcome { get; set; }
    }
}
