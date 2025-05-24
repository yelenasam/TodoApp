using Microsoft.AspNetCore.SignalR;

namespace TodoApp.Server.Hubs
{
    /// <summary>
    /// SignalR hub, the server uses it to push tasks updates
    /// real-time communication between the server and connected clients.
    /// </summary>
    public class TaskItemsHub : Hub
    {
    }
}
