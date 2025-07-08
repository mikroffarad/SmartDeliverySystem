using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace SmartDeliverySystem.Azure.Functions
{
    public class LocationUpdateFunction
    {
        private readonly ILogger<LocationUpdateFunction> _logger;

        public LocationUpdateFunction(ILogger<LocationUpdateFunction> logger)
        {
            _logger = logger;
        }
        [Function("LocationUpdate")]
        public async Task Run([ServiceBusTrigger("location-updates", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("📍 GPS оновлення отримано!");
            _logger.LogInformation("Message ID: {MessageId}", message.MessageId);
            _logger.LogInformation("Location data: {Body}", message.Body.ToString());

            try
            {
                // Десеріалізація GPS даних
                var locationData = JsonSerializer.Deserialize<LocationUpdateMessage>(message.Body.ToString());

                if (locationData != null)
                {
                    _logger.LogInformation("🚛 Delivery {DeliveryId} at coordinates: {Lat}, {Lon}",
                        locationData.DeliveryId, locationData.Latitude, locationData.Longitude);

                    // 1. Save to Table Storage for history
                    await SaveLocationToTableStorage(locationData);

                    // 2. Update current position in SQL via API
                    await UpdateCurrentLocationInDatabase(locationData);

                    // 3. Send through SignalR for real-time updates
                    await SendLocationUpdateViaSignalR(locationData);
                }

                _logger.LogInformation("✅ GPS дані успішно оброблені!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка обробки GPS даних");
                throw;
            }
        }
        private Task SaveLocationToTableStorage(LocationUpdateMessage locationData)
        {
            // TODO: Implement Table Storage saving
            _logger.LogInformation("💾 Saving GPS data to Table Storage for delivery {DeliveryId}", locationData.DeliveryId);

            // This would connect to Azure Table Storage and save GPS history
            // Implementation depends on your table storage setup
            return Task.CompletedTask;
        }

        private Task UpdateCurrentLocationInDatabase(LocationUpdateMessage locationData)
        {
            // TODO: Implement API call to update current location in SQL database
            _logger.LogInformation("🔄 Updating current location in database for delivery {DeliveryId}", locationData.DeliveryId);

            // This would make HTTP call to your main API to update current delivery position
            return Task.CompletedTask;
        }

        private Task SendLocationUpdateViaSignalR(LocationUpdateMessage locationData)
        {
            // TODO: Implement SignalR notification
            _logger.LogInformation("📡 Sending real-time update via SignalR for delivery {DeliveryId}", locationData.DeliveryId);

            // This would send real-time update to connected clients
            return Task.CompletedTask;
        }
    }

    public class LocationUpdateMessage
    {
        public int DeliveryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
