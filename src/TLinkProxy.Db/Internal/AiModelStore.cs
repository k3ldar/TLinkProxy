#pragma warning disable CC0009, CC0010 // SQL column and parameter names are clearer inline

using System.Globalization;
using Microsoft.Data.Sqlite;
using TLinkProxy.Abstractions;

namespace TLinkProxy.Db.Internal
{
    internal sealed class AiModelStore : IAiModelStore
    {
        private const string ErrorModelAliasRequired = "Model alias is required.";

        private const string SelectColumns = """
            Alias, Kind, Endpoint, Model, Tags, ComputeType, LatencyProfile,
            CostPerMillionTokensInput, CostPerMillionTokensOutput, MaxContextSize,
            ConcurrencyLimit, Weight, AllowsQueue, MaxQueueDepth, IsEnabled,
            ApiKeyEnvVar, AuthScheme, AdditionalHeaders, HealthProbeSeconds, GpuLayers, Threads, BatchSize,
            AutoRestart, TimeoutSeconds
            """;

        private const string FirstIdForAlias = "(SELECT Id FROM AIModels WHERE Alias = $alias ORDER BY Id LIMIT 1)";

        private readonly SqliteDatabase _database;
        private readonly Lock _writeGate = new();

        public AiModelStore(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public void AddModel(IAiModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (String.IsNullOrWhiteSpace(model.Alias))
            {
                throw new ArgumentException(ErrorModelAliasRequired, nameof(model));
            }

            Upsert(model);
        }

        public bool DeleteModel(IAiModel model)
        {
            if (model == null || String.IsNullOrWhiteSpace(model.Alias))
            {
                return false;
            }

            lock (_writeGate)
            {
                using SqliteConnection connection = _database.CreateConnection();
                using SqliteCommand command = connection.CreateCommand($"DELETE FROM AIModels WHERE Id = {FirstIdForAlias};")
                    .With("$alias", model.Alias);

                return command.ExecuteNonQuery() > 0;
            }
        }

        public IReadOnlyList<IAiModel> GetAllModels()
        {
            List<IAiModel> models = [];

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand($"SELECT {SelectColumns} FROM AIModels ORDER BY Id;");
            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                models.Add(ToModel(reader));
            }

            return models;
        }

        public IAiModel GetModel(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand($"SELECT {SelectColumns} FROM AIModels WHERE Id = {FirstIdForAlias};")
                .With("$alias", alias);
            using SqliteDataReader reader = command.ExecuteReader();

            return reader.Read() ? ToModel(reader) : null;
        }

        public bool UpdateModel(IAiModel model)
        {
            if (model == null || String.IsNullOrWhiteSpace(model.Alias))
            {
                return false;
            }

            Upsert(model);
            return true;
        }

