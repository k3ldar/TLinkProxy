using TLinkProxy.Abstractions;
using TLinkProxy.Db.Tables;
using SimpleDB;

namespace TLinkProxy.Db.Internal
{
    internal sealed class AiModelStore : IAiModelStore
    {
        private const string ErrorModelAliasRequired = "Model alias is required.";
        private readonly ISimpleDBOperations<AIModelDataRow> _modelData;

        public AiModelStore(ISimpleDBOperations<AIModelDataRow> modelData)
        {
            _modelData = modelData ?? throw new ArgumentNullException(nameof(modelData));
        }

        public void AddModel(IAiModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (String.IsNullOrWhiteSpace(model.Alias))
            {
                throw new ArgumentException(ErrorModelAliasRequired, nameof(model));
            }

            AIModelDataRow existing = FindRow(model.Alias);

            if (existing != null)
            {
                ApplyModel(existing, model);
                _modelData.Update(existing);
                return;
            }

            _modelData.Insert(ToRow(model));
        }

        public bool DeleteModel(IAiModel model)
        {
            if (model == null)
            {
                return false;
            }

            AIModelDataRow row = FindRow(model.Alias);

            if (row == null)
            {
                return false;
            }

            _modelData.Delete(row);
            return true;
        }

        public IReadOnlyList<IAiModel> GetAllModels()
        {
            List<IAiModel> models = [];

            foreach (AIModelDataRow row in _modelData.Select())
            {
                IAiModel model = ToModel(row);

                if (model != null)
                {
                    models.Add(model);
                }
            }

            return models;
        }

        public IAiModel GetModel(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            AIModelDataRow row = FindRow(alias);
            return row == null ? null : ToModel(row);
        }

        public bool UpdateModel(IAiModel model)
        {
            if (model == null)
            {
                return false;
            }

            if (String.IsNullOrWhiteSpace(model.Alias))
            {
                return false;
            }

            AIModelDataRow row = FindRow(model.Alias);

            if (row == null)
            {
                AddModel(model);
                return true;
            }

            ApplyModel(row, model);
            _modelData.Update(row);
            return true;
        }

        private AIModelDataRow FindRow(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            List<AIModelDataRow> matches = [.. _modelData.Select(x => String.Equals(x.Alias, alias, StringComparison.OrdinalIgnoreCase))];
            return matches.FirstOrDefault();
        }

        private static AIModelDataRow ToRow(IAiModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            AIModelDataRow row = new()
            {
                Alias = model.Alias,
                Kind = model.Kind,
                Endpoint = model.Endpoint,
                Model = model.Model,
                ComputeType = model.ComputeType,
                LatencyProfile = model.LatencyProfile,
                CostPerMillionTokensInput = model.CostPerMillionTokensInput,
                CostPerMillionTokensOutput = model.CostPerMillionTokensOutput,
                MaxContextSize = model.MaxContextSize,
                ConcurrencyLimit = model.ConcurrencyLimit,
                Weight = model.Weight,
                AllowsQueue = model.AllowsQueue,
                MaxQueueDepth = model.MaxQueueDepth,
                IsEnabled = model.IsEnabled,
                ApiKeyEnvVar = model.ApiKeyEnvVar,
                HealthProbeSeconds = model.HealthProbeSeconds,
                GpuLayers = model.GpuLayers,
                Threads = model.Threads,
                BatchSize = model.BatchSize,
                AutoRestart = model.AutoRestart,
                TimeoutSeconds = model.TimeoutSeconds,
                Tags = []
            };

            if (model.Tags != null)
            {
                foreach (string tag in model.Tags)
                {
                    row.Tags.Add(tag);
                }
            }

            return row;
        }

        private static void ApplyModel(AIModelDataRow row, IAiModel model)
        {
            ArgumentNullException.ThrowIfNull(row);
            ArgumentNullException.ThrowIfNull(model);

            row.Alias = model.Alias;
            row.Kind = model.Kind;
            row.Endpoint = model.Endpoint;
            row.Model = model.Model;
            row.ComputeType = model.ComputeType;
            row.LatencyProfile = model.LatencyProfile;
            row.CostPerMillionTokensInput = model.CostPerMillionTokensInput;
            row.CostPerMillionTokensOutput = model.CostPerMillionTokensOutput;
            row.MaxContextSize = model.MaxContextSize;
            row.ConcurrencyLimit = model.ConcurrencyLimit;
            row.Weight = model.Weight;
            row.AllowsQueue = model.AllowsQueue;
            row.MaxQueueDepth = model.MaxQueueDepth;
            row.IsEnabled = model.IsEnabled;
            row.ApiKeyEnvVar = model.ApiKeyEnvVar;
            row.HealthProbeSeconds = model.HealthProbeSeconds;
            row.GpuLayers = model.GpuLayers;
            row.Threads = model.Threads;
            row.BatchSize = model.BatchSize;
            row.AutoRestart = model.AutoRestart;
            row.TimeoutSeconds = model.TimeoutSeconds;

            ObservableList<string> tags = [];

            if (model.Tags != null)
            {
                foreach (string tag in model.Tags)
                {
                    tags.Add(tag);
                }
            }

            row.Tags = tags;
        }

        private static IAiModel ToModel(AIModelDataRow row)
        {
            if (row == null)
            {
                return null;
            }

            ObservableList<string> tags = [];

            if (row.Tags != null)
            {
                foreach (string tag in row.Tags)
                {
                    tags.Add(tag);
                }
            }

            return new AiModel
            {
                Alias = row.Alias,
                Kind = row.Kind,
                Endpoint = row.Endpoint,
                Model = row.Model,
                ComputeType = row.ComputeType,
                LatencyProfile = row.LatencyProfile,
                CostPerMillionTokensInput = row.CostPerMillionTokensInput,
                CostPerMillionTokensOutput = row.CostPerMillionTokensOutput,
                MaxContextSize = row.MaxContextSize,
                ConcurrencyLimit = row.ConcurrencyLimit,
                Weight = row.Weight,
                AllowsQueue = row.AllowsQueue,
                MaxQueueDepth = row.MaxQueueDepth,
                IsEnabled = row.IsEnabled,
                ApiKeyEnvVar = row.ApiKeyEnvVar,
                HealthProbeSeconds = row.HealthProbeSeconds,
                GpuLayers = row.GpuLayers,
                Threads = row.Threads,
                BatchSize = row.BatchSize,
                AutoRestart = row.AutoRestart,
                TimeoutSeconds = row.TimeoutSeconds,
                Tags = tags
            };
        }
    }
}
