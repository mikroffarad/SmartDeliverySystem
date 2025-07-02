using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Services;
using Xunit;

namespace SmartDeliverySystem.Tests.Services
{
    public class DeliveryServiceTests
    {
        private DeliveryContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DeliveryContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new DeliveryContext(options);
        }
        private DeliveryService GetService(DeliveryContext context)
        {
            var logger = new Mock<ILogger<DeliveryService>>();
            var mapper = new Mock<AutoMapper.IMapper>();
            return new DeliveryService(context, logger.Object, mapper.Object);
        }

        [Fact]
        public async Task ProcessPaymentAsync_SuccessfulPayment_UpdatesStatusAndReturnsTrue()
        {
            // Arrange
            var context = GetInMemoryContext();
            var delivery = new Delivery
            {
                Id = 1,
                TotalAmount = 100,
                Status = DeliveryStatus.Pending
            };
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            var service = GetService(context);
            var payment = new PaymentDto { Amount = 100, PaymentMethod = "Card" };

            // Act
            var result = await service.ProcessPaymentAsync(1, payment);            // Assert
            Assert.True(result);
            var updated = await context.Deliveries.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal(DeliveryStatus.Paid, updated.Status);
            Assert.Equal(100, updated.PaidAmount);
            Assert.Equal("Card", updated.PaymentMethod);
            Assert.True(updated.PaymentDate > DateTime.MinValue);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WrongAmount_ThrowsException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var delivery = new Delivery
            {
                Id = 2,
                TotalAmount = 200,
                Status = DeliveryStatus.Pending
            };
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            var service = GetService(context);
            var payment = new PaymentDto { Amount = 250, PaymentMethod = "Cash" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ProcessPaymentAsync(2, payment));
        }

        [Fact]
        public async Task ProcessPaymentAsync_AlreadyPaid_ReturnsFalse()
        {
            // Arrange
            var context = GetInMemoryContext();
            var delivery = new Delivery
            {
                Id = 3,
                TotalAmount = 50,
                Status = DeliveryStatus.Paid
            };
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            var service = GetService(context);
            var payment = new PaymentDto { Amount = 50, PaymentMethod = "Card" };

            // Act
            var result = await service.ProcessPaymentAsync(3, payment);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ProcessPaymentAsync_DeliveryNotFound_ReturnsFalse()
        {
            // Arrange
            var context = GetInMemoryContext();
            var service = GetService(context);
            var payment = new PaymentDto { Amount = 200, PaymentMethod = "Cash" };

            // Act
            var result = await service.ProcessPaymentAsync(999, payment);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateLocationAsync_ValidDelivery_UpdatesLocationAndHistory()
        {
            // Arrange
            var context = GetInMemoryContext();
            var delivery = new Delivery
            {
                Id = 1,
                VendorId = 1,
                StoreId = 1,
                Status = DeliveryStatus.InTransit
            };
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            var service = GetService(context);
            var locationUpdate = new LocationUpdateDto
            {
                Latitude = 50.4501,
                Longitude = 30.5234,
                Speed = 60.5,
                Notes = "На дорозі до магазину"
            };

            // Act
            var result = await service.UpdateLocationAsync(1, locationUpdate);

            // Assert
            Assert.True(result);
            var updatedDelivery = await context.Deliveries.FindAsync(1);
            Assert.NotNull(updatedDelivery);
            Assert.Equal(50.4501, updatedDelivery.CurrentLatitude);
            Assert.Equal(30.5234, updatedDelivery.CurrentLongitude);
            Assert.NotNull(updatedDelivery.LastLocationUpdate);

            var history = await context.DeliveryLocationHistory
                .FirstOrDefaultAsync(h => h.DeliveryId == 1);
            Assert.NotNull(history);
            Assert.Equal(50.4501, history.Latitude);
            Assert.Equal(30.5234, history.Longitude);
            Assert.Equal(60.5, history.Speed);
        }

        [Fact]
        public async Task GetDeliveryTrackingAsync_ValidDelivery_ReturnsTrackingInfo()
        {
            // Arrange
            var context = GetInMemoryContext();
            var delivery = new Delivery
            {
                Id = 1,
                VendorId = 1,
                StoreId = 1,
                Status = DeliveryStatus.InTransit,
                DriverId = "DRIVER001",
                GpsTrackerId = "GPS001",
                CurrentLatitude = 50.4501,
                CurrentLongitude = 30.5234,
                FromLatitude = 50.4400,
                FromLongitude = 30.5100,
                ToLatitude = 50.4600,
                ToLongitude = 30.5400
            };
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            var service = GetService(context);

            // Act
            var tracking = await service.GetDeliveryTrackingAsync(1);

            // Assert
            Assert.NotNull(tracking);
            Assert.Equal(1, tracking.DeliveryId);
            Assert.Equal("DRIVER001", tracking.DriverId);
            Assert.Equal("GPS001", tracking.GpsTrackerId);
            Assert.Equal(DeliveryStatus.InTransit, tracking.Status);
            Assert.Equal(50.4501, tracking.CurrentLatitude);
            Assert.Equal(30.5234, tracking.CurrentLongitude);
        }
    }
}
