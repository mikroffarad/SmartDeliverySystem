using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Api.Controllers;
using SmartDeliverySystem.Core.Entities;
using SmartDeliverySystem.Core.Enums;
using SmartDeliverySystem.Infrastructure.Data;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.SignalR;
using SmartDeliverySystem.Api.Hubs;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class GpsControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IHubContext<DeliveryHub>> _hubContextMock;
        private readonly Mock<IHubCallerClients> _clientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly GpsController _controller;

        public GpsControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _hubContextMock = new Mock<IHubContext<DeliveryHub>>();
            _clientsMock = new Mock<IHubCallerClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
            _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);

            _controller = new GpsController(_context, _hubContextMock.Object);
        }

        [Fact]
        public async Task UpdateLocation_UpdatesLocationAndNotifiesClients_WhenValidDelivery()
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
                Status = DeliveryStatus.InTransit,
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow,
                DriverId = "DRV001",
                GpsTrackerId = "GPS001"
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            var locationData = new
            {
                DeliveryId = delivery.Id,
                Latitude = 50.5,
                Longitude = 30.5,
                Notes = "Test location update"
            };

            // Act
            var result = await _controller.UpdateLocation(locationData);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Verify location was updated in database
            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            updatedDelivery?.CurrentLatitude.Should().Be(50.5);
            updatedDelivery?.CurrentLongitude.Should().Be(30.5);
            updatedDelivery?.LastLocationUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            // Verify GPS tracking entry was created
            var gpsEntry = await _context.GpsTrackings
                .Where(g => g.DeliveryId == delivery.Id)
                .FirstOrDefaultAsync();

            gpsEntry.Should().NotBeNull();
            gpsEntry?.Latitude.Should().Be(50.5);
            gpsEntry?.Longitude.Should().Be(30.5);
            gpsEntry?.Notes.Should().Be("Test location update");

            // Verify SignalR notification was sent
            _clientProxyMock.Verify(
                c => c.SendCoreAsync(
                    "LocationUpdated",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task UpdateLocation_ReturnsNotFound_WhenDeliveryDoesNotExist()
        {
            // Arrange
            var locationData = new
            {
                DeliveryId = 999,
                Latitude = 50.5,
                Longitude = 30.5,
                Notes = "Test location update"
            };

            // Act
            var result = await _controller.UpdateLocation(locationData);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateLocation_ReturnsBadRequest_WhenDeliveryNotInTransit()
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
                Status = DeliveryStatus.Pending, // Not InTransit
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            var locationData = new
            {
                DeliveryId = delivery.Id,
                Latitude = 50.5,
                Longitude = 30.5,
                Notes = "Test location update"
            };

            // Act
            var result = await _controller.UpdateLocation(locationData);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateLocation_ReturnsBadRequest_WhenInvalidCoordinates()
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
                Status = DeliveryStatus.InTransit,
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            var locationData = new
            {
                DeliveryId = delivery.Id,
                Latitude = 200.0, // Invalid latitude
                Longitude = 30.5,
                Notes = "Test location update"
            };

            // Act
            var result = await _controller.UpdateLocation(locationData);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetLocationHistory_ReturnsHistory_WhenDeliveryExists()
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
                Status = DeliveryStatus.InTransit,
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            var gpsEntries = new List<GpsTracking>
            {
                new GpsTracking
                {
                    DeliveryId = delivery.Id,
                    Latitude = 50.1,
                    Longitude = 30.1,
                    Timestamp = DateTime.UtcNow.AddMinutes(-10),
                    Notes = "Start location"
                },
                new GpsTracking
                {
                    DeliveryId = delivery.Id,
                    Latitude = 50.2,
                    Longitude = 30.2,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Notes = "Mid location"
                },
                new GpsTracking
                {
                    DeliveryId = delivery.Id,
                    Latitude = 50.3,
                    Longitude = 30.3,
                    Timestamp = DateTime.UtcNow,
                    Notes = "Current location"
                }
            };

            _context.GpsTrackings.AddRange(gpsEntries);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLocationHistory(delivery.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var history = actionResult?.Value as List<GpsTracking>;
            history.Should().HaveCount(3);
            history.Should().BeInDescendingOrder(g => g.Timestamp); // Should be ordered by newest first
            history?.First().Notes.Should().Be("Current location");
            history?.Last().Notes.Should().Be("Start location");
        }

        [Fact]
        public async Task GetLocationHistory_ReturnsNotFound_WhenDeliveryDoesNotExist()
        {
            // Act
            var result = await _controller.GetLocationHistory(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetLocationHistory_ReturnsEmptyList_WhenNoTrackingData()
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
                Status = DeliveryStatus.InTransit,
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetLocationHistory(delivery.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var history = actionResult?.Value as List<GpsTracking>;
            history.Should().NotBeNull();
            history.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateLocation_DetectsArrival_WhenNearDestination()
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
                Status = DeliveryStatus.InTransit,
                TotalAmount = 150.0m,
                CreatedAt = DateTime.UtcNow,
                DriverId = "DRV001",
                GpsTrackerId = "GPS001"
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Location very close to store (within 50 meters)
            var locationData = new
            {
                DeliveryId = delivery.Id,
                Latitude = 51.0001, // Very close to store latitude
                Longitude = 31.0001, // Very close to store longitude
                Notes = "Near destination"
            };

            // Act
            var result = await _controller.UpdateLocation(locationData);

            // Assert
            result.Should().BeOfType<OkResult>();

            // Verify arrival notification was sent
            _clientProxyMock.Verify(
                c => c.SendCoreAsync(
                    "LocationUpdated",
                    It.Is<object[]>(args =>
                        args.Length > 0 &&
                        args[0].ToString()!.Contains("Прибуття на місце призначення")),
                    default),
                Times.Once);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
