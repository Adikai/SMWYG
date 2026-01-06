using Microsoft.AspNetCore.SignalR;

namespace SMWYG.Api.Hubs
{
    public class ChatHub : Hub
    {
        public Task JoinChannel(string channelId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        }

        public Task LeaveChannel(string channelId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);
        }
    }
}
