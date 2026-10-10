
namespace TLinkProxy.Abstractions
{
    public interface IAiBackendRegistry : IDisposable
    {
        IAiSettings Current { get; }

        IEventBus EventBus { get; }

        IAiModelRuntimeStateStore StateStore { get; }

        IReadOnlyList<IAiModelConfig> GetAllBackends();

        IAiModelConfig GetBackendByAlias(string alias);

        bool IsHealthy(string alias);

        void SetHealth(string alias, bool healthy);
    }
}