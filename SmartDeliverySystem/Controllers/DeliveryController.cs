using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Services;
using AutoMapper;

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
        private readonly IMapper _mapper;
        private readonly ILogger<DeliveryController> _logger;

        // Static cache to store route indices
        private static readonly Dictionary<int, int> RouteIndexCache = [];

        public DeliveryController(
            IDeliveryService deliveryService,
            IServiceBusService serviceBusService,
            ISignalRService signalRService,
            ITableStorageService tableStorageService,
            IMapper mapper,
            ILogger<DeliveryController> logger)
        {
            _deliveryService = deliveryService;
            _serviceBusService = serviceBusService;
            _signalRService = signalRService;
            _tableStorageService = tableStorageService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("request")]
        public async Task<ActionResult<DeliveryResponseDto>> RequestDelivery([FromBody] DeliveryRequestDto request)
        {
            _logger.LogInformation("Delivery request received from vendor {VendorId} to store {StoreId}",
                request.VendorId, request.StoreId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for delivery request");
                return BadRequest(ModelState);
            }

            if (request.Products == null || !request.Products.Any())
            {
                return BadRequest("At least one product is required");
            }

            try
            {
                var response = await _deliveryService.CreateDeliveryAsync(request);

                // Send message to Azure Service Bus
                try
                {
                    if (_serviceBusService != null)
                    {
                        await _serviceBusService.SendDeliveryRequestAsync(_mapper.Map<DeliveryRequestDto>(request));
                        _logger.LogInformation("✅ Delivery request sent to Azure Service Bus for delivery {DeliveryId}", response.DeliveryId);
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
                _logger.LogError(ex, "❌ Error creating delivery request");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{deliveryId}")]
        public async Task<ActionResult<DeliveryDto>> GetDelivery(int deliveryId)
        {
            try
            {
                var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);

                if (delivery == null)
                {
                    return NotFound($"Delivery with ID {deliveryId} not found");
                }

                var result = _mapper.Map<DeliveryDto>(delivery);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery {DeliveryId}", deliveryId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<DeliveryDto>>> GetAllDeliveries()
        {
            try
            {
                var deliveries = await _deliveryService.GetAllDeliveriesAsync();
                var result = _mapper.Map<List<DeliveryDto>>(deliveries);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all deliveries");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{deliveryId}/status")]
        public async Task<ActionResult> UpdateDeliveryStatus(int deliveryId, [FromBody] DeliveryStatus status)
        {
            var updated = await _deliveryService.UpdateDeliveryStatusAsync(deliveryId, status);

            if (!updated)
                return NotFound();

            return Ok();
        }

        [HttpPost("{deliveryId}/assign-driver")]
        public async Task<ActionResult> AssignDriver(int deliveryId, [FromBody] AssignDriverDto dto)
        {
            var updated = await _deliveryService.AssignDriverAsync(deliveryId, dto);
            if (!updated)
                return NotFound();
            _logger.LogInformation("Driver {DriverId} assigned to delivery {deliveryId}", dto.DriverId, deliveryId);
            return Ok();
        }

        [HttpPost("{deliveryId}/pay")]
        public async Task<ActionResult> ProcessPayment(int deliveryId, [FromBody] PaymentDto payment)
        {
            try
            {
                var processed = await _deliveryService.ProcessPaymentAsync(deliveryId, payment);
                if (!processed)
                    return NotFound("Delivery not found or already paid");

                _logger.LogInformation("✅ Payment processed for delivery {DeliveryId}: ${Amount} via {PaymentMethod}",
                    deliveryId, payment.Amount, payment.PaymentMethod);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing payment for delivery {DeliveryId}", deliveryId);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{deliveryId}/update-location")]
        public async Task<ActionResult> UpdateLocation(int deliveryId, [FromBody] LocationUpdateDto locationUpdate)
        {
            _logger.LogInformation("🌍 UpdateLocation endpoint called for delivery {DeliveryId} with coordinates {Lat}, {Lon}",
                deliveryId, locationUpdate.Latitude, locationUpdate.Longitude);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Invalid data for updating location for delivery {DeliveryId}", deliveryId);
                return BadRequest(ModelState);
            }

            var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);
            if (delivery == null)
            {
                _logger.LogWarning("❌ Delivery {DeliveryId} is not found", deliveryId);
                return NotFound("Delivery not found.");
            }

            if (delivery.Status == DeliveryStatus.Delivered || delivery.Status == DeliveryStatus.Cancelled)
            {
                _logger.LogWarning("❌ Delivery {DeliveryId} is already completed with status {Status}, ignoring GPS update",
                    deliveryId, delivery.Status);
                return BadRequest($"Delivery is already completed with status {delivery.Status}");
            }

            var result = await _deliveryService.UpdateLocationAsync(deliveryId, locationUpdate);
            if (!result)
            {
                _logger.LogWarning("❌ Failed to update location for delivery {DeliveryId}", deliveryId);
                return NotFound("Failed to update location.");
            }

            _logger.LogInformation("✅ Location successfully updated for delivery {DeliveryId}", deliveryId);

            // Send GPS update to Azure Service Bus for real-time processing
            try
            {
                var serviceBusMessage = _mapper.Map<LocationUpdateServiceBusDto>(locationUpdate);
                serviceBusMessage.DeliveryId = deliveryId;

                await _serviceBusService.SendLocationUpdateAsync(serviceBusMessage);
                _logger.LogInformation("📍 GPS update sent to Azure Service Bus for delivery {DeliveryId}", deliveryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send GPS update to Service Bus");
            }

            // Send real-time update via SignalR
            try
            {
                await _signalRService.SendLocationUpdateAsync(deliveryId, locationUpdate.Latitude, locationUpdate.Longitude, locationUpdate.Notes);
                _logger.LogInformation("📡 Real-time GPS update sent via SignalR for delivery {DeliveryId}", deliveryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR update");
            }

            return Ok();
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

        [HttpGet("{deliveryId}/products")]
        public async Task<ActionResult<List<object>>> GetDeliveryProducts(int deliveryId)
        {
            try
            {
                var products = await _deliveryService.GetDeliveryProductsAsync(deliveryId);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for delivery {DeliveryId}", deliveryId);
                return BadRequest($"Error getting delivery products: {ex.Message}");
            }
        }

        [HttpGet("{deliveryId}/route-index")]
        public ActionResult<string> GetRouteIndex(int deliveryId)
        {
            try
            {
                // Simplified approach: store the index in a temporary cache
                // For simplicity, we use a static Dictionary
                if (RouteIndexCache.TryGetValue(deliveryId, out int index))
                {
                    return Ok(index.ToString());
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting route index for delivery {DeliveryId}", deliveryId);
                return StatusCode(500);
            }
        }

        [HttpPost("{deliveryId}/route-index")]
        public ActionResult SaveRouteIndex(int deliveryId, [FromBody] RouteIndexDto dto)
        {
            try
            {
                RouteIndexCache[deliveryId] = dto.Index;
                _logger.LogInformation("💾 Saved route index {Index} for delivery {DeliveryId}", dto.Index, deliveryId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving route index for delivery {DeliveryId}", deliveryId);
                return StatusCode(500);
            }
        }

        [HttpDelete("{deliveryId}/route-index")]
        public ActionResult ClearRouteIndex(int deliveryId)
        {
            try
            {
                RouteIndexCache.Remove(deliveryId);
                _logger.LogInformation("🗑️ Cleared the route index for delivery {DeliveryId}", deliveryId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing route index for delivery {DeliveryId}", deliveryId);
                return StatusCode(500);
            }
        }

        [HttpDelete("{deliveryId}")]
        public async Task<IActionResult> DeleteDelivery(int deliveryId)
        {
            try
            {
                var delivery = await _deliveryService.GetDeliveryAsync(deliveryId);
                if (delivery == null)
                {
                    return NotFound($"Delivery with ID {deliveryId} not found");
                }

                // Check if delivery is in progress
                if (delivery.Status == DeliveryStatus.InTransit)
                {
                    return BadRequest("Cannot delete delivery that is currently in transit");
                }

                var deleted = await _deliveryService.DeleteDeliveryAsync(deliveryId);
                if (!deleted)
                {
                    return BadRequest("Failed to delete delivery");
                }

                _logger.LogInformation("🗑️ Delivery {DeliveryId} deleted successfully", deliveryId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting delivery {DeliveryId}", deliveryId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
