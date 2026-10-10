using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Models
{
    public sealed class SessionMetricRecord : ISessionMetricRecord
    {
        public string SessionId { get; set; } = String.Empty;

        public DateTime RecordedAtUtc { get; set; }

        public string Model { get; set; } = String.Empty;

        public List<IToolInvocation> ToolInvocations { get; set; } = [];
    }
}
