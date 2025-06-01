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
            catch(Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveConnection(Connection connection)
        {
            if (connection.Id == 0)
                return await CreateNewConnection(connection);
            else
                return await UpdateExistingConnection(connection);
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
            int unknownConnectionId = _db.ConnectionTypes.FirstOrDefault(f => f.Name == "unknown").Id;

            Connection newConnection = new Connection
            {
                Name = connection.Name,
                Description = connection.Description,
                Id = connection.Id,
                BaseUrl = connection.BaseUrl,
                ApiKey = connection.ApiKey,
                //ApiSecretKey = Encryptor.EncryptString(connection.ApiSecretKey, _configOptions.EncryptionPassword)
                ApiSecretKey = connection.ApiSecretKey,
                IsDataSource = connection.IsDataSource
            };

            if (newConnection.Id == 0)
                newConnection.Id = unknownConnectionId;

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
                .Where(w => w.Id == connection.Id)
                .FirstOrDefaultAsync();

            existingConnection.Name = connection.Name;
            existingConnection.Description = connection.Description;
            existingConnection.Id = connection.Id;
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
