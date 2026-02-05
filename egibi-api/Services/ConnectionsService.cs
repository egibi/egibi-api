#nullable disable
using egibi_api.Data;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Connection = egibi_api.Data.Entities.Connection;

namespace egibi_api.Services
{
    public class ConnectionsService
    {
        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        public ConnectionsService(EgibiDbContext db, IOptions<ConfigOptions> configOptions)
        {
            _db = db;
            _configOptions = configOptions.Value;
        }

        public async Task<RequestResponse> GetConnections()
        {
            try
            {
                List<Connection> connections = await _db.Connections
                    .Include("ConnectionType")
                    .ToListAsync();

                return new RequestResponse(connections, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetConnection(int id)
        {
            try
            {
                var connection = await _db.Connections
                    .Include("ConnectionType")
                    .FirstOrDefaultAsync(x => x.Id == id);

                return new RequestResponse(connection, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetConnectionTypes()
        {
            try
            {
                var connectionTypes = await _db.ConnectionTypes.ToListAsync();
                return new RequestResponse(connectionTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveConnection(Connection connection)
        {
            try
            {
                if (connection.Id == 0)
                    return await CreateNewConnection(connection);
                else
                    return await UpdateExistingConnection(connection);
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteConnection(int id)
        {
            try
            {
                _db.Remove(_db.Connections
                    .Where(w => w.Id == id)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(id, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteConnections(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.Connections
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(ids, 500, "Problem Deleting", new ResponseError(ex));
            }
        }


        private async Task<RequestResponse> CreateNewConnection(Connection connection)
        {
            var apiType = await _db.ConnectionTypes.FirstOrDefaultAsync(f => f.Name == "api");
            int defaultTypeId = apiType?.Id ?? _db.ConnectionTypes.FirstOrDefault()?.Id ?? 1;

            Connection newConnection = new Connection
            {
                Name = connection.Name,
                Description = connection.Description,
                BaseUrl = connection.BaseUrl,
                ApiKey = connection.ApiKey,
                ApiSecretKey = connection.ApiSecretKey,
                IsDataSource = connection.IsDataSource,
                ConnectionTypeId = connection.ConnectionTypeId ?? defaultTypeId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,

                // Service catalog fields
                Category = connection.Category,
                IconKey = connection.IconKey,
                Color = connection.Color,
                Website = connection.Website,
                DefaultBaseUrl = connection.DefaultBaseUrl,
                RequiredFields = connection.RequiredFields,
                SortOrder = connection.SortOrder
            };

            try
            {
                await _db.AddAsync(newConnection);
                await _db.SaveChangesAsync();

                return new RequestResponse(newConnection, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingConnection(Connection connection)
        {
            try
            {
                Connection existing = await _db.Connections
                    .Where(w => w.Id == connection.Id)
                    .FirstOrDefaultAsync();

                if (existing == null)
                    return new RequestResponse(null, 404, "Connection not found");

                // Legacy fields
                existing.Name = connection.Name;
                existing.Description = connection.Description;
                existing.BaseUrl = connection.BaseUrl;
                existing.ApiKey = connection.ApiKey;
                existing.ApiSecretKey = connection.ApiSecretKey;
                existing.IsDataSource = connection.IsDataSource;
                existing.LastModifiedAt = DateTime.UtcNow;

                // Service catalog fields
                existing.Category = connection.Category;
                existing.IconKey = connection.IconKey;
                existing.Color = connection.Color;
                existing.Website = connection.Website;
                existing.DefaultBaseUrl = connection.DefaultBaseUrl;
                existing.RequiredFields = connection.RequiredFields;
                existing.SortOrder = connection.SortOrder;
                existing.ConnectionTypeId = connection.ConnectionTypeId ?? existing.ConnectionTypeId;

                _db.Update(existing);
                await _db.SaveChangesAsync();

                return new RequestResponse(existing, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}