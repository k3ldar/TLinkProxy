namespace TLinkProxy.Abstractions
{
    public interface IAiModelRuntimeStateStore
    {
        bool? GetHealth(string alias);

        void SetHealth(string alias, bool healthy);

        bool GetRunning(string alias);

        void SetRunning(string alias, bool isRunning);
    }
}
