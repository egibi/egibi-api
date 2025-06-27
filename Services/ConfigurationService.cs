#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace egibi_api.Services
{
    public class ConfigurationService
    {
        private readonly EgibiDbContext _db;

        public ConfigurationService(EgibiDbContext db)
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

                return new RequestResponse(tableNames, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }


        }

        public async Task<RequestResponse> GetEntityTypeRecords(string tableName)
        {
            try
            {
                var entityTypes = await _db.Database
                    .SqlQueryRaw<EntityBase>($"SELECT * FROM \"{tableName}\"")
                    .ToListAsync();

                return new RequestResponse(entityTypes, 200, "OK");
            }
            catch(Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
