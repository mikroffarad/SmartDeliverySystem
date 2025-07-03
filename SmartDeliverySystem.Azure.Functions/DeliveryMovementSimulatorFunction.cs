using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace SmartDeliverySystem.Azure.Functions
{
    public class DeliveryMovementSimulatorFunction
    {
        private readonly ILogger<DeliveryMovementSimulatorFunction> _logger;
        private readonly HttpClient _httpClient;

        public DeliveryMovementSimulatorFunction(ILogger<DeliveryMovementSimulatorFunction> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        [Function("DeliveryMovementSimulator")]
        public async Task Run([TimerTrigger("0/1 * * * * *")] TimerInfo myTimer) // Кожні 5 секунд
        {
            _logger.LogInformation("🚛 Запуск симуляції руху доставок...");

            try
            {
                // Отримуємо активні доставки
                var response = await _httpClient.GetAsync("https://localhost:7183/api/delivery/tracking/active");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Не вдалося отримати активні доставки");
                    return;
                }

                var deliveriesJson = await response.Content.ReadAsStringAsync();
                var deliveries = JsonSerializer.Deserialize<List<DeliveryTrackingData>>(deliveriesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (deliveries == null || !deliveries.Any())
                {
                    _logger.LogInformation("Немає активних доставок для симуляції");
                    return;
                }
                foreach (var delivery in deliveries.Where(d => d.Status == 3)) // 3 = InTransit
                {
                    await SimulateDeliveryMovement(delivery);
                }

                _logger.LogInformation($"✅ Оброблено {deliveries.Count(d => d.Status == 3)} активних доставок");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка при симуляції руху доставок");
            }
        }

        private async Task SimulateDeliveryMovement(DeliveryTrackingData delivery)
        {
            try
            {
                // Перевіряємо, чи є всі необхідні координати
                if (!delivery.VendorLatitude.HasValue || !delivery.VendorLongitude.HasValue ||
                    !delivery.StoreLatitude.HasValue || !delivery.StoreLongitude.HasValue ||
                    !delivery.CurrentLatitude.HasValue || !delivery.CurrentLongitude.HasValue)
                {
                    _logger.LogWarning("Доставка {DeliveryId} не має всіх необхідних координат", delivery.DeliveryId);
                    return;
                }

                // Розраховуємо нову позицію
                var newPosition = CalculateNextPosition(
                    delivery.CurrentLatitude.Value, delivery.CurrentLongitude.Value,
                    delivery.StoreLatitude.Value, delivery.StoreLongitude.Value,
                    speedKmh: 50 // швидкість 50 км/год
                );

                // Відправляємо оновлення координат
                var locationUpdate = new
                {
                    latitude = newPosition.Latitude,
                    longitude = newPosition.Longitude,
                    speed = 50.0,
                    notes = "Автоматичне оновлення позиції"
                };

                var json = JsonSerializer.Serialize(locationUpdate);
                var content = new StringContent(json, Encoding.UTF8, "application/json"); var updateResponse = await _httpClient.PostAsync(
                    $"https://localhost:7183/api/delivery/{delivery.DeliveryId}/update-location",
                    content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("📍 Оновлено позицію доставки {DeliveryId}: {Lat}, {Lon}",
                        delivery.DeliveryId, newPosition.Latitude, newPosition.Longitude);
                }
                else
                {
                    _logger.LogWarning("Не вдалося оновити позицію доставки {DeliveryId}", delivery.DeliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при симуляції руху доставки {DeliveryId}", delivery.DeliveryId);
            }
        }

        private (double Latitude, double Longitude) CalculateNextPosition(
            double currentLat, double currentLon,
            double targetLat, double targetLon,
            double speedKmh)
        {
            // Розрахунок відстані до цілі
            var distance = CalculateDistance(currentLat, currentLon, targetLat, targetLon);

            // Якщо дуже близько до цілі (менше 50 метрів), повертаємо координати цілі
            if (distance < 0.05) // 50 метрів
            {
                return (targetLat, targetLon);
            }

            // Розрахунок швидкості в координатах за 5 секунд
            var speedPerSecond = speedKmh / 3600.0; // км/сек
            var distanceToMove = speedPerSecond * 5; // відстань за 5 секунд

            // Обчислення пропорції руху
            var movementRatio = Math.Min(distanceToMove / distance, 1.0);

            // Нові координати
            var newLat = currentLat + (targetLat - currentLat) * movementRatio;
            var newLon = currentLon + (targetLon - currentLon) * movementRatio;

            return (newLat, newLon);
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return 6371 * c; // відстань в км
        }
    }
    public class DeliveryTrackingData
    {
        public int DeliveryId { get; set; }
        public int Status { get; set; } // Status приходить як число (enum)
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public double? VendorLatitude { get; set; }
        public double? VendorLongitude { get; set; }
        public double? StoreLatitude { get; set; }
        public double? StoreLongitude { get; set; }
    }
}
