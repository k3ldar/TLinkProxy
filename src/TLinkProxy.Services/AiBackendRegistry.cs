using TLinkProxy.Abstractions;
using TLinkProxy.Base.Configuration;
using TLinkProxy.Base.Models;

namespace TLinkProxy.Services
{
    public sealed class AiBackendRegistry : IAiBackendRegistry
    {
        private readonly IAiModelStore _modelService;
        private readonly IAiModelRuntimeStateStore _stateStore;
        private readonly IEventBus _eventBus;

        public IAiSettings Current { get; }

        public IAiModelRuntimeStateStore StateStore => _stateStore;

        public IEventBus EventBus => _eventBus;

        public AiBackendRegistry(IAiSettings settings, IAiModelStore modelService)
            : this(settings, modelService, new AiModelRuntimeStateStore(), null)
        {
        }

        public AiBackendRegistry(IAiSettings settings, IAiModelStore modelService, IAiModelRuntimeStateStore stateStore, IEventBus eventBus)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(modelService);
            ArgumentNullException.ThrowIfNull(stateStore);
            Current = settings;
            _modelService = modelService;
            _stateStore = stateStore;
            _eventBus = eventBus;
        }

        public IReadOnlyList<IAiModelConfig> GetAllBackends()
        {
            return _modelService.GetAllModels()
                .Select(AiModelMapper.ToConfig)
                .ToList();
        }

        public IAiModelConfig GetBackendByAlias(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            return AiModelMapper.ToConfig(_modelService.GetModel(alias));
        }

        public void SetHealth(string alias, bool healthy)
        {
            _stateStore.SetHealth(alias, healthy);
        }

        public bool IsHealthy(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
                return true;

            IAiModelConfig modelConfig = GetBackendByAlias(alias);

            if (modelConfig != null)
            {
                if (!modelConfig.Enabled)
                    return false;

                if (modelConfig.IsOnDemand)
                    return true;
            }

            return _stateStore.GetHealth(alias) ?? true;
        }

        public void Dispose()
        {
        }
    }
}