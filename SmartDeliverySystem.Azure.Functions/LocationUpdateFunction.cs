using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Data.Tables;
using System.Text.Json;

namespace SmartDeliverySystem.Azure.Functions
{    public class LocationUpdateFunction
    {
        private readonly ILogger<LocationUpdateFunction> _logger;
        private readonly TableServiceClient _tableServiceClient;

        public LocationUpdateFunction(ILogger<LocationUpdateFunction> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
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
                var locationData = JsonSerializer.Deserialize<LocationUpdateMessage>(message.Body.ToString()); if (locationData != null)
                {
                    _logger.LogInformation("🚛 Delivery {DeliveryId} at coordinates: {Lat}, {Lon}",
                        locationData.DeliveryId, locationData.Latitude, locationData.Longitude);

                    // Save to Table Storage for history
                    await SaveLocationToTableStorage(locationData);
                }

                _logger.LogInformation("✅ GPS дані успішно оброблені!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка обробки GPS даних");
                throw;
            }
        }

        private async Task SaveLocationToTableStorage(LocationUpdateMessage locationData)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("LocationHistory");
                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity($"Delivery_{locationData.DeliveryId}", DateTime.UtcNow.Ticks.ToString())
                {
                    ["DeliveryId"] = locationData.DeliveryId,
                    ["Latitude"] = locationData.Latitude,
                    ["Longitude"] = locationData.Longitude,
                    ["Speed"] = locationData.Speed,
                    ["Notes"] = locationData.Notes ?? "",
                    ["Timestamp"] = locationData.Timestamp
                };

                await tableClient.AddEntityAsync(entity);
                _logger.LogInformation("💾 GPS data saved to Table Storage for delivery {DeliveryId}", locationData.DeliveryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to save GPS data to Table Storage for delivery {DeliveryId}", locationData.DeliveryId);
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
