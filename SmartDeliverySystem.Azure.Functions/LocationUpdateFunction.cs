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
        public void Run([ServiceBusTrigger("location-updates", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
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

                    // TODO:
                    // 1. Зберегти в Table Storage
                    // 2. Оновити поточну позицію в SQL
                    // 3. Надіслати через SignalR для real-time оновлень
                }

                _logger.LogInformation("✅ GPS дані успішно оброблені!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка обробки GPS даних");
                throw;
            }
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
