using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SmartDeliverySystem.DTOs;

namespace SmartDeliverySystem.Services
{
    public interface ITableStorageService
    {
        Task SaveLocationHistoryAsync(int deliveryId, LocationUpdateDto locationUpdate);
        Task<List<LocationHistoryDto>> GetLocationHistoryAsync(int deliveryId);
    }

    public class TableStorageService : ITableStorageService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<TableStorageService> _logger;

        public TableStorageService(TableServiceClient tableServiceClient, ILogger<TableStorageService> logger)
        {
            _tableClient = tableServiceClient.GetTableClient("LocationHistory");
            _tableClient.CreateIfNotExistsAsync();
            _logger = logger;
        }

        public async Task SaveLocationHistoryAsync(int deliveryId, LocationUpdateDto locationUpdate)
        {
            var entity = new TableEntity($"Delivery_{deliveryId}", DateTime.UtcNow.Ticks.ToString())
            {
                ["DeliveryId"] = deliveryId,
                ["Latitude"] = locationUpdate.Latitude,
                ["Longitude"] = locationUpdate.Longitude,
                ["Speed"] = locationUpdate.Speed,
                ["Notes"] = locationUpdate.Notes,
                ["Timestamp"] = DateTime.UtcNow
            };

            await _tableClient.AddEntityAsync(entity);
            _logger.LogInformation("Location saved to Table Storage for delivery {DeliveryId}", deliveryId);
        }

        public async Task<List<LocationHistoryDto>> GetLocationHistoryAsync(int deliveryId)
        {
            var entities = _tableClient.Query<TableEntity>(e => e.PartitionKey == $"Delivery_{deliveryId}");

            return entities.Select(e => new LocationHistoryDto
            {
                Latitude = e.GetDouble("Latitude") ?? 0,
                Longitude = e.GetDouble("Longitude") ?? 0,
                Speed = e.GetDouble("Speed"),
                Notes = e.GetString("Notes"),
                Timestamp = e.GetDateTime("Timestamp") ?? DateTime.UtcNow
            }).OrderBy(h => h.Timestamp).ToList();
        }
    }
}
