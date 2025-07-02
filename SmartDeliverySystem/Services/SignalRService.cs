using Microsoft.AspNetCore.SignalR;
using SmartDeliverySystem.Hubs;

namespace SmartDeliverySystem.Services
{
    public interface ISignalRService
    {
        Task SendLocationUpdateAsync(int deliveryId, double latitude, double longitude, string? notes = null);
        Task SendDeliveryStatusUpdateAsync(int deliveryId, string status);
    }

    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<DeliveryTrackingHub> _hubContext;
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(IHubContext<DeliveryTrackingHub> hubContext, ILogger<SignalRService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }
        public async Task SendLocationUpdateAsync(int deliveryId, double latitude, double longitude, string? notes = null)
        {
            var locationData = new
            {
                deliveryId = deliveryId,      // lowercase для JavaScript
                latitude = latitude,
                longitude = longitude,
                notes = notes,
                timestamp = DateTime.UtcNow
            };

            // Надіслати конкретній доставці
            await _hubContext.Clients.Group($"Delivery_{deliveryId}")
                .SendAsync("LocationUpdated", locationData);

            // Надіслати всім хто слідкує за всіма доставками
            await _hubContext.Clients.Group("AllDeliveries")
                .SendAsync("LocationUpdated", locationData);

            _logger.LogInformation("📡 SignalR: Location update sent for delivery {DeliveryId} - {Lat}, {Lon}", deliveryId, latitude, longitude);
        }

        public async Task SendDeliveryStatusUpdateAsync(int deliveryId, string status)
        {
            var statusData = new
            {
                DeliveryId = deliveryId,
                Status = status,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"Delivery_{deliveryId}")
                .SendAsync("StatusUpdated", statusData);

            await _hubContext.Clients.Group("AllDeliveries")
                .SendAsync("StatusUpdated", statusData);

            _logger.LogInformation("📡 SignalR: Status update sent for delivery {DeliveryId}: {Status}", deliveryId, status);
        }
    }
}
