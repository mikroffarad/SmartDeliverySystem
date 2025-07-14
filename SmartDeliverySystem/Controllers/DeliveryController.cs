using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Services;
using DeliveryDto = SmartDeliverySystem.DTOs.DeliveryResponseDto;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;
        private readonly IServiceBusService _serviceBusService;
        private readonly ISignalRService _signalRService;
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<DeliveryController> _logger;

        public DeliveryController(
            IDeliveryService deliveryService,
            IServiceBusService serviceBusService,
            ISignalRService signalRService,
            ITableStorageService tableStorageService,
            ILogger<DeliveryController> logger)
        {
            _deliveryService = deliveryService;
            _serviceBusService = serviceBusService;
            _signalRService = signalRService;
            _tableStorageService = tableStorageService;
            _logger = logger;
        }
        [HttpPost("request")]
        public async Task<ActionResult<DeliveryResponseDto>> RequestDelivery([FromBody] DeliveryRequestDto request)
        {
            _logger.LogInformation("Delivery request received from vendor {VendorId}", request.VendorId);
            var response = await _deliveryService.CreateDeliveryAsync(request);

            // Send message to Azure Service Bus
            try
            {
                _logger.LogInformation("🔄 Attempting to send message to Service Bus...");
                _logger.LogInformation("ServiceBusService is null: {IsNull}", _serviceBusService == null);

                if (_serviceBusService != null)
                {
                    await _serviceBusService.SendDeliveryRequestAsync(new
                    {
                        DeliveryId = response.DeliveryId,
                        VendorId = request.VendorId,
                        StoreId = response.StoreId,
                        TotalAmount = response.TotalAmount,
                        CreatedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("✅ Delivery request sent to Azure Service Bus for delivery {DeliveryId}", response.DeliveryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send delivery request to Service Bus: {Message}. StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                // Continue execution even if Service Bus fails
            }

            return Ok(response);
        }
        [HttpPost("request-manual")]
        public async Task<ActionResult<DeliveryResponseDto>> RequestDeliveryManual([FromBody] DeliveryRequestManualDto request)
        {
            _logger.LogInformation("Manual delivery request received from vendor {VendorId} to store {StoreId}",
                request.VendorId, request.StoreId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for manual delivery request");
                return BadRequest(ModelState);
            }

            if (request.Products == null || !request.Products.Any())
            {
                return BadRequest("At least one product is required");
            }

            try
            {
                var response = await _deliveryService.CreateDeliveryManualAsync(request);                // Send message to Azure Service Bus
                try
                {
                    if (_serviceBusService != null)
                    {
                        await _serviceBusService.SendDeliveryRequestAsync(new
                        {
                            DeliveryId = response.DeliveryId,
                            VendorId = request.VendorId,
                            StoreId = response.StoreId,
                            TotalAmount = response.TotalAmount,
                            CreatedAt = DateTime.UtcNow
                        });
                        _logger.LogInformation("✅ Manual delivery request sent to Azure Service Bus for delivery {DeliveryId}", response.DeliveryId);
                    }
                    else
                    {
                        _logger.LogWarning("Service Bus service is not available");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to send manual delivery request to Service Bus");
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating manual delivery request");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Delivery>> GetDelivery(int id)
        {
            var delivery = await _deliveryService.GetDeliveryAsync(id);

            if (delivery == null)
                return NotFound();

            return Ok(delivery);
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<Delivery>>> GetActiveDeliveries()
        {
            var deliveries = await _deliveryService.GetActiveDeliveriesAsync();
            return Ok(deliveries);
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateDeliveryStatus(int id, [FromBody] DeliveryStatus status)
        {
            var updated = await _deliveryService.UpdateDeliveryStatusAsync(id, status);

            if (!updated)
                return NotFound();

            return Ok();
        }

        [HttpPost("{id}/assign-driver")]
        public async Task<ActionResult> AssignDriver(int id, [FromBody] AssignDriverDto dto)
        {
            var updated = await _deliveryService.AssignDriverAsync(id, dto);
            if (!updated)
                return NotFound();
            _logger.LogInformation("Driver {DriverId} assigned to delivery {DeliveryId}", dto.DriverId, id);
            return Ok();
        }
        [HttpPost("{id}/pay")]
        public async Task<ActionResult> ProcessPayment(int id, [FromBody] PaymentDto payment)
        {
            try
            {
                var processed = await _deliveryService.ProcessPaymentAsync(id, payment);
                if (!processed)
                    return NotFound("Delivery not found or already paid");

                _logger.LogInformation("✅ Payment processed for delivery {DeliveryId}: ${Amount} via {PaymentMethod}",
                    id, payment.Amount, payment.PaymentMethod);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing payment for delivery {DeliveryId}", id);
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("{id}/update-location")]
        public async Task<ActionResult> UpdateLocation(int id, [FromBody] LocationUpdateDto locationUpdate)
        {
            _logger.LogInformation("🌍 UpdateLocation endpoint викликано для доставки {DeliveryId} з координатами {Lat}, {Lon}",
                id, locationUpdate.Latitude, locationUpdate.Longitude);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Невалідні дані для оновлення локації доставки {DeliveryId}", id);
                return BadRequest(ModelState);
            }

            var result = await _deliveryService.UpdateLocationAsync(id, locationUpdate);
            if (!result)
            {
                _logger.LogWarning("❌ Не вдалося оновити локацію для доставки {DeliveryId}", id);
                return NotFound("Delivery not found.");
            }

            _logger.LogInformation("✅ Локація успішно оновлена для доставки {DeliveryId}", id);

            // Send GPS update to Azure Service Bus for real-time processing
            try
            {
                await _serviceBusService.SendLocationUpdateAsync(new
                {
                    DeliveryId = id,
                    Latitude = locationUpdate.Latitude,
                    Longitude = locationUpdate.Longitude,
                    Speed = locationUpdate.Speed,
                    Notes = locationUpdate.Notes,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogInformation("📍 GPS update sent to Azure Service Bus for delivery {DeliveryId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send GPS update to Service Bus");
            }

            // Send real-time update via SignalR
            try
            {
                await _signalRService.SendLocationUpdateAsync(id, locationUpdate.Latitude, locationUpdate.Longitude, locationUpdate.Notes);
                _logger.LogInformation("📡 Real-time GPS update sent via SignalR for delivery {DeliveryId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR update");
            }

            _logger.LogInformation("Location updated for delivery {DeliveryId}", id);
            return Ok();
        }

        [HttpGet("{id}/tracking")]
        public async Task<ActionResult<DeliveryTrackingDto>> GetDeliveryTracking(int id)
        {
            var tracking = await _deliveryService.GetDeliveryTrackingAsync(id);
            if (tracking == null)
                return NotFound("Delivery not found.");
            return Ok(tracking);
        }

        [HttpGet("tracking/active")]
        public async Task<ActionResult<List<DeliveryTrackingDto>>> GetAllActiveTracking()
        {
            var trackingList = await _deliveryService.GetAllActiveTrackingAsync();
            return Ok(trackingList);
        }

        [HttpPost("find-best-store")]
        public async Task<IActionResult> FindBestStore([FromBody] FindBestStoreRequestDto request)
        {
            try
            {
                var result = await _deliveryService.FindBestStoreForDeliveryAsync(request.VendorId, request.Products);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding best store for vendor {VendorId}", request.VendorId);
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("{deliveryId}/location-history")]
        public async Task<ActionResult<List<LocationHistoryDto>>> GetLocationHistory(int deliveryId)
        {
            try
            {
                _logger.LogInformation("Requesting GPS history for delivery {DeliveryId}", deliveryId);

                var history = await _tableStorageService.GetLocationHistoryAsync(deliveryId);

                _logger.LogInformation("Found {Count} GPS records for delivery {DeliveryId}", history.Count, deliveryId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location history for delivery {DeliveryId}", deliveryId);
                return BadRequest($"Error getting location history: {ex.Message}");
            }
        }

        [HttpPost("test-movement/{deliveryId}")]
        public async Task<ActionResult> TestMovement(int deliveryId)
        {
            _logger.LogInformation("🧪 Test movement endpoint викликано для доставки {DeliveryId}", deliveryId);

            var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);
            if (delivery == null)
            {
                return NotFound("Delivery not found");
            }

            // Симулюємо рух на 0.001 градусів на північ
            var currentLat = delivery.CurrentLatitude ?? delivery.FromLatitude ?? 0;
            var currentLon = delivery.CurrentLongitude ?? delivery.FromLongitude ?? 0;

            var newLat = currentLat + 0.001;
            var newLon = currentLon + 0.001;

            var locationUpdate = new LocationUpdateDto
            {
                Latitude = newLat,
                Longitude = newLon,
                Speed = 30,
                Notes = "🧪 Тестове переміщення"
            };

            var result = await _deliveryService.UpdateLocationAsync(deliveryId, locationUpdate);
            if (!result)
            {
                return BadRequest("Failed to update location");
            }

            // Відправляємо SignalR оновлення
            try
            {
                await _signalRService.SendLocationUpdateAsync(deliveryId, newLat, newLon, "🧪 Тестове переміщення");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при відправці SignalR оновлення");
            }

            return Ok(new
            {
                message = "Test movement completed",
                oldPosition = new { lat = currentLat, lon = currentLon },
                newPosition = new { lat = newLat, lon = newLon }
            });
        }
        [HttpGet("{deliveryId}/route-index")]
        public ActionResult<string> GetRouteIndex(int deliveryId)
        {
            try
            {
                // Простий підхід: зберігаємо індекс у тимчасовому кеші або базі даних
                // Для простоти використаємо статичний Dictionary
                if (RouteIndexCache.TryGetValue(deliveryId, out int index))
                {
                    return Ok(index.ToString());
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні індексу маршруту для доставки {DeliveryId}", deliveryId);
                return StatusCode(500);
            }
        }

        [HttpPost("{deliveryId}/route-index")]
        public ActionResult SaveRouteIndex(int deliveryId, [FromBody] RouteIndexDto dto)
        {
            try
            {
                RouteIndexCache[deliveryId] = dto.Index;
                _logger.LogInformation("💾 Збережено індекс маршруту {Index} для доставки {DeliveryId}", dto.Index, deliveryId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при збереженні індексу маршруту для доставки {DeliveryId}", deliveryId);
                return StatusCode(500);
            }
        }

        [HttpDelete("{deliveryId}/route-index")]
        public ActionResult ClearRouteIndex(int deliveryId)
        {
            try
            {
                RouteIndexCache.Remove(deliveryId);
                _logger.LogInformation("🗑️ Очищено індекс маршруту для доставки {DeliveryId}", deliveryId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при очищенні індексу маршруту для доставки {DeliveryId}", deliveryId);
                return StatusCode(500);
            }
        }

        // Статичний кеш для збереження індексів маршрутів
        private static readonly Dictionary<int, int> RouteIndexCache = new();

        public class RouteIndexDto
        {
            public int Index { get; set; }
        }
    }
}
