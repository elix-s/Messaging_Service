using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MessagingService.Models;

namespace MessagingService.Services
{
    public class MessageBroadcaster
    {
        private readonly ILogger<MessageBroadcaster> _logger;
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new ConcurrentDictionary<Guid, WebSocket>();

        public MessageBroadcaster(ILogger<MessageBroadcaster> logger)
        {
            _logger = logger;
        }
        
        public async Task RegisterAsync(WebSocket socket)
        {
            Guid socketId = Guid.NewGuid();
            _sockets[socketId] = socket;
            _logger.LogInformation("New WebSocket connection: {SocketId}", socketId);

            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error WebSocket connection: {SocketId}", socketId);
            }
            finally
            {
                _sockets.TryRemove(socketId, out _);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                _logger.LogInformation("Closing WebSocket: {SocketId}", socketId);
            }
        }
        
        public async Task BroadcastMessageAsync(Message message)
        {
            if (_sockets.IsEmpty)
            {
                _logger.LogDebug("Not active WebSocket connection");
                return;
            }

            string json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            List<Task> tasks = new List<Task>();

            foreach (var pair in _sockets)
            {
                var socket = pair.Value;
                if (socket.State == WebSocketState.Open)
                {
                    tasks.Add(socket.SendAsync(new ArraySegment<byte>(data),
                        WebSocketMessageType.Text, true, CancellationToken.None));
                }
            }
            
            await Task.WhenAll(tasks);
            _logger.LogInformation("Message sent to {Count} clients.", _sockets.Count);
        }
    }
}