#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Security.Principal;
using EntityType = EgibiCoreLibrary.Models.EntityType;

namespace egibi_api.Services
{
    public class AppConfigurationsService
    {
        private readonly EgibiDbContext _db;

        public AppConfigurationsService(EgibiDbContext db)
        {
            _db = db;
        }

        //=====================================================================================
        // ENTITY TYPES
        //=====================================================================================  
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

                if (tableNames != null && tableNames.Count > 0)
                    tableNames = tableNames.OrderBy(tableName => tableName).ToList();

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

                    string tableName = entityType.TableName; // make sure you validate this!
                    string sql = $"UPDATE \"{tableName}\" SET Name = @p1, Description = @p2, IsActive = @p3, LastModifiedAt = @p4, WHERE id = @id";

                    using var cmd = new NpgsqlCommand(sql, conn, (NpgsqlTransaction)transaction.GetDbTransaction());
                    cmd.Parameters.AddWithValue("p1", entityType.Name);
                    cmd.Parameters.AddWithValue("p2", entityType.Description);
                    cmd.Parameters.AddWithValue("p3", entityType.IsActive);
                    cmd.Parameters.AddWithValue("id", entityType.Id);

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

        public async Task<RequestResponse> DeleteEntityType(EntityType entityType)
        {
            var entityTypeTableName = entityType.TableName;                  // e.g., AccountType
            var parentTableName = entityTypeTableName.Replace("Type", "");   // e.g., Account
            var fkColumn = $"{entityTypeTableName}Id";                       // e.g., AccountTypeId

            await using var conn = new NpgsqlConnection(_db.Database.GetConnectionString());
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            string Q(string ident) => new NpgsqlCommandBuilder().QuoteIdentifier(ident);

            var sql = $@"
            DELETE FROM {Q(entityTypeTableName)} t
            WHERE t.""Id"" = @id
            AND NOT EXISTS (
                SELECT 1 FROM {Q(parentTableName)} p
                WHERE p.{Q(fkColumn)} = @id
            );";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", entityType.Id);
            var affected = await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            if (affected == 0)
                return new RequestResponse(null, 500, "Can't delete. EntityType in use");

            return new RequestResponse(entityType, 200, $"Deleted EntityType: {entityType.Name} from {entityType.TableName}");

        }

        //=====================================================================================
        // ACCOUNT USERS
        //=====================================================================================  
        public async Task<RequestResponse> GetAccountUsers()
        {
            try
            {
                List<AccountUser> accountUsers = await _db.AccountUsers
                    .ToListAsync();

                return new RequestResponse(accountUsers, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveAccountUser(AccountUser accountUser)
        {
            try
            {
                if (accountUser.Id == 0)
                    return await CreateAccountUser(accountUser);
                else
                    return await UpdateAccountUser(accountUser);
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> CreateAccountUser(AccountUser accountUser)
        {
            AccountUser newAccountUser = new AccountUser
            {
                Email = accountUser.Email,
                FirstName = accountUser.FirstName,
                LastName = accountUser.LastName,
                PhoneNumber = accountUser.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.Now.ToUniversalTime()
            };

            try
            {
                await _db.AddAsync(newAccountUser);
                await _db.SaveChangesAsync();

                return new RequestResponse(newAccountUser, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateAccountUser(AccountUser accountUser)
        {
            try
            {
                AccountUser existingAccountUser = await _db.AccountUsers
                    .Where(w => w.Id == accountUser.Id)
                    .FirstOrDefaultAsync();

                existingAccountUser.Email = accountUser.Email;
                existingAccountUser.FirstName = accountUser.FirstName;
                existingAccountUser.LastName = accountUser.LastName;
                existingAccountUser.PhoneNumber = accountUser.PhoneNumber;
                existingAccountUser.IsActive = accountUser.IsActive;
                existingAccountUser.LastModifiedAt = DateTime.Now.ToUniversalTime();

                _db.Update(existingAccountUser);
                await _db.SaveChangesAsync();

                return new RequestResponse(accountUser, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        // TODO: Need to handle situations where AccountUser is tied to other tables
        public async Task<RequestResponse> DeleteAccountUser(AccountUser accountUser)
        {

            var parentTableName = "AccountUser";
            var fkColumn = "AccountUserId";


            await using var conn = new NpgsqlConnection(_db.Database.GetConnectionString());
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            string Q(string ident) => new NpgsqlCommandBuilder().QuoteIdentifier(ident);

            var sql = $@"
            DELETE FROM {Q(parentTableName)} t
            WHERE t.""Id"" = @id
            AND NOT EXISTS (
                SELECT 1 FROM {Q(parentTableName)} p
                WHERE p.{Q(fkColumn)} = @id
            );";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", accountUser.Id);
            var affected = await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            if (affected == 0)
                // TODO: Handle situation where user is tied to existing accounts
                return new RequestResponse(null, 500, "Can't delete. EntityType in use"); 
            return new RequestResponse(accountUser, 200, $"Deleted AccountUser: {accountUser.Email} from AccountUsers");


        }

        //=====================================================================================
        // GEO DATE TIME DATA
        //=====================================================================================  
        public async Task<RequestResponse> GetCountryData()
        {
            return null;
        }

        public async Task<RequestResponse> GetTimeZoneData()
        {
            return null;
        }
    }
}
