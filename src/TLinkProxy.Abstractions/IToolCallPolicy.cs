namespace TLinkProxy.Abstractions
{
    public interface IToolCallPolicy
    {
        IToolCallDecision Evaluate(IToolInvocation invocation);
    }
}
