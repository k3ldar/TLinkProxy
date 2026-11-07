using Microsoft.Data.Sqlite;

namespace TLinkProxy.Db.Internal
{
    internal static class SqliteExtensions
    {
        public static SqliteCommand CreateCommand(this SqliteConnection connection, string sql, SqliteTransaction transaction = null)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            return command;
        }

        /// <summary>Adds a parameter, mapping null to <see cref="DBNull"/>.</summary>
        public static SqliteCommand With(this SqliteCommand command, string name, object value)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return command;
        }

        public static string GetStringOrNull(this SqliteDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public static long GetInt64(this SqliteDataReader reader, string column)
        {
            return reader.GetInt64(reader.GetOrdinal(column));
        }
    }
}
