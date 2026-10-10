using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class DashboardOptimizerSummaryDto
    {
        [JsonPropertyName("today")]
        public TokenPeriodSummaryDto Today { get; set; } = new();

        [JsonPropertyName("thisMonth")]
        public TokenPeriodSummaryDto ThisMonth { get; set; } = new();

        [JsonPropertyName("lifetime")]
        public TokenPeriodSummaryDto Lifetime { get; set; } = new();

        [JsonPropertyName("toolsCalled")]
        public List<ToolUsageSummaryDto> ToolsCalled { get; set; } = [];
    }
}
