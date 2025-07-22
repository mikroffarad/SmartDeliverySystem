using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.DTOs;
using System.Text.Json;

namespace SmartDeliverySystem.Tests.Services
{
    public class ServiceBusServiceTests
    {
        private readonly Mock<ServiceBusClient> _mockClient;
        private readonly Mock<ServiceBusSender> _mockSender;
        private readonly Mock<ILogger<ServiceBusService>> _mockLogger;
        private readonly ServiceBusService _service;

        public ServiceBusServiceTests()
        {
            _mockClient = new Mock<ServiceBusClient>();
            _mockSender = new Mock<ServiceBusSender>();
            _mockLogger = new Mock<ILogger<ServiceBusService>>(); _mockClient.Setup(c => c.CreateSender("delivery-requests"))
                     .Returns(_mockSender.Object);
            _mockClient.Setup(c => c.CreateSender("location-updates"))
                     .Returns(_mockSender.Object);

            _service = new ServiceBusService(_mockClient.Object, _mockLogger.Object);
        }
        [Fact]
        public async Task SendDeliveryRequestAsync_ValidMessage_SendsMessage()
        {
            // Arrange
            var request = new DeliveryRequestDto
            {
                VendorId = 1,
                StoreId = 1,
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = 1, Quantity = 2 }
                }
            };

            // Act
            await _service.SendDeliveryRequestAsync(request);

            // Assert
            _mockSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockClient.Verify(c => c.CreateSender("delivery-requests"), Times.Once);
        }

        [Fact]
        public async Task SendLocationUpdateAsync_ValidMessage_SendsMessage()
        {
            // Arrange
            var locationUpdate = new LocationUpdateServiceBusDto
            {
                DeliveryId = 1,
                Latitude = 50.4501,
                Longitude = 30.5234,
                Speed = 60.0,
                Notes = "On route",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await _service.SendLocationUpdateAsync(locationUpdate);

            // Assert
            _mockSender.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockClient.Verify(c => c.CreateSender("location-updates"), Times.Once);
        }
    }
}
