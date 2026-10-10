using System.Text.Json.Serialization;

namespace TLinkProxy.Models
{
    public sealed class DashboardStatusModel
    {
        [JsonPropertyName("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; set; }

        [JsonPropertyName("optimizer")]
        public DashboardOptimizerSummaryDto Optimizer { get; set; } = new();

        [JsonPropertyName("models")]
        public List<ModelStatusDto> Models { get; set; } = [];
    }
}
