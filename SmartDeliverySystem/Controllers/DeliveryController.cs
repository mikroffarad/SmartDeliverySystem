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
        private readonly ILogger<DeliveryController> _logger;

        public DeliveryController(IDeliveryService deliveryService, IServiceBusService serviceBusService, ISignalRService signalRService, ILogger<DeliveryController> logger)
        {
            _deliveryService = deliveryService;
            _serviceBusService = serviceBusService;
            _signalRService = signalRService;
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
            var result = await _deliveryService.UpdateLocationAsync(id, locationUpdate);
            if (!result)
                return NotFound("Delivery not found.");            // Send GPS update to Azure Service Bus for real-time processing
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
    }
}
