using TLinkProxy.Abstractions;

namespace TLinkProxy.Base.Configuration
{
    public static class AiModelMapper
    {
        public static AiModelConfig ToConfig(IAiModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new AiModelConfig
            {
                Alias = model.Alias,
                Kind = model.Kind,
                Endpoint = model.Endpoint,
                Model = model.Model,
                Tags = model.Tags?.ToArray(),
                Compute = model.ComputeType.ToString().ToLowerInvariant(),
                LatencyProfile = model.LatencyProfile.ToString().ToLowerInvariant(),
                CostPer1mInput = (double)(model.CostPerMillionTokensInput) / Constants.OneMillionTokens,
                CostPer1mOutput = (double)(model.CostPerMillionTokensOutput) / Constants.OneMillionTokens,
                MaxContext = (int)model.MaxContextSize,
                ConcurrencyLimit = model.ConcurrencyLimit,
                Weight = model.Weight,
                ApiKeyEnvVar = model.ApiKeyEnvVar,
                AllowQueue = model.AllowsQueue,
                MaxQueueDepth = model.MaxQueueDepth,
                Enabled = model.IsEnabled,
                HealthProbeSeconds = (int)model.HealthProbeSeconds,
                Details = new LlamaCppConfig
                {
                    GpuLayers = (int)model.GpuLayers,
                    Threads = model.Threads,
                    BatchSize = (int)model.BatchSize,
                    AutoRestart = model.AutoRestart,
                    TimeoutSeconds = (int)model.TimeoutSeconds
                }
            };
        }

        public static IAiModel ToModel(IAiModelConfig config)
        {
            if (config == null)
            {
                return null;
            }

            return new AiModel
            {
                Alias = config.Alias,
                AllowsQueue = config.AllowQueue,
                ApiKeyEnvVar = config.ApiKeyEnvVar,
                AutoRestart = config.Details?.AutoRestart ?? false,
                BatchSize = (uint)(config.Details?.BatchSize ?? 0),
                ComputeType = ParseCompute(config.Compute),
                ConcurrencyLimit = ToByte(config.ConcurrencyLimit),
                CostPerMillionTokensInput = (decimal)(config.CostPer1mInput * Constants.OneMillionTokens),
                CostPerMillionTokensOutput = (decimal)(config.CostPer1mOutput * Constants.OneMillionTokens),
                Endpoint = config.Endpoint,
                GpuLayers = (uint)(config.Details?.GpuLayers ?? 0),
                HealthProbeSeconds = (uint)(config.HealthProbeSeconds > 0 ? config.HealthProbeSeconds : 0),
                IsEnabled = config.Enabled,
                Kind = config.Kind,
                LatencyProfile = ParseLatency(config.LatencyProfile),
                MaxContextSize = (ModelMaximumContextSize)config.MaxContext,
                MaxQueueDepth = ToByte(config.MaxQueueDepth),
                Model = config.Model,
                Tags = config.Tags?.ToList() ?? [],
                Threads = ToByte(config.Details?.Threads ?? 0),
                TimeoutSeconds = (uint)(config.Details?.TimeoutSeconds ?? 0),
                Weight = ToByte(config.Weight)
            };
        }

        private static ModelComputeProfile ParseCompute(string value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                Constants.CapabilityGpu => ModelComputeProfile.Gpu,
                Constants.CapabilityCpu => ModelComputeProfile.Cpu,
                _ => ModelComputeProfile.Cloud,
            };
        }

        private static ModelLatencyProfile ParseLatency(string value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                Constants.LatencyFast => ModelLatencyProfile.Fast,
                Constants.LatencySlow => ModelLatencyProfile.Slow,
                _ => ModelLatencyProfile.Medium,
            };
        }

        private static byte ToByte(int value)
        {
            return (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
        }
    }
}
