using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Services;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;
        private readonly ILogger<DeliveryController> _logger;

        public DeliveryController(IDeliveryService deliveryService, ILogger<DeliveryController> logger)
        {
            _deliveryService = deliveryService;
            _logger = logger;
        }

        [HttpPost("request")]
        public async Task<ActionResult<DeliveryResponseDto>> RequestDelivery([FromBody] DeliveryRequestDto request)
        {
            _logger.LogInformation("Delivery request received from vendor {VendorId}", request.VendorId);
            var response = await _deliveryService.CreateDeliveryAsync(request);
            return Ok(response);
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
            // TODO: Implement driver assignment logic
            _logger.LogInformation("Driver {DriverId} assigned to delivery {DeliveryId}", dto.DriverId, id);

            await _deliveryService.UpdateDeliveryStatusAsync(id, DeliveryStatus.Assigned);

            return Ok();
        }

        [HttpPost("{id}/pay")]
        public async Task<ActionResult> PayForDelivery(int id, [FromBody] PaymentDto payment)
        {
            var result = await _deliveryService.ProcessPaymentAsync(id, payment);
            if (!result)
                return NotFound("Payment failed or delivery not found.");
            _logger.LogInformation("Payment for delivery {DeliveryId} processed", id);
            return Ok();
        }
    }
}
