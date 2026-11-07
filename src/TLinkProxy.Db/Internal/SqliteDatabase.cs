using Microsoft.Data.Sqlite;

#pragma warning disable CC0009

namespace TLinkProxy.Db.Internal
{
    /// <summary>
    /// Owns the SQLite database file and connection string, and creates the schema on first use.
    /// </summary>
    internal sealed class SqliteDatabase
    {
        private const int ConnectionTimeoutSeconds = 30;
        private const string BusyTimeoutCommand = "PRAGMA busy_timeout = 5000;";
        private const string AiModelsTableName = "AIModels";
        private const string TextColumnNullable = "TEXT NULL";

        private const string SchemaScript = """
            CREATE TABLE IF NOT EXISTS Settings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL COLLATE NOCASE UNIQUE,
                Value TEXT NULL,
                Created TEXT NULL,
                Updated TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS AIModels (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Alias TEXT NOT NULL COLLATE NOCASE,
                Kind INTEGER NOT NULL,
                Endpoint TEXT NULL,
                Model TEXT NULL,
                Tags TEXT NULL,
                ComputeType INTEGER NOT NULL,
                LatencyProfile INTEGER NOT NULL,
                CostPerMillionTokensInput TEXT NULL,
                CostPerMillionTokensOutput TEXT NULL,
                MaxContextSize INTEGER NOT NULL,
                ConcurrencyLimit INTEGER NOT NULL,
                Weight INTEGER NOT NULL,
                AllowsQueue INTEGER NOT NULL,
                MaxQueueDepth INTEGER NOT NULL,
                IsEnabled INTEGER NOT NULL,
                ApiKeyEnvVar TEXT NULL,
                AuthScheme TEXT NULL,
                AdditionalHeaders TEXT NULL,
                HealthProbeSeconds INTEGER NOT NULL,
                GpuLayers INTEGER NOT NULL,
                Threads INTEGER NOT NULL,
                BatchSize INTEGER NOT NULL,
                AutoRestart INTEGER NOT NULL,
                TimeoutSeconds INTEGER NOT NULL,
                Created TEXT NULL,
                Updated TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS DailyTokenCounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RequestCount INTEGER NOT NULL,
                OriginalTokens INTEGER NOT NULL,
                OptimizedTokens INTEGER NOT NULL,
                PerModel TEXT NULL,
                Created TEXT NULL,
                CreatedTicks INTEGER NOT NULL,
                CreatedDay TEXT NOT NULL,
                Updated TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_DailyTokenCounts_CreatedDay ON DailyTokenCounts(CreatedDay);

            CREATE TABLE IF NOT EXISTS SessionMetrics (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Model TEXT NULL,
                SessionId TEXT NULL,
                ToolInvocations TEXT NULL,
                Created TEXT NULL,
                Updated TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_SessionMetrics_SessionId ON SessionMetrics(SessionId);

            CREATE TABLE IF NOT EXISTS Meta (
                Key TEXT PRIMARY KEY,
                Value TEXT NULL
            );
            """;

        private readonly string _connectionString;

        public SqliteDatabase(string databaseFilePath)
        {
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = true,
                DefaultTimeout = ConnectionTimeoutSeconds
            }.ToString();
        }

        public SqliteConnection CreateConnection()
        {
            SqliteConnection connection = new(_connectionString);
            connection.Open();

            using SqliteCommand pragma = connection.CreateCommand();
            pragma.CommandText = BusyTimeoutCommand;
            pragma.ExecuteNonQuery();

            return connection;
        }

        public void EnsureSchema()
        {
            using SqliteConnection connection = CreateConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = SchemaScript;
            command.ExecuteNonQuery();

            EnsureColumn(connection, AiModelsTableName, "AuthScheme", TextColumnNullable);
            EnsureColumn(connection, AiModelsTableName, "AdditionalHeaders", TextColumnNullable);
        }

        private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
        {
            using SqliteCommand check = connection.CreateCommand();
            check.CommandText = $"PRAGMA table_info({table});";

            using (SqliteDataReader reader = check.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (String.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            using SqliteCommand add = connection.CreateCommand();
            add.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
            add.ExecuteNonQuery();
        }
    }
}
#pragma warning restore CC0009