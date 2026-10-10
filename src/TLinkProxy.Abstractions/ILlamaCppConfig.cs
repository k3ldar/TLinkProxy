namespace TLinkProxy.Abstractions
{
    public interface ILlamaCppConfig
    {
        bool AutoRestart { get; set; }
        int BatchSize { get; set; }
        int ContextSize { get; set; }
        int GpuLayers { get; set; }
        string Host { get; set; }
        int HealthCheckSeconds { get; set; }
        int Port { get; set; }
        int Threads { get; set; }
        int TimeoutSeconds { get; set; }
    }
}
