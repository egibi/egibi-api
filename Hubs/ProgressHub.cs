using Microsoft.AspNetCore.SignalR;

namespace egibi_api.Hubs
{
    public class ProgressHub : Hub
    {
        public async Task StartProcessing(string connectionId)
        {
            // Connection-specific logic if needed
            await Clients.Caller.SendAsync("Connected", connectionId);
        }
    }
}
