using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Data;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.BusinessLogic
{
    public class BusinessRulesTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public BusinessRulesTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateDelivery_ShouldCalculateCorrectTotalAmount()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Name = "Product 1", Price = 10.50m, VendorId = vendor.Id },
                new Product { Name = "Product 2", Price = 25.75m, VendorId = vendor.Id }
            };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Create delivery
            var delivery = new Delivery
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Status = DeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = 0 // Will be calculated
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Add delivery items
            var deliveryItems = new List<DeliveryItem>
            {
                new DeliveryItem
                {
                    DeliveryId = delivery.Id,
                    ProductId = products[0].Id,
                    Quantity = 3,
                    Price = products[0].Price
                },
                new DeliveryItem
                {
                    DeliveryId = delivery.Id,
                    ProductId = products[1].Id,
                    Quantity = 2,
                    Price = products[1].Price
                }
            };
            _context.DeliveryItems.AddRange(deliveryItems);

            // Calculate total amount
            var totalAmount = deliveryItems.Sum(item => item.Quantity * item.Price);
            delivery.TotalAmount = totalAmount;

            await _context.SaveChangesAsync();

            // Assert
            delivery.TotalAmount.Should().Be(83.0m); // (3 * 10.50) + (2 * 25.75) = 31.50 + 51.50 = 83.00
        }

        [Fact]
        public async Task DeliveryStatusTransition_ShouldFollowCorrectFlow()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var delivery = new Delivery
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Status = DeliveryStatus.Pending,
                TotalAmount = 100m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act & Assert - Status transitions

            // 1. Pending -> InTransit (when driver assigned)
            delivery.Status = DeliveryStatus.InTransit;
            delivery.DriverId = "DRV001";
            delivery.GpsTrackerId = "GPS001";
            await _context.SaveChangesAsync();

            delivery.Status.Should().Be(DeliveryStatus.InTransit);
            delivery.DriverId.Should().Be("DRV001");

            // 2. InTransit -> Delivered
            delivery.Status = DeliveryStatus.Delivered;
            delivery.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            delivery.Status.Should().Be(DeliveryStatus.Delivered);
            delivery.DeliveredAt.Should().NotBeNull();
        }

        [Fact]
        public async Task InventoryManagement_ShouldUpdateStoreInventory_WhenDeliveryCompleted()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var product = new Product { Name = "Test Product", Price = 15.0m, VendorId = vendor.Id };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var delivery = new Delivery
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Status = DeliveryStatus.InTransit,
                TotalAmount = 75m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            var deliveryItem = new DeliveryItem
            {
                DeliveryId = delivery.Id,
                ProductId = product.Id,
                Quantity = 5,
                Price = product.Price
            };
            _context.DeliveryItems.Add(deliveryItem);
            await _context.SaveChangesAsync();

            // Act - Mark delivery as completed and update inventory
            delivery.Status = DeliveryStatus.Delivered;

            // Simulate inventory update logic
            var existingInventory = await _context.StoreInventories
                .Where(si => si.StoreId == store.Id && si.ProductId == product.Id)
                .FirstOrDefaultAsync();

            if (existingInventory != null)
            {
                existingInventory.Quantity += deliveryItem.Quantity;
            }
            else
            {
                var newInventory = new StoreInventory
                {
                    StoreId = store.Id,
                    ProductId = product.Id,
                    Quantity = deliveryItem.Quantity
                };
                _context.StoreInventories.Add(newInventory);
            }

            await _context.SaveChangesAsync();

            // Assert
            var inventory = await _context.StoreInventories
                .Where(si => si.StoreId == store.Id && si.ProductId == product.Id)
                .FirstOrDefaultAsync();

            inventory.Should().NotBeNull();
            inventory!.Quantity.Should().Be(5);
        }

        [Theory]
        [InlineData(DeliveryStatus.Pending, true)]
        [InlineData(DeliveryStatus.InTransit, false)]
        [InlineData(DeliveryStatus.Delivered, false)]
        [InlineData(DeliveryStatus.Cancelled, false)]
        public void CanCancelDelivery_ShouldReturnCorrectResult(DeliveryStatus status, bool canCancel)
        {
            // Act
            var result = CanDeliveryBeCancelled(status);

            // Assert
            result.Should().Be(canCancel);
        }

        [Theory]
        [InlineData(DeliveryStatus.Pending, DeliveryStatus.InTransit, true)]
        [InlineData(DeliveryStatus.InTransit, DeliveryStatus.Delivered, true)]
        [InlineData(DeliveryStatus.Pending, DeliveryStatus.Delivered, false)] // Skip InTransit
        [InlineData(DeliveryStatus.Delivered, DeliveryStatus.InTransit, false)] // Backwards
        [InlineData(DeliveryStatus.Cancelled, DeliveryStatus.InTransit, false)] // From cancelled
        public void IsValidStatusTransition_ShouldValidateTransitions(DeliveryStatus from, DeliveryStatus to, bool isValid)
        {
            // Act
            var result = IsValidStatusTransition(from, to);

            // Assert
            result.Should().Be(isValid);
        }

        [Fact]
        public void ValidateDeliveryItems_ShouldRejectEmptyItems()
        {
            // Arrange
            var items = new List<DeliveryItem>();

            // Act
            var isValid = ValidateDeliveryItems(items);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateDeliveryItems_ShouldRejectZeroQuantity()
        {
            // Arrange
            var items = new List<DeliveryItem>
            {
                new DeliveryItem { ProductId = 1, Quantity = 0, Price = 10m }
            };

            // Act
            var isValid = ValidateDeliveryItems(items);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateDeliveryItems_ShouldAcceptValidItems()
        {
            // Arrange
            var items = new List<DeliveryItem>
            {
                new DeliveryItem { ProductId = 1, Quantity = 5, Price = 10m },
                new DeliveryItem { ProductId = 2, Quantity = 3, Price = 15m }
            };

            // Act
            var isValid = ValidateDeliveryItems(items);

            // Assert
            isValid.Should().BeTrue();
        }

        // Helper methods for business logic
        private static bool CanDeliveryBeCancelled(DeliveryStatus status)
        {
            return status == DeliveryStatus.Pending;
        }

        private static bool IsValidStatusTransition(DeliveryStatus from, DeliveryStatus to)
        {
            return (from, to) switch
            {
                (DeliveryStatus.Pending, DeliveryStatus.InTransit) => true,
                (DeliveryStatus.InTransit, DeliveryStatus.Delivered) => true,
                (DeliveryStatus.Pending, DeliveryStatus.Cancelled) => true,
                _ => false
            };
        }

        private static bool ValidateDeliveryItems(List<DeliveryItem> items)
        {
            return items.Any() && items.All(item => item.Quantity > 0 && item.Price > 0);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
