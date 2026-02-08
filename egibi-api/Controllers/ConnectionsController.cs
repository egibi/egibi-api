using egibi_api.Data.Entities;
using egibi_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EgibiCoreLibrary.Models;

namespace egibi_api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ConnectionsController : ControllerBase
    {
        private readonly ConnectionsService _connectionsService;

        public ConnectionsController(ConnectionsService connectionsService)
        {
            _connectionsService = connectionsService;
        }

        [HttpGet("get-connections")]
        public async Task<RequestResponse> GetConnections()
        {
            return await _connectionsService.GetConnections();
        }

        [HttpGet("get-connection")]
        public async Task<RequestResponse> GetConnection(int connectionId)
        {
            return await _connectionsService.GetConnection(connectionId);
        }

        [HttpGet("get-connection-types")]
        public async Task<RequestResponse> GetConnectionTypes()
        {
            return await _connectionsService.GetConnectionTypes();
        }

        [HttpPost("save-connection")]
        public async Task<RequestResponse> SaveConnection(Connection connection)
        {
            return await _connectionsService.SaveConnection(connection);
        }
        
        [HttpDelete("delete-connections")]
        public async Task<RequestResponse> DeleteConnections(List<int> connectionIds)
        {
            return await _connectionsService.DeleteConnections(connectionIds);
        }

        [HttpDelete("delete-connection")]
        public async Task<RequestResponse> DeleteConnection(int id)
        {
            return await _connectionsService.DeleteConnection(id);
        }
    }
}
