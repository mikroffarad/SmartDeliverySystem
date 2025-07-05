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
    public class VendorsController : ControllerBase
    {
        private readonly DeliveryContext _context;
        private readonly IMapper _mapper;

        public VendorsController(DeliveryContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VendorWithProductsDto>>> GetVendors()
        {
            var vendors = await _context.Vendors
                .Include(v => v.Products)
                .ToListAsync();

            var result = _mapper.Map<List<VendorWithProductsDto>>(vendors);
            return Ok(result);
        }
        [HttpGet("map")]
        public async Task<ActionResult> GetVendorsForMap()
        {
            var vendors = await _context.Vendors
                .Where(v => v.Latitude != 0 && v.Longitude != 0)
                .Select(v => new
                {
                    id = v.Id,
                    name = v.Name,
                    latitude = v.Latitude,
                    longitude = v.Longitude
                })
                .ToListAsync();

            return Ok(vendors);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<VendorWithProductsDto>> GetVendor(int id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.Products)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vendor == null) return NotFound();

            var result = _mapper.Map<VendorWithProductsDto>(vendor);
            return Ok(result);
        }
        [HttpPost]
        public async Task<ActionResult<Vendor>> CreateVendor([FromBody] VendorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Vendors.AnyAsync(v => v.Name == dto.Name))
                return BadRequest($"Vendor with name '{dto.Name}' already exists.");

            var vendor = _mapper.Map<Vendor>(dto);
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVendor), new { id = vendor.Id }, vendor);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] VendorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            if (await _context.Vendors.AnyAsync(v => v.Id != id && v.Name == dto.Name))
                return BadRequest($"Vendor with name '{dto.Name}' already exists.");

            _mapper.Map(dto, vendor);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();
            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
