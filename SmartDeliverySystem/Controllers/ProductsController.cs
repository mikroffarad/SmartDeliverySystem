using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly DeliveryContext _context;
        private readonly IMapper _mapper;

        public ProductsController(DeliveryContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Vendor)
                .ToListAsync();

            var result = _mapper.Map<List<ProductDto>>(products);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var result = _mapper.Map<ProductDto>(product);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(ProductDto dto)
        {
            var vendor = await _context.Vendors.FindAsync(dto.VendorId);
            if (vendor == null)
                return BadRequest($"Vendor with ID {dto.VendorId} not found.");

            var product = _mapper.Map<Product>(dto);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var result = _mapper.Map<ProductDto>(product);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _mapper.Map(dto, product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Check if product is in any deliveries
            var hasDeliveries = await _context.DeliveryProducts.AnyAsync(dp => dp.ProductId == id);
            if (hasDeliveries)
            {
                return BadRequest("Cannot delete product because it is associated with deliveries. Please delete all related deliveries first.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
