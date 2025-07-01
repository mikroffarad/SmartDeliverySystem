using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
