using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Events
{
    public sealed class ModelsReloadedEvent
    {
        public ModelsReloadedEvent(IReadOnlyList<IAiModelConfig> models)
        {
            Models = models ?? throw new ArgumentNullException(nameof(models));
        }

        public IReadOnlyList<IAiModelConfig> Models { get; }
    }
}