        private void Upsert(IAiModel model)
        {
            string now = DbTime.Format(DateTime.UtcNow);

            lock (_writeGate)
            {
                using SqliteConnection connection = _database.CreateConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                using (SqliteCommand update = connection.CreateCommand($"""
                    UPDATE AIModels SET
                        Alias = $alias, Kind = $kind, Endpoint = $endpoint, Model = $model, Tags = $tags,
                        ComputeType = $computeType, LatencyProfile = $latencyProfile,
                        CostPerMillionTokensInput = $costInput, CostPerMillionTokensOutput = $costOutput,
                        MaxContextSize = $maxContextSize, ConcurrencyLimit = $concurrencyLimit, Weight = $weight,
                        AllowsQueue = $allowsQueue, MaxQueueDepth = $maxQueueDepth, IsEnabled = $isEnabled,
                        ApiKeyEnvVar = $apiKeyEnvVar, AuthScheme = $authScheme, AdditionalHeaders = $additionalHeaders, HealthProbeSeconds = $healthProbeSeconds, GpuLayers = $gpuLayers,
                        Threads = $threads, BatchSize = $batchSize, AutoRestart = $autoRestart,
                        TimeoutSeconds = $timeoutSeconds, Updated = $now
                    WHERE Id = {FirstIdForAlias};
                    """, transaction))
                {
                    Bind(update, model).With("$now", now);

                    if (update.ExecuteNonQuery() > 0)
                    {
                        transaction.Commit();
                        return;
                    }
                }

                using SqliteCommand insert = connection.CreateCommand("""
                    INSERT INTO AIModels (
                        Alias, Kind, Endpoint, Model, Tags, ComputeType, LatencyProfile,
                        CostPerMillionTokensInput, CostPerMillionTokensOutput, MaxContextSize,
                        ConcurrencyLimit, Weight, AllowsQueue, MaxQueueDepth, IsEnabled,
                        ApiKeyEnvVar, AuthScheme, AdditionalHeaders, HealthProbeSeconds, GpuLayers, Threads, BatchSize,
                        AutoRestart, TimeoutSeconds, Created, Updated
                    ) VALUES (
                        $alias, $kind, $endpoint, $model, $tags, $computeType, $latencyProfile,
                        $costInput, $costOutput, $maxContextSize,
                        $concurrencyLimit, $weight, $allowsQueue, $maxQueueDepth, $isEnabled,
                        $apiKeyEnvVar, $authScheme, $additionalHeaders, $healthProbeSeconds, $gpuLayers, $threads, $batchSize,
                        $autoRestart, $timeoutSeconds, $now, $now
                    );
                    """, transaction);

                Bind(insert, model).With("$now", now);
                insert.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        private static SqliteCommand Bind(SqliteCommand command, IAiModel model)
        {
            return command
                .With("$alias", model.Alias)
                .With("$kind", (long)model.Kind)
                .With("$endpoint", model.Endpoint)
                .With("$model", model.Model)
                .With("$tags", DbJson.Serialize(model.Tags ?? []))
                .With("$computeType", (long)model.ComputeType)
                .With("$latencyProfile", (long)model.LatencyProfile)
                .With("$costInput", model.CostPerMillionTokensInput.ToString(CultureInfo.InvariantCulture))
                .With("$costOutput", model.CostPerMillionTokensOutput.ToString(CultureInfo.InvariantCulture))
                .With("$maxContextSize", (long)model.MaxContextSize)
                .With("$concurrencyLimit", (long)model.ConcurrencyLimit)
                .With("$weight", (long)model.Weight)
                .With("$allowsQueue", model.AllowsQueue ? 1L : 0L)
                .With("$maxQueueDepth", (long)model.MaxQueueDepth)
                .With("$isEnabled", model.IsEnabled ? 1L : 0L)
                .With("$apiKeyEnvVar", model.ApiKeyEnvVar)
                .With("$authScheme", model.AuthScheme)
                .With("$additionalHeaders", DbJson.Serialize(model.AdditionalHeaders))
                .With("$healthProbeSeconds", (long)model.HealthProbeSeconds)
                .With("$gpuLayers", (long)model.GpuLayers)
                .With("$threads", (long)model.Threads)
                .With("$batchSize", (long)model.BatchSize)
                .With("$autoRestart", model.AutoRestart ? 1L : 0L)
                .With("$timeoutSeconds", (long)model.TimeoutSeconds);
        }

        private static AiModel ToModel(SqliteDataReader reader)
        {
            return new AiModel
            {
                Alias = reader.GetStringOrNull("Alias"),
                Kind = (ModelKind)reader.GetInt64("Kind"),
                Endpoint = reader.GetStringOrNull("Endpoint"),
                Model = reader.GetStringOrNull("Model"),
                Tags = DbJson.Deserialize<List<string>>(reader.GetStringOrNull("Tags")) ?? [],
                ComputeType = (ModelComputeProfile)reader.GetInt64("ComputeType"),
                LatencyProfile = (ModelLatencyProfile)reader.GetInt64("LatencyProfile"),
                CostPerMillionTokensInput = ParseDecimal(reader.GetStringOrNull("CostPerMillionTokensInput")),
                CostPerMillionTokensOutput = ParseDecimal(reader.GetStringOrNull("CostPerMillionTokensOutput")),
                MaxContextSize = (ModelMaximumContextSize)reader.GetInt64("MaxContextSize"),
                ConcurrencyLimit = (byte)reader.GetInt64("ConcurrencyLimit"),
                Weight = (byte)reader.GetInt64("Weight"),
                AllowsQueue = reader.GetInt64("AllowsQueue") != 0,
                MaxQueueDepth = (byte)reader.GetInt64("MaxQueueDepth"),
                IsEnabled = reader.GetInt64("IsEnabled") != 0,
                ApiKeyEnvVar = reader.GetStringOrNull("ApiKeyEnvVar"),
                AuthScheme = reader.GetStringOrNull("AuthScheme"),
                AdditionalHeaders = DbJson.Deserialize<Dictionary<string, string>>(reader.GetStringOrNull("AdditionalHeaders")),
                HealthProbeSeconds = (uint)reader.GetInt64("HealthProbeSeconds"),
                GpuLayers = (uint)reader.GetInt64("GpuLayers"),
                Threads = (byte)reader.GetInt64("Threads"),
                BatchSize = (uint)reader.GetInt64("BatchSize"),
                AutoRestart = reader.GetInt64("AutoRestart") != 0,
                TimeoutSeconds = (uint)reader.GetInt64("TimeoutSeconds")
            };
        }

        private static decimal ParseDecimal(string value)
        {
            return Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result) ? result : 0m;
        }
    }
}
