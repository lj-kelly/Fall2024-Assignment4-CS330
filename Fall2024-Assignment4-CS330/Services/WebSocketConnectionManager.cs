using System.Net.WebSockets;

namespace Fall2024_Assignment4_CS330.Services
{
    public class WebSocketConnectionManager
    {
        public static readonly Dictionary<string, WebSocket> ActiveSockets = new();

        public static void AddSocket(string userId, WebSocket socket) 
        {
            if (!ActiveSockets.ContainsKey(userId))
            {
                ActiveSockets[userId] = socket;
            }
        }

        public static void RemoveSocket(string userId)
        {
            if (ActiveSockets.ContainsKey(userId))
            {
                ActiveSockets.Remove(userId);
            }
        }
    }
}
