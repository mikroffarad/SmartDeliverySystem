using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestDataController : ControllerBase
    {
        private readonly DeliveryContext _context;
        private readonly ILogger<TestDataController> _logger;

        public TestDataController(DeliveryContext context, ILogger<TestDataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("seed")]
        public async Task<ActionResult> SeedTestData()
        {
            if (_context.Stores.Any() || _context.Products.Any() || _context.Vendors.Any())
            {
                return BadRequest("Test data already exists");
            }

            _logger.LogInformation("Creating simplified test data: 1 vendor, 5 products, 10 stores");

            // 1 головний вендор
            var vendor = new Vendor
            {
                Name = "Київський Продуктовий Центр",
                Latitude = 50.4501,
                Longitude = 30.5234
            };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            // 10 магазинів у різних районах Києва
            var stores = new List<Store>
            {
                new Store { Name = "Магазин Центр", Latitude = 50.4501, Longitude = 30.5234},
                new Store { Name = "Магазин Оболонь", Latitude = 50.5168, Longitude = 30.4982},
                new Store { Name = "Магазин Позняки", Latitude = 50.3975, Longitude = 30.6290},
                new Store { Name = "Магазин Лівобережна", Latitude = 50.4502, Longitude = 30.6090},
                new Store { Name = "Магазин Теремки", Latitude = 50.3740, Longitude = 30.4760},
                new Store { Name = "Магазин Виноградар", Latitude = 50.4880, Longitude = 30.3900},
                new Store { Name = "Магазин Троєщина", Latitude = 50.5160, Longitude = 30.6010},
                new Store { Name = "Магазин Солом'янка", Latitude = 50.4310, Longitude = 30.4710},
                new Store { Name = "Магазин Святошин", Latitude = 50.4570, Longitude = 30.3560},
                new Store { Name = "Магазин Дарниця", Latitude = 50.4310, Longitude = 30.6510}
            };
            _context.Stores.AddRange(stores);
            await _context.SaveChangesAsync();

            // 5 продуктів від одного вендора
            var products = new List<Product>
            {
                new Product { Name = "Хліб білий", Weight = 0.4m, Category = "Хлібобулочні", Price = 25.50m, VendorId = vendor.Id },
                new Product { Name = "Молоко 2.5%", Weight = 1.0m, Category = "Молочні", Price = 32.00m, VendorId = vendor.Id },
                new Product { Name = "Яблука червоні", Weight = 1.0m, Category = "Фрукти", Price = 45.00m, VendorId = vendor.Id },
                new Product { Name = "Курячі яйця", Weight = 0.6m, Category = "Яйця", Price = 55.00m, VendorId = vendor.Id },
                new Product { Name = "Гречка", Weight = 1.0m, Category = "Крупи", Price = 60.00m, VendorId = vendor.Id }
            };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // StoreProducts: кожен магазин має всі продукти, але у різній кількості
            var storeProducts = new List<StoreProduct>();
            var rand = new Random();
            foreach (var store in stores)
            {
                foreach (var product in products)
                {
                    storeProducts.Add(new StoreProduct
                    {
                        StoreId = store.Id,
                        ProductId = product.Id,
                        Quantity = rand.Next(50, 200)
                    });
                }
            }
            _context.StoreProducts.AddRange(storeProducts);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Test data created for geo-check",
                Stores = stores.Count,
                Products = products.Count,
                Vendors = 1,
                StoreProducts = storeProducts.Count
            });
        }

        [HttpDelete("clear")]
        public async Task<ActionResult> ClearTestData()
        {
            _logger.LogInformation("Clearing all test data");

            // Clear in correct order due to foreign key constraints
            _context.DeliveryLocationHistory.RemoveRange(_context.DeliveryLocationHistory);
            _context.DeliveryProducts.RemoveRange(_context.DeliveryProducts);
            _context.StoreProducts.RemoveRange(_context.StoreProducts);
            _context.Deliveries.RemoveRange(_context.Deliveries);
            _context.Products.RemoveRange(_context.Products);
            _context.Stores.RemoveRange(_context.Stores);
            _context.Vendors.RemoveRange(_context.Vendors);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "All test data cleared successfully" });
        }
    }
}
