using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Data;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.Services
{
    public class DeliveryServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly DeliveryService _service;

        public DeliveryServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new DeliveryService(_context);
        }

        [Fact]
        public async Task GetActiveDeliveriesAsync_ShouldReturnEmptyList_WhenNoDeliveries()
        {
            // Act
            var result = await _service.GetActiveDeliveriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActiveDeliveriesAsync_ShouldReturnActiveDeliveries_WhenDeliveriesExist()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var activeDelivery = new Delivery
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Status = DeliveryStatus.InTransit,
                TotalAmount = 100m,
                CreatedAt = DateTime.UtcNow
            };

            var completedDelivery = new Delivery
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Status = DeliveryStatus.Delivered,
                TotalAmount = 200m,
                CreatedAt = DateTime.UtcNow
            };

            _context.Deliveries.AddRange(activeDelivery, completedDelivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveDeliveriesAsync();

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(d => d.Status == DeliveryStatus.InTransit);
            result.Should().NotContain(d => d.Status == DeliveryStatus.Delivered);
        }

        [Fact]
        public async Task GetDeliveryByIdAsync_ShouldReturnDelivery_WhenExists()
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
                TotalAmount = 150m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDeliveryByIdAsync(delivery.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(delivery.Id);
            result.TotalAmount.Should().Be(150m);
        }

        [Fact]
        public async Task GetDeliveryByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _service.GetDeliveryByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateDeliveryStatusAsync_ShouldUpdateStatus_WhenDeliveryExists()
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
                TotalAmount = 150m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateDeliveryStatusAsync(delivery.Id, DeliveryStatus.InTransit);

            // Assert
            result.Should().BeTrue();

            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            updatedDelivery!.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task UpdateDeliveryStatusAsync_ShouldReturnFalse_WhenDeliveryNotExists()
        {
            // Act
            var result = await _service.UpdateDeliveryStatusAsync(999, DeliveryStatus.InTransit);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
