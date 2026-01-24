using Microsoft.AspNetCore.SignalR;

namespace egibi_api.Hubs
{
    public class FileUpload : Hub
    {
        public async Task UploadFile(string fileName, string base64Content)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                await Clients.Caller.SendAsync("UploadProgress", "Start file upload...");

                //Convert base64 to byte array
                byte[] fileBytes = Convert.FromBase64String(base64Content);
                await Clients.Caller.SendAsync("UploadProgress", $"File received: {fileName} ({fileBytes.Length} bytes)");

                // Simulate file process with progress updates
                await ProcessFile(fileBytes, fileName, connectionId);
            }
            catch(Exception ex)
            {
                await Clients.Caller.SendAsync("UploadProgress", $"Error: {ex.Message}");
                await Clients.Caller.SendAsync("UploadError", ex.Message);
            }
        }

        private async Task ProcessFile(byte[] fileBytes, string fileName, string connectionId)
        {
            //Simulate processing the file in chunks
            int totalChunks = 10;
            int chunkSize = fileBytes.Length / totalChunks;

            for(int i = 0; i < totalChunks; i++)
            {
                // Simulate some processing time
                await Task.Delay(500);

                int progress = ((i + 1) * 100) / totalChunks;

                await Clients.Client(connectionId).SendAsync("UploadProgress",
                    $"Processing chunk {i + 1}/{totalChunks} ({progress}%");
            }

            // Save file or do actual process here
            string savepath = Path.Combine("uploads", fileName);
            Directory.CreateDirectory("uploads");
            await File.WriteAllBytesAsync(savepath, fileBytes);

            await Clients.Client(connectionId).SendAsync("UploadProgress",
                $"File save to: {savepath}");
        }
    }
}
