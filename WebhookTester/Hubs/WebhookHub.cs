using Microsoft.AspNetCore.SignalR;

namespace WebhookTester.Hubs
{
    public class WebhookHub : Hub
    {
        public async Task JoinGroup(string webhookId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, webhookId);
        }

        public async Task LeaveGroup(string webhookId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, webhookId);
        }
    }
}
