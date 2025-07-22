using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartDeliverySystem.Controllers;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class DeliveryControllerTests : BaseTest
    {
        private readonly DeliveryController _controller;
        private readonly Mock<IDeliveryService> _mockDeliveryService;
        private readonly Mock<IServiceBusService> _mockServiceBusService;
        private readonly Mock<ISignalRService> _mockSignalRService;
        private readonly Mock<ITableStorageService> _mockTableStorageService;
        private readonly Mock<ILogger<DeliveryController>> _mockLogger;

        public DeliveryControllerTests()
        {
            _mockDeliveryService = new Mock<IDeliveryService>();
            _mockServiceBusService = new Mock<IServiceBusService>();
            _mockSignalRService = new Mock<ISignalRService>();
            _mockTableStorageService = new Mock<ITableStorageService>();
            _mockLogger = CreateMockLogger<DeliveryController>();

            _controller = new DeliveryController(
                _mockDeliveryService.Object,
                _mockServiceBusService.Object,
                _mockSignalRService.Object,
                _mockTableStorageService.Object,
                Mapper,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task RequestDelivery_ValidRequest_ReturnsOkResult()
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

            var response = new DeliveryResponseDto
            {
                DeliveryId = 1,
                StoreId = 1,
                StoreName = "Test Store",
                TotalAmount = 50.00m
            };

            _mockDeliveryService.Setup(s => s.CreateDeliveryAsync(request))
                              .ReturnsAsync(response);

            // Act
            var result = await _controller.RequestDelivery(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResponse = Assert.IsType<DeliveryResponseDto>(okResult.Value);
            Assert.Equal(response.DeliveryId, returnedResponse.DeliveryId);
        }

        [Fact]
        public async Task RequestDelivery_EmptyProducts_ReturnsBadRequest()
        {
            // Arrange
            var request = new DeliveryRequestDto
            {
                VendorId = 1,
                StoreId = 1,
                Products = new List<ProductRequestDto>()
            };

            // Act
            var result = await _controller.RequestDelivery(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("At least one product is required", badRequestResult.Value);
        }

        [Fact]
        public async Task RequestDelivery_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var request = new DeliveryRequestDto
            {
                VendorId = 1,
                StoreId = 1,
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = 1, Quantity = 1 }
                }
            };

            _mockDeliveryService.Setup(s => s.CreateDeliveryAsync(request))
                              .ThrowsAsync(new InvalidOperationException("Store not found"));

            // Act
            var result = await _controller.RequestDelivery(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Store not found", badRequestResult.Value);
        }

        [Fact]
        public async Task GetDelivery_ExistingDelivery_ReturnsOkResult()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Vendor = TestDataHelper.CreateTestVendor();
            delivery.Store = TestDataHelper.CreateTestStore();

            _mockDeliveryService.Setup(s => s.GetDeliveryAsync(1))
                              .ReturnsAsync(delivery);

            // Act
            var result = await _controller.GetDelivery(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedDelivery = Assert.IsType<DeliveryDto>(okResult.Value);
            Assert.Equal(delivery.Id, returnedDelivery.DeliveryId);
        }

        [Fact]
        public async Task GetDelivery_NonExistentDelivery_ReturnsNotFound()
        {
            // Arrange
            _mockDeliveryService.Setup(s => s.GetDeliveryAsync(999))
                              .ReturnsAsync((Delivery?)null);

            // Act
            var result = await _controller.GetDelivery(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("Delivery with ID 999 not found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task UpdateDeliveryStatus_ValidDelivery_ReturnsOk()
        {
            // Arrange
            _mockDeliveryService.Setup(s => s.UpdateDeliveryStatusAsync(1, DeliveryStatus.Paid))
                              .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateDeliveryStatus(1, DeliveryStatus.Paid);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateDeliveryStatus_NonExistentDelivery_ReturnsNotFound()
        {
            // Arrange
            _mockDeliveryService.Setup(s => s.UpdateDeliveryStatusAsync(999, DeliveryStatus.Paid))
                              .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateDeliveryStatus(999, DeliveryStatus.Paid);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ProcessPayment_ValidPayment_ReturnsOk()
        {
            // Arrange
            var payment = new PaymentDto
            {
                Amount = 100.00m,
                PaymentMethod = "Credit Card"
            };

            _mockDeliveryService.Setup(s => s.ProcessPaymentAsync(1, payment))
                              .ReturnsAsync(true);

            // Act
            var result = await _controller.ProcessPayment(1, payment);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ProcessPayment_InvalidPayment_ReturnsBadRequest()
        {
            // Arrange
            var payment = new PaymentDto
            {
                Amount = 50.00m,
                PaymentMethod = "Credit Card"
            };

            _mockDeliveryService.Setup(s => s.ProcessPaymentAsync(1, payment))
                              .ThrowsAsync(new InvalidOperationException("Payment amount mismatch"));

            // Act
            var result = await _controller.ProcessPayment(1, payment);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Payment amount mismatch", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateLocation_ValidLocation_ReturnsOk()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Status = DeliveryStatus.InTransit;

            var locationUpdate = new LocationUpdateDto
            {
                Latitude = 50.4503,
                Longitude = 30.5236,
                Speed = 60.0,
                Notes = "On route"
            };

            _mockDeliveryService.Setup(s => s.GetDeliveryAsync(1))
                              .ReturnsAsync(delivery);
            _mockDeliveryService.Setup(s => s.UpdateLocationAsync(1, locationUpdate))
                              .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateLocation(1, locationUpdate);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateLocation_CompletedDelivery_ReturnsBadRequest()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Status = DeliveryStatus.Delivered;

            var locationUpdate = new LocationUpdateDto
            {
                Latitude = 50.4503,
                Longitude = 30.5236,
                Speed = 60.0
            };

            _mockDeliveryService.Setup(s => s.GetDeliveryAsync(1))
                              .ReturnsAsync(delivery);

            // Act
            var result = await _controller.UpdateLocation(1, locationUpdate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("already completed", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task DeleteDelivery_ValidDelivery_ReturnsNoContent()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Status = DeliveryStatus.PendingPayment;

            _mockDeliveryService.Setup(s => s.GetDeliveryAsync(1))
                              .ReturnsAsync(delivery);
            _mockDeliveryService.Setup(s => s.DeleteDeliveryAsync(1))
                              .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteDelivery(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteDelivery_InTransitDelivery_ReturnsBadRequest()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Status = DeliveryStatus.InTransit;

            _mockDeliveryService.Setup(s => s.GetDeliveryAsync(1))
                              .ReturnsAsync(delivery);

            // Act
            var result = await _controller.DeleteDelivery(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Cannot delete delivery that is currently in transit", badRequestResult.Value.ToString());
        }
    }
}
