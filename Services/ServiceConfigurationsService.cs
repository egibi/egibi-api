#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace egibi_api.Services
{
    public class ServiceConfigurationsService
    {
        private readonly EgibiDbContext _db;

        public ServiceConfigurationsService(EgibiDbContext db)
        {
            _db = db;
        }

        public async Task<RequestResponse> GetEntityTypeTables()
        {
            try
            {
                var connection = _db.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT table_name
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                AND table_name LIKE '%Type';
                ";

                List<string> tableNames = new List<string>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    tableNames.Add(reader.GetString(0));

                await connection.CloseAsync();

                return new RequestResponse(tableNames, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }


        }

        public async Task<RequestResponse> GetEntityTypeRecords(string tableName)
        {
            // TODO: Sanitize tableName to prevent injection attacks

            try
            {
                var entityTypes = await _db.Database
                    .SqlQueryRaw<EntityBase>($"SELECT * FROM \"{tableName}\"")
                    .ToListAsync();

                return new RequestResponse(entityTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveEntityType(EntityType entityType)
        {
            try
            {
                if (entityType.Id == 0)
                    return await CreateNewEntityType(entityType);
                else
                    return await UpdateExistingEntityType(entityType);
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }

        }

        private async Task<RequestResponse> CreateNewEntityType(EntityType entityType)
        {
            try
            {
                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    var conn = (NpgsqlConnection)_db.Database.GetDbConnection();

                    string tableName = $"{entityType.TableName}"; // sanitize this!
                    string sql = $"INSERT INTO \"{tableName}\" (" +
                        $"\"Name\", " +
                        $"\"Description\", " +
                        $"\"IsActive\", " +
                        $"\"CreatedAt\") " +
                        $"VALUES (@p1, @p2, @p3, @p4)";

                    using var cmd = new NpgsqlCommand(sql, conn, (NpgsqlTransaction)transaction.GetDbTransaction());
                    cmd.Parameters.AddWithValue("p1", entityType.Name);
                    cmd.Parameters.AddWithValue("p2", entityType.Description);
                    cmd.Parameters.AddWithValue("p3", true);
                    cmd.Parameters.AddWithValue("p4", DateTime.Now);

                    await cmd.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    return new RequestResponse(entityType, 200, "OK");
                }
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingEntityType(EntityType entityType)
        {
            try
            {
                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
                    //await conn.OpenAsync();

                    string tableName = entityType.TableName; // make sure you validate this!
                    string sql = $"UPDATE \"{tableName}\" SET Name = @p1, Description = @p2, IsActive = @p3, LastModifiedAt = @p4, WHERE id = @id";

                    using var cmd = new NpgsqlCommand(sql, conn, (NpgsqlTransaction)transaction.GetDbTransaction());
                    cmd.Parameters.AddWithValue("p1", entityType.Name);
                    cmd.Parameters.AddWithValue("p2", entityType.Description);
                    cmd.Parameters.AddWithValue("p3", entityType.IsActive);
                    cmd.Parameters.AddWithValue("id", entityType.Id); // your record ID here

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"Rows updated: {rowsAffected}");

                    await transaction.CommitAsync();
                }

                return new RequestResponse(entityType, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }


    }
}
