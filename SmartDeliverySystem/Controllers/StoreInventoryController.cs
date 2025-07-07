using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/stores/{storeId}/inventory")]
    public class StoreInventoryController : ControllerBase
    {
        private readonly DeliveryContext _context;
        private readonly ILogger<StoreInventoryController> _logger;

        public StoreInventoryController(DeliveryContext context, ILogger<StoreInventoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoreInventoryDto>>> GetStoreInventory(int storeId)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null)
                return NotFound("Store not found");

            var inventory = await _context.StoreProducts
                .Include(sp => sp.Product)
                .Where(sp => sp.StoreId == storeId && sp.Quantity > 0)
                .Select(sp => new StoreInventoryDto
                {
                    ProductId = sp.ProductId,
                    ProductName = sp.Product!.Name,
                    Category = sp.Product.Category,
                    Price = sp.Product.Price,
                    Quantity = sp.Quantity,
                    Weight = sp.Product.Weight
                })
                .ToListAsync();

            return Ok(inventory);
        }

        [HttpPost("add")]
        public async Task<ActionResult> AddProductToStore(int storeId, [FromBody] AddToInventoryDto dto)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null)
                return NotFound("Store not found");

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound("Product not found");

            var existingInventory = await _context.StoreProducts
                .FirstOrDefaultAsync(sp => sp.StoreId == storeId && sp.ProductId == dto.ProductId);

            if (existingInventory != null)
            {
                existingInventory.Quantity += dto.Quantity;
                _logger.LogInformation("Updated inventory: Store {StoreId}, Product {ProductId}, New Quantity: {Quantity}",
                    storeId, dto.ProductId, existingInventory.Quantity);
            }
            else
            {
                var newInventory = new StoreProduct
                {
                    StoreId = storeId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                _context.StoreProducts.Add(newInventory);
                _logger.LogInformation("Added new inventory: Store {StoreId}, Product {ProductId}, Quantity: {Quantity}",
                    storeId, dto.ProductId, dto.Quantity);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
