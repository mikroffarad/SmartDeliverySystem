using Microsoft.AspNetCore.SignalR;

namespace SmartDeliverySystem.Hubs
{
    public class DeliveryTrackingHub : Hub
    {
        public async Task JoinDeliveryGroup(string deliveryId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Delivery_{deliveryId}");
        }

        public async Task LeaveDeliveryGroup(string deliveryId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Delivery_{deliveryId}");
        }

        public async Task JoinAllDeliveries()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AllDeliveries");
        }
    }
}
