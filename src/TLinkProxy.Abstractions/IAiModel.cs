namespace TLinkProxy.Abstractions
{
    public interface IAiModel
    {
        string Alias { get; set; }
        bool AllowsQueue { get; set; }
        string ApiKeyEnvVar { get; set; }
        bool AutoRestart { get; set; }
        uint BatchSize { get; set; }
        ModelComputeProfile ComputeType { get; set; }
        byte ConcurrencyLimit { get; set; }
        decimal CostPerMillionTokensInput { get; set; }
        decimal CostPerMillionTokensOutput { get; set; }
        string Endpoint { get; set; }
        uint GpuLayers { get; set; }
        uint HealthProbeSeconds { get; set; }
        bool IsEnabled { get; set; }
        ModelKind Kind { get; set; }
        ModelLatencyProfile LatencyProfile { get; set; }
        ModelMaximumContextSize MaxContextSize { get; set; }
        byte MaxQueueDepth { get; set; }
        string Model { get; set; }
        List<string> Tags { get; set; }
        byte Threads { get; set; }
        uint TimeoutSeconds { get; set; }
        byte Weight { get; set; }
    }
}