using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.Data;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReferenceDataController : ControllerBase
    {
        private readonly DeliveryContext _context;

        public ReferenceDataController(DeliveryContext context)
        {
            _context = context;
        }

        [HttpGet("info")]
        public ActionResult GetDataInfo()
        {
            var storesCount = _context.Stores.Count();
            var productsCount = _context.Products.Count();
            var vendorsCount = _context.Vendors.Count();
            var deliveriesCount = _context.Deliveries.Count();

            return Ok(new
            {
                Stores = storesCount,
                Products = productsCount,
                Vendors = vendorsCount,
                Deliveries = deliveriesCount
            });
        }
    }
}