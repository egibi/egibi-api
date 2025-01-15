using egibi_api.Data.Entities;
using egibi_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConnectionsController : ControllerBase
    {
        private readonly ConnectionsService _connectionsService;

        public ConnectionsController(ConnectionsService connectionsService)
        {
            _connectionsService = connectionsService;
        }

        [HttpGet(Name = "get-connections")]
        public async Task<RequestResponse> GetConnections()
        {
            return await _connectionsService.GetConnections();
        }

        [HttpGet(Name = "get-connection")]
        public async Task<RequestResponse>GetConnection(int connectionId)
        {
            return await _connectionsService.GetConnection(connectionId);
        }

        [HttpPost(Name = "save-connection")]
        public async Task<RequestResponse> SaveConnection(Connection connection)
        {
            return await _connectionsService.SaveConnection(connection);
        }

        [HttpDelete(Name = "delete-connections")]
        public async Task<RequestResponse> DeleteConnections(List<int> connectionIds)
        {
            return await _connectionsService.DeleteConnections(connectionIds);
        }

        [HttpDelete(Name = "delete-connection")]
        public async Task<RequestResponse> DeleteConnection(int connectionId)
        {
            return await _connectionsService.DeleteConnection(connectionId);
        }
    }
}
