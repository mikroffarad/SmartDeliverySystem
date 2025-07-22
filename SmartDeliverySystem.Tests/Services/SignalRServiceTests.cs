using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.Hubs;

namespace SmartDeliverySystem.Tests.Services
{
    public class SignalRServiceTests
    {
        private readonly Mock<IHubContext<DeliveryTrackingHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<ILogger<SignalRService>> _mockLogger;
        private readonly SignalRService _service;

        public SignalRServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<DeliveryTrackingHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockLogger = new Mock<ILogger<SignalRService>>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _service = new SignalRService(_mockHubContext.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendLocationUpdateAsync_ValidUpdate_SendsToGroups()
        {
            // Arrange
            int deliveryId = 1;
            double latitude = 50.4501;
            double longitude = 30.5234;
            string notes = "On route";

            // Act
            await _service.SendLocationUpdateAsync(deliveryId, latitude, longitude, notes);

            // Assert
            _mockClients.Verify(c => c.Group($"Delivery_{deliveryId}"), Times.Once);
            _mockClients.Verify(c => c.Group("AllDeliveries"), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync("LocationUpdated",
                It.IsAny<object[]>(), default), Times.Exactly(2));
        }

        [Fact]
        public async Task SendDeliveryStatusUpdateAsync_ValidUpdate_SendsToGroups()
        {
            // Arrange
            int deliveryId = 1;
            string status = "Delivered";

            // Act
            await _service.SendDeliveryStatusUpdateAsync(deliveryId, status);

            // Assert
            _mockClients.Verify(c => c.Group($"Delivery_{deliveryId}"), Times.Once);
            _mockClients.Verify(c => c.Group("AllDeliveries"), Times.Once);
            _mockClientProxy.Verify(p => p.SendCoreAsync("StatusUpdated",
                It.IsAny<object[]>(), default), Times.Exactly(2));
        }
    }
}
