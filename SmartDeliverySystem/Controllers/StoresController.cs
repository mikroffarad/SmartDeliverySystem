using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly DeliveryContext _context;
        private readonly IMapper _mapper;

        public StoresController(DeliveryContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Store>>> GetStores()
        {
            return Ok(await _context.Stores.ToListAsync());
        }
        [HttpGet("map")]
        public async Task<ActionResult> GetStoresForMap()
        {
            var stores = await _context.Stores
                .Where(s => s.Latitude != 0 && s.Longitude != 0)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    latitude = s.Latitude,
                    longitude = s.Longitude
                })
                .ToListAsync();

            return Ok(stores);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Store>> GetStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();
            return Ok(store);
        }

        [HttpPost]
        public async Task<ActionResult<Store>> CreateStore(StoreDto dto)
        {
            if (await _context.Stores.AnyAsync(s => s.Name == dto.Name))
                return BadRequest($"Store with name '{dto.Name}' already exists.");
            var store = _mapper.Map<Store>(dto);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetStore), new { id = store.Id }, store);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStore(int id, StoreDto dto)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();
            if (await _context.Stores.AnyAsync(s => s.Id != id && s.Name == dto.Name))
                return BadRequest($"Store with name '{dto.Name}' already exists.");
            _mapper.Map(dto, store);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStore(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();

            // Check if there are any deliveries for this store
            var hasDeliveries = await _context.Deliveries.AnyAsync(d => d.StoreId == id);
            if (hasDeliveries)
            {
                return BadRequest("Cannot delete store because it has associated deliveries. Please delete all deliveries first.");
            }

            // Check if there are any store products in inventory
            var storeProductsCount = await _context.StoreProducts
                .Where(sp => sp.StoreId == id)
                .CountAsync();

            if (storeProductsCount > 0)
            {
                return BadRequest($"Cannot delete store because it has {storeProductsCount} products in inventory. Please remove all products from store inventory first or delete all related deliveries.");
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}/inventory")]
        public async Task<IActionResult> ClearStoreInventory(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();

            var storeProducts = await _context.StoreProducts
                .Where(sp => sp.StoreId == id)
                .ToListAsync();

            if (storeProducts.Any())
            {
                _context.StoreProducts.RemoveRange(storeProducts);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = $"Cleared inventory for store {store.Name}", productsRemoved = storeProducts.Count });
        }

        [HttpGet("{id}/inventory")]
        public async Task<ActionResult<IEnumerable<object>>> GetStoreInventory(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();

            var inventory = await _context.StoreProducts
                .Where(sp => sp.StoreId == id)
                .Include(sp => sp.Product)
                .Select(sp => new
                {
                    productId = sp.ProductId,
                    productName = sp.Product.Name,
                    category = sp.Product.Category,
                    quantity = sp.Quantity,
                    price = sp.Product.Price
                })
                .ToListAsync();

            return Ok(inventory);
        }
    }
}
