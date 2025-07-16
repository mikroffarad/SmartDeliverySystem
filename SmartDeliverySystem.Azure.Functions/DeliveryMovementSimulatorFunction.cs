using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using SmartDeliverySystem.Azure.Functions.DTOs;

namespace SmartDeliverySystem.Azure.Functions
{
    public class DeliveryMovementSimulatorFunction
    {
        private readonly ILogger<DeliveryMovementSimulatorFunction> _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<int, RouteSimulation> _activeRoutes = new();

        public DeliveryMovementSimulatorFunction(ILogger<DeliveryMovementSimulatorFunction> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }
        [Function("DeliveryMovementSimulator")]
        public async Task Run([TimerTrigger("0/1 * * * * *")] TimerInfo myTimer) // Кожну секунду
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _logger.LogInformation("🚛 [{Timestamp}] Запуск симуляції руху доставок...", timestamp);

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
                // Логуємо тільки кількість доставок, а не весь JSON
                _logger.LogInformation("📦 Отримано JSON доставок (символів: {Length})", deliveriesJson.Length);

                var deliveries = JsonSerializer.Deserialize<List<DeliveryTrackingData>>(deliveriesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (deliveries == null || !deliveries.Any())
                {
                    _logger.LogInformation("Немає активних доставок для симуляції");
                    return;
                }

                _logger.LogInformation("📋 Всього доставок: {Total}, InTransit: {InTransit}",
                    deliveries.Count, deliveries.Count(d => d.Status == 3));

                foreach (var delivery in deliveries.Where(d => d.Status == 3)) // 3 = InTransit
                {
                    _logger.LogInformation("🚛 Обробляю доставку {DeliveryId} зі статусом {Status}",
                        delivery.DeliveryId, delivery.Status);
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
                _logger.LogInformation("🚛 === Симуляція руху для доставки {DeliveryId} ===", delivery.DeliveryId);

                // Перевіряємо, чи є всі необхідні координати
                if (!delivery.VendorLatitude.HasValue || !delivery.VendorLongitude.HasValue ||
                    !delivery.StoreLatitude.HasValue || !delivery.StoreLongitude.HasValue)
                {
                    _logger.LogWarning("Доставка {DeliveryId} не має всіх необхідних координат", delivery.DeliveryId);
                    return;
                }

                // Отримуємо або створюємо маршрут
                RouteSimulation routeSimulation;

                if (!_activeRoutes.ContainsKey(delivery.DeliveryId))
                {
                    _logger.LogInformation("🗺️ Створення нового маршруту для доставки {DeliveryId}", delivery.DeliveryId);

                    var route = await GetRouteFromOSRM(
                        delivery.VendorLatitude.Value, delivery.VendorLongitude.Value,
                        delivery.StoreLatitude.Value, delivery.StoreLongitude.Value);

                    if (route == null || !route.Any())
                    {
                        _logger.LogWarning("❌ Не вдалося отримати маршрут для доставки {DeliveryId}", delivery.DeliveryId);
                        return;
                    }

                    // Отримуємо збережений індекс з бази даних або починаємо з 0
                    var savedIndex = await GetSavedRouteIndexAsync(delivery.DeliveryId);

                    routeSimulation = new RouteSimulation
                    {
                        RoutePoints = route,
                        CurrentIndex = savedIndex,
                        StartTime = DateTime.UtcNow
                    };

                    _activeRoutes[delivery.DeliveryId] = routeSimulation;

                    _logger.LogInformation("✅ Створено маршрут для доставки {DeliveryId} з {PointCount} точок, починаючи з індексу {Index}",
                        delivery.DeliveryId, route.Count, savedIndex);
                }
                else
                {
                    routeSimulation = _activeRoutes[delivery.DeliveryId];
                    _logger.LogInformation("🔄 Використовую існуючий маршрут для доставки {DeliveryId}", delivery.DeliveryId);
                }

                // Логування прогресу
                _logger.LogInformation("📊 Доставка {DeliveryId}: точка {CurrentIndex}/{TotalPoints}",
                    delivery.DeliveryId, routeSimulation.CurrentIndex, routeSimulation.RoutePoints.Count);                // Отримуємо наступну точку маршруту
                var nextPosition = GetNextRoutePosition(routeSimulation);

                if (nextPosition == null)
                {
                    // Маршрут завершено - прибуття на місце призначення
                    _logger.LogInformation("🎯 Доставка {DeliveryId} досягла призначення", delivery.DeliveryId);
                    _activeRoutes.Remove(delivery.DeliveryId);
                    await ClearSavedRouteIndexAsync(delivery.DeliveryId);                    // Відправляємо оновлення з спеціальною позначкою про прибуття
                    await UpdateDeliveryLocation(delivery.DeliveryId,
                        delivery.StoreLatitude.Value, delivery.StoreLongitude.Value,
                        0, "🎯 Прибуття на місце призначення");

                    // Затримка 5 секунд, потім оновлюємо статус на Delivered
                    await Task.Delay(3000);

                    try
                    {
                        var statusUpdateContent = new StringContent("4", Encoding.UTF8, "application/json"); // 4 = Delivered
                        var statusResponse = await _httpClient.PutAsync(
                            $"https://localhost:7183/api/delivery/{delivery.DeliveryId}/status",
                            statusUpdateContent);

                        if (statusResponse.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("✅ Статус доставки {DeliveryId} оновлено на Delivered", delivery.DeliveryId);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Не вдалося оновити статус доставки {DeliveryId}", delivery.DeliveryId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Помилка при оновленні статусу доставки {DeliveryId}", delivery.DeliveryId);
                    }

                    return;
                }

                // Перевіряємо, чи це передостання точка
                if (routeSimulation.CurrentIndex >= routeSimulation.RoutePoints.Count - 1)
                {
                    _logger.LogInformation("🎯 Доставка {DeliveryId} майже прибула! Наступна точка - остання.", delivery.DeliveryId);
                }

                // Зберігаємо поточний індекс
                await SaveRouteIndexAsync(delivery.DeliveryId, routeSimulation.CurrentIndex);

                // Оновлюємо позицію доставки
                await UpdateDeliveryLocation(delivery.DeliveryId,
                    nextPosition.Value.Latitude, nextPosition.Value.Longitude,
                    50, "🚛 Рух");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка симуляції руху доставки {DeliveryId}", delivery.DeliveryId);
            }
        }
        private async Task<List<RoutePoint>> GetRouteFromOSRM(double fromLat, double fromLon, double toLat, double toLon)
        {
            try
            {
                // Використовуємо публічний OSRM API для отримання маршруту
                // Важливо: використовуємо InvariantCulture для правильного форматування координат (крапка замість коми)
                var url = $"http://router.project-osrm.org/route/v1/driving/{fromLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{fromLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{toLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{toLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=full&geometries=geojson";

                _logger.LogInformation("🌐 OSRM URL: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OSRM API запит не вдався: {StatusCode}", response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("OSRM Error: {Error}", errorContent);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var osrmResponse = JsonSerializer.Deserialize<OSRMResponse>(jsonContent);

                if (osrmResponse?.routes == null || !osrmResponse.routes.Any())
                {
                    _logger.LogWarning("OSRM повернув порожній маршрут");
                    return null;
                }

                var coordinates = osrmResponse.routes[0].geometry.coordinates;
                var routePoints = coordinates.Select(coord => new RoutePoint
                {
                    Latitude = coord[1], // В GeoJSON спочатку longitude, потім latitude
                    Longitude = coord[0]
                }).ToList();                // Зменшуємо кількість точок для плавнішого руху (кожна n-на точка)
                var simplifiedRoute = routePoints.Where((point, index) => index % 2 == 0).ToList();

                // ВАЖЛИВО: Додаємо точну кінцеву точку магазину
                var storePoint = new RoutePoint
                {
                    Latitude = toLat, // Точні координати магазину
                    Longitude = toLon
                };

                // Замінюємо останню точку на точні координати магазину
                if (simplifiedRoute.Any())
                {
                    simplifiedRoute[simplifiedRoute.Count - 1] = storePoint;
                }
                else
                {
                    simplifiedRoute.Add(storePoint);
                }

                return simplifiedRoute;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні маршруту з OSRM");
                return null;
            }
        }
        private (double Latitude, double Longitude)? GetNextRoutePosition(RouteSimulation routeSimulation)
        {
            if (routeSimulation.CurrentIndex >= routeSimulation.RoutePoints.Count)
            {
                return null; // Маршрут завершено
            }

            var currentPoint = routeSimulation.RoutePoints[routeSimulation.CurrentIndex];
            routeSimulation.CurrentIndex++;

            _logger.LogInformation("🎯 Переходжу до точки {Index}/{Total}: {Lat}, {Lon}",
                routeSimulation.CurrentIndex, routeSimulation.RoutePoints.Count,
                currentPoint.Latitude, currentPoint.Longitude);

            return (currentPoint.Latitude, currentPoint.Longitude);
        }
        private async Task UpdateDeliveryLocation(int deliveryId, double latitude, double longitude, double speed, string notes)
        {
            try
            {
                var locationUpdate = new
                {
                    latitude = latitude,
                    longitude = longitude,
                    speed = speed,
                    notes = notes
                };

                var json = JsonSerializer.Serialize(locationUpdate);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("🌐 Відправляю оновлення позиції для доставки {DeliveryId}: {Lat}, {Lon}",
                    deliveryId, latitude, longitude);

                var updateResponse = await _httpClient.PostAsync(
                    $"https://localhost:7183/api/delivery/{deliveryId}/update-location",
                    content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Успішно оновлено позицію доставки {DeliveryId}", deliveryId);
                }
                else
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("❌ Не вдалося оновити позицію доставки {DeliveryId}: {StatusCode}, {Error}",
                        deliveryId, updateResponse.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка при оновленні позиції доставки {DeliveryId}", deliveryId);
            }
        }

        private async Task<int> GetSavedRouteIndexAsync(int deliveryId)
        {
            try
            {
                // Простий підхід: використовуємо HTTP запит до API для отримання збереженого індексу
                var response = await _httpClient.GetAsync($"https://localhost:7183/api/delivery/{deliveryId}/route-index");
                if (response.IsSuccessStatusCode)
                {
                    var indexString = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(indexString, out int savedIndex))
                    {
                        _logger.LogInformation("📍 Знайдено збережений індекс {Index} для доставки {DeliveryId}", savedIndex, deliveryId);
                        return savedIndex;
                    }
                }

                _logger.LogInformation("📍 Збережений індекс не знайдено для доставки {DeliveryId}, починаємо з 0", deliveryId);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Помилка при отриманні збереженого індексу для доставки {DeliveryId}", deliveryId);
                return 0;
            }
        }

        private async Task SaveRouteIndexAsync(int deliveryId, int currentIndex)
        {
            try
            {
                var indexData = new { index = currentIndex };
                var json = JsonSerializer.Serialize(indexData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"https://localhost:7183/api/delivery/{deliveryId}/route-index", content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("💾 Збережено індекс {Index} для доставки {DeliveryId}", currentIndex, deliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Помилка при збереженні індексу для доставки {DeliveryId}", deliveryId);
            }
        }

        private async Task ClearSavedRouteIndexAsync(int deliveryId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://localhost:7183/api/delivery/{deliveryId}/route-index");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("🗑️ Очищено збережений індекс для доставки {DeliveryId}", deliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Помилка при очищенні індексу для доставки {DeliveryId}", deliveryId);
            }
        }
    }

    // Допоміжні класи для роботи з маршрутами
    public class RouteSimulation
    {
        public List<RoutePoint> RoutePoints { get; set; } = new();
        public int CurrentIndex { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class RoutePoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // Класи для десеріалізації OSRM відповіді
    public class OSRMResponse
    {
        public OSRMRoute[] routes { get; set; }
    }

    public class OSRMRoute
    {
        public OSRMGeometry geometry { get; set; }
        public double distance { get; set; }
        public double duration { get; set; }
    }

    public class OSRMGeometry
    {
        public double[][] coordinates { get; set; }
    }
}
