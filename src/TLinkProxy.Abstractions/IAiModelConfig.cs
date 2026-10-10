namespace TLinkProxy.Abstractions
{
    public interface IAiModelConfig
    {
        string Alias { get; set; }
        bool AllowQueue { get; set; }
        string ApiKeyEnvVar { get; set; }
        string Compute { get; set; }
        int ConcurrencyLimit { get; set; }
        double CostPer1mInput { get; set; }
        double CostPer1mOutput { get; set; }
        ILlamaCppConfig Details { get; set; }
        bool Enabled { get; set; }
        string Endpoint { get; set; }
        int HealthProbeSeconds { get; set; }
        bool IsConfigured { get; set; }
        bool IsOnDemand { get; }
        ModelKind Kind { get; set; }
        string LatencyProfile { get; set; }
        int MaxContext { get; set; }
        int MaxQueueDepth { get; set; }
        string Model { get; set; }
        string[] Tags { get; set; }
        int Weight { get; set; }
    }
}
