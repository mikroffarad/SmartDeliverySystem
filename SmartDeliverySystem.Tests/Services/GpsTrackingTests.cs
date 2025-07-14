using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Data;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.Services
{
    public class GpsTrackingTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public GpsTrackingTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [Theory]
        [InlineData(50.4501, 30.5234, true)]  // Valid coordinates (Kyiv)
        [InlineData(0, 0, true)]              // Valid coordinates (Equator/Prime Meridian)
        [InlineData(-90, -180, true)]         // Valid edge case
        [InlineData(90, 180, true)]           // Valid edge case
        [InlineData(91, 30, false)]           // Invalid latitude
        [InlineData(50, 181, false)]          // Invalid longitude
        [InlineData(-91, 30, false)]          // Invalid latitude
        [InlineData(50, -181, false)]         // Invalid longitude
        public void ValidateCoordinates_ShouldReturnExpectedResult(double latitude, double longitude, bool expected)
        {
            // Act
            var result = IsValidCoordinate(latitude, longitude);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task UpdateDeliveryLocation_ShouldUpdateCoordinates_WhenValidDelivery()
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
                TotalAmount = 150m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Act
            delivery.CurrentLatitude = 50.5;
            delivery.CurrentLongitude = 30.5;
            delivery.LastLocationUpdate = DateTime.UtcNow;
            _context.Update(delivery);
            await _context.SaveChangesAsync();

            // Create GPS tracking entry
            var gpsEntry = new GpsTracking
            {
                DeliveryId = delivery.Id,
                Latitude = 50.5,
                Longitude = 30.5,
                Timestamp = DateTime.UtcNow,
                Notes = "Location update test"
            };
            _context.GpsTrackings.Add(gpsEntry);
            await _context.SaveChangesAsync();

            // Assert
            var updatedDelivery = await _context.Deliveries.FindAsync(delivery.Id);
            updatedDelivery!.CurrentLatitude.Should().Be(50.5);
            updatedDelivery.CurrentLongitude.Should().Be(30.5);

            var gpsRecord = await _context.GpsTrackings
                .Where(g => g.DeliveryId == delivery.Id)
                .FirstOrDefaultAsync();
            gpsRecord.Should().NotBeNull();
            gpsRecord!.Latitude.Should().Be(50.5);
            gpsRecord.Longitude.Should().Be(30.5);
        }

        [Fact]
        public async Task GetLocationHistory_ShouldReturnOrderedHistory()
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
                TotalAmount = 150m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Create multiple GPS entries
            var gpsEntries = new List<GpsTracking>
            {
                new GpsTracking
                {
                    DeliveryId = delivery.Id,
                    Latitude = 50.1,
                    Longitude = 30.1,
                    Timestamp = DateTime.UtcNow.AddMinutes(-10),
                    Notes = "Start"
                },
                new GpsTracking
                {
                    DeliveryId = delivery.Id,
                    Latitude = 50.2,
                    Longitude = 30.2,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Notes = "Middle"
                },
                new GpsTracking
                {
                    DeliveryId = delivery.Id,
                    Latitude = 50.3,
                    Longitude = 30.3,
                    Timestamp = DateTime.UtcNow,
                    Notes = "Current"
                }
            };

            _context.GpsTrackings.AddRange(gpsEntries);
            await _context.SaveChangesAsync();

            // Act
            var history = await _context.GpsTrackings
                .Where(g => g.DeliveryId == delivery.Id)
                .OrderByDescending(g => g.Timestamp)
                .ToListAsync();

            // Assert
            history.Should().HaveCount(3);
            history.Should().BeInDescendingOrder(g => g.Timestamp);
            history.First().Notes.Should().Be("Current");
            history.Last().Notes.Should().Be("Start");
        }

        [Theory]
        [InlineData(50.0, 30.0, 50.0001, 30.0001, true)]   // Very close (< 50m)
        [InlineData(50.0, 30.0, 50.001, 30.001, false)]    // Further away (> 50m)
        [InlineData(50.4501, 30.5234, 50.4502, 30.5235, true)] // Close in Kyiv
        public void IsNearDestination_ShouldDetectProximity(double storeLat, double storeLng,
            double currentLat, double currentLng, bool expectedNear)
        {
            // Act
            var distance = CalculateDistance(storeLat, storeLng, currentLat, currentLng);
            var isNear = distance < 0.05; // 50 meters

            // Assert
            isNear.Should().Be(expectedNear);
        }

        [Fact]
        public void CalculateDistance_ShouldReturnCorrectDistance()
        {
            // Arrange - coordinates of two points in Kyiv
            double lat1 = 50.4501; // Kyiv center
            double lng1 = 30.5234;
            double lat2 = 50.4601; // ~1.1 km north
            double lng2 = 30.5234;

            // Act
            var distance = CalculateDistance(lat1, lng1, lat2, lng2);

            // Assert
            distance.Should().BeApproximately(1.1, 0.2); // ~1.1 km with tolerance
        }

        // Helper methods
        private static bool IsValidCoordinate(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
