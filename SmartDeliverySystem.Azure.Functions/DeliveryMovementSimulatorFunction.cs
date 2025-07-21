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
        private readonly Dictionary<int, RouteSimulationDto> _activeRoutes = new();

        public DeliveryMovementSimulatorFunction(ILogger<DeliveryMovementSimulatorFunction> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }
        [Function("DeliveryMovementSimulator")]
        public async Task Run([TimerTrigger("0/1 * * * * *")] TimerInfo myTimer) // Run task each second
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _logger.LogInformation("🚛 [{Timestamp}] Launching a delivery simulation...", timestamp);

            try
            {
                // Retrieving active deliveries
                var response = await _httpClient.GetAsync("https://localhost:7183/api/delivery/tracking/active");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve active deliveries");
                    return;
                }

                var deliveriesJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📦 Received JSON of deliveries (chars: {Length})", deliveriesJson.Length);

                var deliveries = JsonSerializer.Deserialize<List<DeliveryTrackingData>>(deliveriesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (deliveries == null || !deliveries.Any())
                {
                    _logger.LogInformation("No active deliveries to simulate.");
                    return;
                }

                _logger.LogInformation("📋 Total deliveries: {Total}, InTransit: {InTransit}",
                    deliveries.Count, deliveries.Count(d => d.Status == 3));

                foreach (var delivery in deliveries.Where(d => d.Status == 3)) // 3 = InTransit
                {
                    _logger.LogInformation("🚛 Processing delivery {DeliveryId} with status {Status}",
                        delivery.DeliveryId, delivery.Status);
                    await SimulateDeliveryMovement(delivery);
                }

                _logger.LogInformation($"✅ Processed {deliveries.Count(d => d.Status == 3)} active deliveries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error simulating delivery traffic");
            }
        }
        private async Task SimulateDeliveryMovement(DeliveryTrackingData delivery)
        {
            try
            {
                _logger.LogInformation("🚛 === Traffic simulation for delivery {DeliveryId} ===", delivery.DeliveryId);

                // Check if all necessary coordinates are available
                if (!delivery.VendorLatitude.HasValue || !delivery.VendorLongitude.HasValue ||
                    !delivery.StoreLatitude.HasValue || !delivery.StoreLongitude.HasValue)
                {
                    _logger.LogWarning("Delivery {DeliveryId} does not have all the required coordinates", delivery.DeliveryId);
                    return;
                }

                // Get or create route simulation
                RouteSimulationDto routeSimulation;

                if (!_activeRoutes.ContainsKey(delivery.DeliveryId))
                {
                    _logger.LogInformation("🗺️ Creating a new delivery route {DeliveryId}", delivery.DeliveryId);

                    var route = await GetRouteFromOSRM(
                        delivery.VendorLatitude.Value, delivery.VendorLongitude.Value,
                        delivery.StoreLatitude.Value, delivery.StoreLongitude.Value);

                    if (route == null || !route.Any())
                    {
                        _logger.LogWarning("❌ Failed to retrieve delivery route {DeliveryId}", delivery.DeliveryId);
                        return;
                    }

                    // Get the stored index from the database or start from 0
                    var savedIndex = await GetSavedRouteIndexAsync(delivery.DeliveryId);

                    routeSimulation = new RouteSimulationDto
                    {
                        RoutePoints = route,
                        CurrentIndex = savedIndex,
                        StartTime = DateTime.UtcNow
                    };

                    _activeRoutes[delivery.DeliveryId] = routeSimulation;

                    _logger.LogInformation("✅ Route created for delivery {DeliveryId} from {PointCount} points, starting at index {Index}",
                        delivery.DeliveryId, route.Count, savedIndex);
                }
                else
                {
                    routeSimulation = _activeRoutes[delivery.DeliveryId];
                    _logger.LogInformation("🔄 Using an existing delivery route {DeliveryId}", delivery.DeliveryId);
                }

                // Progress logging
                _logger.LogInformation("📊 Delivery {DeliveryId}: point {CurrentIndex}/{TotalPoints}",
                    delivery.DeliveryId, routeSimulation.CurrentIndex, routeSimulation.RoutePoints.Count);

                // Retrieving the next route position
                var nextPosition = GetNextRoutePosition(routeSimulation);

                if (nextPosition == null)
                {
                    // Route completed - arrival at destination
                    _logger.LogInformation("🎯 Доставка {DeliveryId} досягла призначення", delivery.DeliveryId);
                    
                    _activeRoutes.Remove(delivery.DeliveryId);
                    
                    await ClearSavedRouteIndexAsync(delivery.DeliveryId);

                    // Send updates with a special arrival mark
                    await UpdateDeliveryLocation(delivery.DeliveryId,
                        delivery.StoreLatitude.Value, delivery.StoreLongitude.Value,
                        0, "🎯 Arrival at destination");

                    // Delay 5 seconds, then update the status to Delivered
                    await Task.Delay(3000);

                    try
                    {
                        var statusUpdateContent = new StringContent("4", Encoding.UTF8, "application/json"); // 4 = Delivered
                        var statusResponse = await _httpClient.PutAsync(
                            $"https://localhost:7183/api/delivery/{delivery.DeliveryId}/status",
                            statusUpdateContent);

                        if (statusResponse.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("✅ Delivery status {DeliveryId} updated to Delivered", delivery.DeliveryId);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Failed to update delivery status {DeliveryId}", delivery.DeliveryId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error updating delivery status {DeliveryId}", delivery.DeliveryId);
                    }

                    return;
                }

                // Check if this is the penultimate point
                if (routeSimulation.CurrentIndex >= routeSimulation.RoutePoints.Count - 1)
                {
                    _logger.LogInformation("🎯 Delivery {DeliveryId} is almost here! The next point is the last.", delivery.DeliveryId);
                }

                // Save the current index
                await SaveRouteIndexAsync(delivery.DeliveryId, routeSimulation.CurrentIndex);

                // Update delivery position
                await UpdateDeliveryLocation(delivery.DeliveryId,
                    nextPosition.Value.Latitude, nextPosition.Value.Longitude,
                    50, "🚛 Movement");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Delivery movement simulation error {DeliveryId}", delivery.DeliveryId);
            }
        }
        private async Task<List<RoutePointDto>?> GetRouteFromOSRM(double fromLat, double fromLon, double toLat, double toLon)
        {
            try
            {
                // Using the OSRM API to get the route
                // Important: use InvariantCulture for correct coordinate formatting (dot instead of comma)
                var url = $"http://localhost:5000/route/v1/driving/{fromLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{fromLat.ToString(System.Globalization.CultureInfo.InvariantCulture)};{toLon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{toLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=full&geometries=geojson";

                _logger.LogInformation("🌐 OSRM URL: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OSRM API request failed: {StatusCode}", response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("OSRM Error: {Error}", errorContent);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var osrmResponse = JsonSerializer.Deserialize<OSRMResponseDto>(jsonContent);

                if (osrmResponse?.routes == null || !osrmResponse.routes.Any())
                {
                    _logger.LogWarning("OSRM returned an empty route");
                    return null;
                }

                var coordinates = osrmResponse.routes[0].geometry.coordinates;
                var routePoints = coordinates.Select(coord => new RoutePointDto
                {
                    Latitude = coord[1], // In GeoJSON longitude first, then latitude
                    Longitude = coord[0]
                }).ToList();

                // Reduce the number of points for smoother motion (every nth point)
                var simplifiedRoute = routePoints.Where((point, index) => index % 2 == 0).ToList();

                // Add the exact store endpoint
                var storePoint = new RoutePointDto
                {
                    Latitude = toLat, 
                    Longitude = toLon
                };

                // Replace the last point with the exact coordinates of the store
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
                _logger.LogError(ex, "Error getting route from OSRM");
                return null;
            }
        }
        private (double Latitude, double Longitude)? GetNextRoutePosition(RouteSimulationDto routeSimulation)
        {
            if (routeSimulation.CurrentIndex >= routeSimulation.RoutePoints.Count)
            {
                return null; // Route is complete
            }

            var currentPoint = routeSimulation.RoutePoints[routeSimulation.CurrentIndex];
            routeSimulation.CurrentIndex++;

            _logger.LogInformation("🎯 Go to point {Index}/{Total}: {Lat}, {Lon}",
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

                _logger.LogInformation("🌐 Sending position update for delivery {DeliveryId}: {Lat}, {Lon}",
                    deliveryId, latitude, longitude);

                var updateResponse = await _httpClient.PostAsync(
                    $"https://localhost:7183/api/delivery/{deliveryId}/update-location",
                    content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Delivery position {DeliveryId} successfully updated", deliveryId);
                }
                else
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("❌ Failed to update delivery position {DeliveryId}: {StatusCode}, {Error}",
                        deliveryId, updateResponse.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating delivery item {DeliveryId}", deliveryId);
            }
        }

        private async Task<int> GetSavedRouteIndexAsync(int deliveryId)
        {
            try
            {
                // Easy way: use HTTP request to API to get saved index
                var response = await _httpClient.GetAsync($"https://localhost:7183/api/delivery/{deliveryId}/route-index");
                if (response.IsSuccessStatusCode)
                {
                    var indexString = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(indexString, out int savedIndex))
                    {
                        _logger.LogInformation("📍 Found saved index {Index} for delivery {DeliveryId}", savedIndex, deliveryId);
                        return savedIndex;
                    }
                }

                _logger.LogInformation("📍 Saved index not found for delivery {DeliveryId}, starting from 0", deliveryId);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Error retrieving saved index for delivery {DeliveryId}", deliveryId);
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
                    _logger.LogInformation("💾 Saved index {Index} for delivery {DeliveryId}", currentIndex, deliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Error saving index for delivery {DeliveryId}", deliveryId);
            }
        }

        private async Task ClearSavedRouteIndexAsync(int deliveryId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://localhost:7183/api/delivery/{deliveryId}/route-index");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("🗑️ Cleared saved index for delivery {DeliveryId}", deliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Error clearing index for delivery {DeliveryId}", deliveryId);
            }
        }
    }
}
