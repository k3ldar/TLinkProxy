namespace TLinkProxy.Abstractions
{
    public interface ISessionMetricRecord
    {
        string Model { get; set; }
        DateTime RecordedAtUtc { get; set; }
        string SessionId { get; set; }
        List<IToolInvocation> ToolInvocations { get; set; }
    }
}
