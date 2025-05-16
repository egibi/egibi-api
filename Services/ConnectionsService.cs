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

                //connections.ForEach(connection =>
                //{
                //    connection.ApiSecretKey =
                //        !string.IsNullOrWhiteSpace(connection.ApiKey) ?
                //        Encryptor.DecryptString(connection.ApiSecretKey, _configOptions.EncryptionPassword) : null;
                //});

                return new RequestResponse(connections, 200, "OK");

            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetConnection(int connectionId)
        {
            try
            {
                var connection = await _db.Connections
                    .Include("ConnectionType")
                    .FirstOrDefaultAsync(x => x.ConnectionID == connectionId);
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
            catch(Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveConnection(Connection connection)
        {
            if (connection.ConnectionID == 0)
                return await CreateNewConnection(connection);
            else
                return await UpdateExistingConnection(connection);
        }

        public async Task<RequestResponse> DeleteConnection(int connectionId)
        {
            try
            {
                _db.Remove(_db.Connections
                    .Where(w => w.ConnectionID == connectionId)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(connectionId, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(connectionId, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteConnections(List<int> connectionIds)
        {
            try
            {
                _db.RemoveRange(_db.Connections
                    .Where(w => connectionIds.Contains(w.ConnectionID)));
                await _db.SaveChangesAsync();

                return new RequestResponse(connectionIds, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(connectionIds, 500, "Problem Deleting", new ResponseError(ex));
            }
        }


        private async Task<RequestResponse> CreateNewConnection(Connection connection)
        {
            int unknownConnectionId = _db.ConnectionTypes.FirstOrDefault(f => f.Name == "unknown").ConnectionTypeID;

            Connection newConnection = new Connection
            {
                Name = connection.Name,
                Description = connection.Description,
                ConnectionTypeID = connection.ConnectionTypeID,
                BaseUrl = connection.BaseUrl,
                ApiKey = connection.ApiKey,
                //ApiSecretKey = Encryptor.EncryptString(connection.ApiSecretKey, _configOptions.EncryptionPassword)
                ApiSecretKey = connection.ApiSecretKey,
                IsDataSource = connection.IsDataSource
            };

            if (newConnection.ConnectionTypeID == 0)
                newConnection.ConnectionTypeID = unknownConnectionId;

            try
            {
                await _db.AddAsync(newConnection);
                await _db.SaveChangesAsync();

                return new RequestResponse(connection, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingConnection(Connection connection)
        {
            Connection existingConnection = await _db.Connections
                .Where(w => w.ConnectionID == connection.ConnectionID)
                .FirstOrDefaultAsync();

            existingConnection.Name = connection.Name;
            existingConnection.Description = connection.Description;
            existingConnection.ConnectionTypeID = connection.ConnectionTypeID;
            existingConnection.BaseUrl = connection.BaseUrl;
            existingConnection.ApiKey = connection.ApiKey;
            //existingConnection.ApiSecretKey = Encryptor.EncryptString(connection.ApiSecretKey, _configOptions.EncryptionPassword);
            existingConnection.ApiSecretKey = connection.ApiSecretKey;
            existingConnection.IsDataSource = connection.IsDataSource;

            try
            {
                _db.Update(existingConnection);
                await _db.SaveChangesAsync();

                return new RequestResponse(connection, 200, "OK");
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException?.Message;

                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
