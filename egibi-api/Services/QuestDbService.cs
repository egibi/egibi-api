#nullable disable
using EgibiCoreLibrary.Models;
using EgibiCoreLibrary.Models.QuestDbModels;
using Npgsql;

namespace egibi_api.Services
{
    public class QuestDbService
    {
        private readonly string _connectionString;

        public QuestDbService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<RequestResponse> GetTables()
        {
            List<string> tableNames = new List<string>();

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var selectCommand = new NpgsqlCommand("SELECT table_name FROM tables();", connection);
                await using var reader = await selectCommand.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tableNames.Add(reader.GetString(0));
                }

                return new RequestResponse(tableNames, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse("GetTables()", 500, "There was an error", new ResponseError(ex));
            }

        }

        public async Task<RequestResponse> CreateTable(QuestDbTable table)
        {
            try
            {
                string sql = $@"CREATE TABLE IF NOT EXISTS {table.TableName} (";

                for (int i = 0; i < table.TableColumns.Count; i++)
                {
                    sql += $"{table.TableColumns[i].ColumnName} {table.TableColumns[i].DataType}";
                    if (i < table.TableColumns.Count - 1)
                        sql += ',';
                }

                sql += $")TIMESTAMP(timestamp) PARTITION BY {table.TablePartitionBy};";

                var reviewCommand = sql;

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var sqlCmd = new NpgsqlCommand(sql, connection);
                await sqlCmd.ExecuteNonQueryAsync();

                return new RequestResponse(reviewCommand, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(table.TableName, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DropTable(string tableName)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sqlCommandText = $"DROP TABLE IF EXISTS \"{tableName}\"";

                await using var sqlCommand = new NpgsqlCommand(sqlCommandText, connection);
                await sqlCommand.ExecuteNonQueryAsync();

                return new RequestResponse(tableName, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
