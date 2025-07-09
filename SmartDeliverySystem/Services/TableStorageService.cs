using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SmartDeliverySystem.DTOs;

namespace SmartDeliverySystem.Services
{
    public interface ITableStorageService
    {
        Task<List<LocationHistoryDto>> GetLocationHistoryAsync(int deliveryId);
    }

    public class TableStorageService : ITableStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<TableStorageService> _logger;

        public TableStorageService(IConfiguration configuration, ILogger<TableStorageService> logger)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage");
            _tableServiceClient = new TableServiceClient(connectionString);
            _logger = logger;
        }

        public async Task<List<LocationHistoryDto>> GetLocationHistoryAsync(int deliveryId)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("LocationHistory");
                await tableClient.CreateIfNotExistsAsync();

                var filter = $"PartitionKey eq 'Delivery_{deliveryId}'";
                var entities = tableClient.QueryAsync<TableEntity>(filter);

                var history = new List<LocationHistoryDto>();

                await foreach (var entity in entities)
                {
                    history.Add(new LocationHistoryDto
                    {
                        Latitude = entity.GetDouble("Latitude") ?? 0,
                        Longitude = entity.GetDouble("Longitude") ?? 0,
                        Speed = entity.GetDouble("Speed"),
                        Notes = entity.GetString("Notes"),
                        Timestamp = entity.GetDateTimeOffset("Timestamp")?.DateTime ?? DateTime.UtcNow
                    });
                }

                // Sort by timestamp descending (newest first)
                history = history.OrderByDescending(h => h.Timestamp).ToList();

                _logger.LogInformation("Retrieved {Count} GPS records for delivery {DeliveryId}", history.Count, deliveryId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GPS history for delivery {DeliveryId}", deliveryId);
                return new List<LocationHistoryDto>();
            }
        }
    }
}
