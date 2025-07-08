using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using SmartDeliverySystem.Azure.Functions.DTOs;

namespace SmartDeliverySystem.Azure.Functions
{
    public class DeliveryStatusCheckerFunction
    {
        private readonly ILogger<DeliveryStatusCheckerFunction> _logger;
        private readonly HttpClient _httpClient;

        public DeliveryStatusCheckerFunction(ILogger<DeliveryStatusCheckerFunction> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [Function("DeliveryStatusChecker")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("🔍 Delivery status checker executed at: {Time}", DateTime.Now);

            try
            {
                // Get all active deliveries from API
                var response = await _httpClient.GetAsync("https://localhost:7183/api/delivery/tracking/active");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve active deliveries");
                    return;
                }

                var deliveriesJson = await response.Content.ReadAsStringAsync();
                var deliveries = JsonSerializer.Deserialize<List<DeliveryTrackingData>>(deliveriesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (deliveries == null || !deliveries.Any())
                {
                    _logger.LogInformation("No active deliveries to check");
                    return;
                }

                // Check for overdue deliveries (example: more than 2 hours old)
                var overdueDeliveries = deliveries.Where(d =>
                    d.CreatedAt.HasValue &&
                    DateTime.UtcNow - d.CreatedAt.Value > TimeSpan.FromHours(2)).ToList();

                if (overdueDeliveries.Any())
                {
                    _logger.LogWarning("Found {Count} overdue deliveries", overdueDeliveries.Count);
                    // TODO: Send notifications to administrators
                }

                // Check for deliveries with stale GPS data (no updates in 15 minutes)
                var staleGpsDeliveries = deliveries.Where(d =>
                    d.LastLocationUpdate.HasValue &&
                    DateTime.UtcNow - d.LastLocationUpdate.Value > TimeSpan.FromMinutes(15)).ToList();

                if (staleGpsDeliveries.Any())
                {
                    _logger.LogWarning("Found {Count} deliveries with stale GPS data", staleGpsDeliveries.Count);
                    // TODO: Alert about potential GPS tracking issues
                }

                _logger.LogInformation("✅ Status check completed. Processed {Count} deliveries", deliveries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during delivery status check");
                throw;
            }
        }
    }
}
