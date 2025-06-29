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
            return new DeliveryService(context, logger.Object);
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
            var result = await service.ProcessPaymentAsync(1, payment);

            // Assert
            Assert.True(result);
            var updated = await context.Deliveries.FindAsync(1);
            Assert.Equal(DeliveryStatus.Paid, updated.Status);
            Assert.Equal(100, updated.PaidAmount);
            Assert.Equal("Card", updated.PaymentMethod);
            Assert.NotNull(updated.PaymentDate);
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
    }
}