using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Models;
using System.Numerics;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestDataController : ControllerBase
    {
        private readonly DeliveryContext _context;

        public TestDataController(DeliveryContext context)
        {
            _context = context;
        }

        [HttpPost("seed")]
        public async Task<ActionResult> SeedTestData()
        {
            // Перевіряємо, чи вже є дані
            if (_context.Stores.Any() || _context.Products.Any() || _context.Vendors.Any())
            {
                return BadRequest("Test data already exists");
            }

            // Додаємо постачальників
            var vendors = new List<Vendor>
            {
                new Vendor { Name = "ТОВ 'Київхліб'", ContactEmail = "orders@kyivbread.com", Address = "вул. Промислова, 5, Київ", Latitude = 50.4215, Longitude = 30.5384 },
                new Vendor { Name = "Молочна ферма 'Буренка'", ContactEmail = "delivery@burenka.ua", Address = "с. Борщагівка, Київська обл.", Latitude = 50.4089, Longitude = 30.3526 }
            };
            _context.Vendors.AddRange(vendors);
            await _context.SaveChangesAsync();

            // Додаємо магазини
            var stores = new List<Store>
            {
                new Store { Name = "Магазин Центр", Address = "вул. Хрещатик, 1, Київ", Latitude = 50.4501, Longitude = 30.5234, IsActive = true },
                new Store { Name = "Магазин Поділ", Address = "вул. Сагайдачного, 25, Київ", Latitude = 50.4676, Longitude = 30.5176, IsActive = true },
                new Store { Name = "Магазин Оболонь", Address = "просп. Оболонський, 15, Київ", Latitude = 50.5168, Longitude = 30.4982, IsActive = true }
            };
            _context.Stores.AddRange(stores);
            await _context.SaveChangesAsync();

            // Додаємо товари з VendorId
            var products = new List<Product>
            {
                new Product { Name = "Хліб білий", Weight = 0.4m, Category = "Хлібобулочні", Price = 25.50m, VendorId = vendors[0].Id },
                new Product { Name = "Молоко 2.5%", Weight = 1.0m, Category = "Молочні", Price = 32.00m, VendorId = vendors[1].Id },
                new Product { Name = "Яблука червоні", Weight = 1.0m, Category = "Фрукти", Price = 45.00m, VendorId = vendors[0].Id },
                new Product { Name = "Курячі яйця", Weight = 0.6m, Category = "Молочні", Price = 55.00m, VendorId = vendors[1].Id },
                new Product { Name = "Макарони", Weight = 0.5m, Category = "Крупи", Price = 28.50m, VendorId = vendors[0].Id }
            };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Test data created successfully",
                Stores = stores.Count,
                Products = products.Count,
                Vendors = vendors.Count
            });
        }

        [HttpGet("info")]
        public async Task<ActionResult> GetDataInfo()
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

        [HttpGet("stores")]
        public async Task<ActionResult> GetStores()
        {
            var stores = _context.Stores.ToList();
            return Ok(stores);
        }

        [HttpGet("products")]
        public async Task<ActionResult> GetProducts()
        {
            var products = _context.Products.ToList();
            return Ok(products);
        }

        [HttpGet("vendors")]
        public async Task<ActionResult> GetVendors()
        {
            var vendors = _context.Vendors.ToList();
            return Ok(vendors);
        }
    }
}