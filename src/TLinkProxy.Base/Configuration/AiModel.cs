using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Configuration
{
    public sealed class AiModel : IAiModel
    {
        public string Alias { get; set; }

        public bool AllowsQueue { get; set; }

        public string ApiKeyEnvVar { get; set; }

        public bool AutoRestart { get; set; }

        public uint BatchSize { get; set; }

        public ModelComputeProfile ComputeType { get; set; }

        public byte ConcurrencyLimit { get; set; }

        public decimal CostPerMillionTokensInput { get; set; }

        public decimal CostPerMillionTokensOutput { get; set; }

        public string Endpoint { get; set; }

        public uint GpuLayers { get; set; }

        public uint HealthProbeSeconds { get; set; }

        public bool IsEnabled { get; set; }

        public ModelKind Kind { get; set; }

        public ModelLatencyProfile LatencyProfile { get; set; }

        public ModelMaximumContextSize MaxContextSize { get; set; }

        public byte MaxQueueDepth { get; set; }

        public string Model { get; set; }

        public List<string> Tags { get; set; } = [];

        public byte Threads { get; set; }

        public uint TimeoutSeconds { get; set; }

        public byte Weight { get; set; }
    }
}
