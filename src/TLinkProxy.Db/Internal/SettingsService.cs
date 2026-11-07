#pragma warning disable CC0009, CC0010 // SQL parameter names are clearer inline

using Microsoft.Data.Sqlite;
using TLinkProxy.Abstractions;

namespace TLinkProxy.Db.Internal
{
    internal sealed class SettingsService : ISettingsService
    {
        private const string ErrorSettingNameRequired = "Setting name is required.";
        private readonly SqliteDatabase _database;

        public SettingsService(SqliteDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public bool DeleteSetting(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand("DELETE FROM Settings WHERE Name = $name;")
                .With("$name", name);

            return command.ExecuteNonQuery() > 0;
        }

        public string GetSetting(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand("SELECT Value FROM Settings WHERE Name = $name;")
                .With("$name", name);

            object result = command.ExecuteScalar();
            return result == null || result is DBNull ? null : Convert.ToString(result);
        }

        public string UpdateSetting(string name, string value)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(ErrorSettingNameRequired, nameof(name));
            }

            string now = DbTime.Format(DateTime.UtcNow);

            using SqliteConnection connection = _database.CreateConnection();
            using SqliteCommand command = connection.CreateCommand("""
                INSERT INTO Settings (Name, Value, Created, Updated)
                VALUES ($name, $value, $now, $now)
                ON CONFLICT(Name) DO UPDATE SET Value = excluded.Value, Updated = excluded.Updated;
                """)
                .With("$name", name)
                .With("$value", value)
                .With("$now", now);

            command.ExecuteNonQuery();
            return value;
        }
    }
}
