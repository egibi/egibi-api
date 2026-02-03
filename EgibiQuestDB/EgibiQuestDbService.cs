using Npgsql;

namespace EgibiQuestDbSdk
{
    public class EgibiQuestDbService
    {
        private readonly string _connectionString;

        public EgibiQuestDbService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<string>> ListTablesAsync()
        {
            List<string> tableNames = new List<string>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var selectCommand = new NpgsqlCommand("SELECT name FROM tables();", connection);
            await using var reader = await selectCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }

            return tableNames;
        }

        public async Task CreateTableAsync(string tableName, string[] columnNames)
        {
            //TODO: setup columns

            var sql = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    timestamp TIMESTAMP,
                    price DOUBLE,
                    voulme LONG
                )
                TIMESTAMP(timestamp);";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var sqlCmd = new NpgsqlCommand(sql, connection);
            await sqlCmd.ExecuteNonQueryAsync();
        }

        public async Task DropTableAsync(string tableName)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sqlCommandText = $"DROP TABLE IF EXISTS \"{tableName}\"";

            await using var sqlCommand = new NpgsqlCommand(sqlCommandText, connection);
            await sqlCommand.ExecuteNonQueryAsync();
        }
    }
}
