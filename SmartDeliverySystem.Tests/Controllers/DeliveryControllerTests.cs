using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Controllers;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Enums;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.SignalR;
using SmartDeliverySystem.Hubs;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class DeliveryControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IHubContext<DeliveryHub>> _hubContextMock;
        private readonly DeliveryController _controller;

        public DeliveryControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _hubContextMock = new Mock<IHubContext<DeliveryHub>>();
            _controller = new DeliveryController(_context, _hubContextMock.Object);
        }

        [Fact]
        public async Task GetActiveDeliveries_ReturnsEmptyList_WhenNoActiveDeliveries()
        {
            // Act
            var result = await _controller.GetActiveDeliveries();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActiveDeliveries_ReturnsOnlyActiveDeliveries_WhenMixedStatusExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var deliveries = new List<Delivery>
            {
                new Delivery
                {
                    VendorId = vendor.Id,
                    StoreId = store.Id,
                    Status = DeliveryStatus.Pending,
                    TotalAmount = 100.0m,
                    CreatedAt = DateTime.UtcNow
                },
                new Delivery
                {
                    VendorId = vendor.Id,
                    StoreId = store.Id,
                    Status = DeliveryStatus.InTransit,
                    TotalAmount = 200.0m,
                    CreatedAt = DateTime.UtcNow
                },
                new Delivery
                {
                    VendorId = vendor.Id,
                    StoreId = store.Id,
                    Status = DeliveryStatus.Delivered,
                    TotalAmount = 300.0m,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Deliveries.AddRange(deliveries);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetActiveDeliveries();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(d => d.Status == DeliveryStatus.Pending);
            result.Should().Contain(d => d.Status == DeliveryStatus.InTransit);
            result.Should().NotContain(d => d.Status == DeliveryStatus.Delivered);
        }

        [Fact]
        public async Task GetDelivery_ReturnsDelivery_WhenDeliveryExists()
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
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDelivery(delivery.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var returnedDelivery = actionResult!.Value as Delivery;
            returnedDelivery.Should().NotBeNull();
            returnedDelivery!.TotalAmount.Should().Be(150.0m);
            returnedDelivery.Status.Should().Be(DeliveryStatus.Pending);
        }

        [Fact]
        public async Task CreateDelivery_ReturnsCreatedDelivery_WhenValidData()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var product = new Product { Name = "Test Product", Price = 25.0m, VendorId = vendor.Id };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var deliveryData = new
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Items = new[]
                {
                    new { ProductId = product.Id, Quantity = 4 }
                }
            };

            // Act
            var result = await _controller.CreateDelivery(deliveryData);

            // Assert
            var actionResult = result.Result as CreatedAtActionResult;
            actionResult.Should().NotBeNull();

            var createdDelivery = actionResult!.Value as Delivery;
            createdDelivery.Should().NotBeNull();
            createdDelivery!.TotalAmount.Should().Be(100.0m); // 25 * 4 = 100
            createdDelivery.Status.Should().Be(DeliveryStatus.Pending);
        }

        [Fact]
        public async Task UpdateDeliveryStatus_UpdatesStatus_WhenValidDelivery()
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
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateDeliveryStatus(delivery.Id, DeliveryStatus.InTransit);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Verify status was updated in database
            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            updatedDelivery!.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task CancelDelivery_UpdatesStatusToCancelled_WhenValidDelivery()
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
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.CancelDelivery(delivery.Id);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Verify status was updated
            var cancelledDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            cancelledDelivery!.Status.Should().Be(DeliveryStatus.Cancelled);
        }

        [Fact]
        public async Task AssignDriver_UpdatesDriverInfo_WhenValidData()
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
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            var driverData = new
            {
                DriverId = "DRV001",
                GpsTrackerId = "GPS001"
            };

            // Act
            var result = await _controller.AssignDriver(delivery.Id, driverData);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Verify driver was assigned
            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            updatedDelivery!.DriverId.Should().Be("DRV001");
            updatedDelivery.GpsTrackerId.Should().Be("GPS001");
            updatedDelivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
