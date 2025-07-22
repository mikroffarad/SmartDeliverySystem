using Xunit;
using Moq;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace SmartDeliverySystem.Tests.Services
{
    public class DeliveryServiceTests : BaseTest
    {
        private readonly DeliveryService _deliveryService;
        private readonly Mock<ILogger<DeliveryService>> _mockLogger;

        public DeliveryServiceTests()
        {
            _mockLogger = CreateMockLogger<DeliveryService>();
            _deliveryService = new DeliveryService(Context, _mockLogger.Object, Mapper);
        }

        [Fact]
        public async Task CreateDeliveryAsync_ValidRequest_ReturnsDeliveryResponse()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            Context.Vendors.Add(vendor);
            Context.Stores.Add(store);
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            var request = new DeliveryRequestDto
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = product.Id, Quantity = 2 }
                }
            };

            // Act
            var result = await _deliveryService.CreateDeliveryAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(store.Id, result.StoreId);
            Assert.Equal(store.Name, result.StoreName);
            Assert.Equal(product.Price * 2, result.TotalAmount);
        }

        [Fact]
        public async Task CreateDeliveryAsync_ProductNotBelongToVendor_ThrowsException()
        {
            // Arrange
            var vendor1 = TestDataHelper.CreateTestVendor(1, "Vendor1");
            var vendor2 = TestDataHelper.CreateTestVendor(2, "Vendor2");
            var store = TestDataHelper.CreateTestStore();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor2.Id);

            Context.Vendors.AddRange(vendor1, vendor2);
            Context.Stores.Add(store);
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            var request = new DeliveryRequestDto
            {
                VendorId = vendor1.Id,
                StoreId = store.Id,
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = product.Id, Quantity = 1 }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _deliveryService.CreateDeliveryAsync(request));

            Assert.Contains("do not belong to the vendor", exception.Message);
        }

        [Fact]
        public async Task CreateDeliveryAsync_StoreNotFound_ThrowsException()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            Context.Vendors.Add(vendor);
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            var request = new DeliveryRequestDto
            {
                VendorId = vendor.Id,
                StoreId = 999, // Non-existent store
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = product.Id, Quantity = 1 }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _deliveryService.CreateDeliveryAsync(request));

            Assert.Contains("Store with ID 999 not found", exception.Message);
        }

        [Fact]
        public async Task GetDeliveryAsync_ExistingDelivery_ReturnsDelivery()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var delivery = TestDataHelper.CreateTestDelivery(vendorId: vendor.Id, storeId: store.Id);

            Context.Vendors.Add(vendor);
            Context.Stores.Add(store);
            Context.Deliveries.Add(delivery);
            await Context.SaveChangesAsync();

            // Act
            var result = await _deliveryService.GetDeliveryAsync(delivery.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(delivery.Id, result.Id);
            Assert.Equal(vendor.Name, result.Vendor.Name);
            Assert.Equal(store.Name, result.Store.Name);
        }

        [Fact]
        public async Task UpdateDeliveryStatusAsync_ValidDelivery_ReturnsTrue()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            Context.Deliveries.Add(delivery);
            await Context.SaveChangesAsync();

            // Act
            var result = await _deliveryService.UpdateDeliveryStatusAsync(delivery.Id, DeliveryStatus.Paid);

            // Assert
            Assert.True(result);
            var updatedDelivery = await Context.Deliveries.FindAsync(delivery.Id);
            Assert.Equal(DeliveryStatus.Paid, updatedDelivery.Status);
        }

        [Fact]
        public async Task UpdateDeliveryStatusAsync_NonExistentDelivery_ReturnsFalse()
        {
            // Act
            var result = await _deliveryService.UpdateDeliveryStatusAsync(999, DeliveryStatus.Paid);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FindBestStoreForDeliveryAsync_MultipleStores_ReturnsClosestStore()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var store1 = TestDataHelper.CreateTestStore(1, "Close Store");
            store1.Latitude = 50.4502; // Close to vendor
            store1.Longitude = 30.5235;

            var store2 = TestDataHelper.CreateTestStore(2, "Far Store");
            store2.Latitude = 51.0; // Far from vendor
            store2.Longitude = 31.0;

            Context.Vendors.Add(vendor);
            Context.Stores.AddRange(store1, store2);
            await Context.SaveChangesAsync();

            var products = new List<ProductRequestDto>
            {
                new ProductRequestDto { ProductId = 1, Quantity = 1 }
            };

            // Act
            var result = await _deliveryService.FindBestStoreForDeliveryAsync(vendor.Id, products);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(store1.Id, result.StoreId);
            Assert.Equal(store1.Name, result.StoreName);
            Assert.True(result.Distance < 1.0); // Should be close
        }

        [Fact]
        public async Task ProcessPaymentAsync_ValidPayment_ReturnsTrue()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.TotalAmount = 100.00m;
            Context.Deliveries.Add(delivery);
            await Context.SaveChangesAsync();

            var payment = new PaymentDto
            {
                Amount = 100.00m,
                PaymentMethod = "Credit Card"
            };

            // Act
            var result = await _deliveryService.ProcessPaymentAsync(delivery.Id, payment);

            // Assert
            Assert.True(result);
            var updatedDelivery = await Context.Deliveries.FindAsync(delivery.Id);
            Assert.Equal(DeliveryStatus.Paid, updatedDelivery.Status);
            Assert.Equal(payment.Amount, updatedDelivery.PaidAmount);
        }

        [Fact]
        public async Task ProcessPaymentAsync_IncorrectAmount_ThrowsException()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.TotalAmount = 100.00m;
            Context.Deliveries.Add(delivery);
            await Context.SaveChangesAsync();

            var payment = new PaymentDto
            {
                Amount = 50.00m, // Wrong amount
                PaymentMethod = "Credit Card"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _deliveryService.ProcessPaymentAsync(delivery.Id, payment));

            Assert.Contains("Payment amount mismatch", exception.Message);
        }

        [Fact]
        public async Task DeleteDeliveryAsync_ValidDelivery_ReturnsTrue()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Status = DeliveryStatus.PendingPayment; // Safe to delete
            Context.Deliveries.Add(delivery);
            await Context.SaveChangesAsync();

            // Act
            var result = await _deliveryService.DeleteDeliveryAsync(delivery.Id);

            // Assert
            Assert.True(result);
            var deletedDelivery = await Context.Deliveries.FindAsync(delivery.Id);
            Assert.Null(deletedDelivery);
        }

        [Fact]
        public async Task DeleteDeliveryAsync_InTransitDelivery_ReturnsFalse()
        {
            // Arrange
            var delivery = TestDataHelper.CreateTestDelivery();
            delivery.Status = DeliveryStatus.InTransit;
            Context.Deliveries.Add(delivery);
            await Context.SaveChangesAsync();

            // Act
            var result = await _deliveryService.DeleteDeliveryAsync(delivery.Id);

            // Assert
            Assert.False(result);
            var existingDelivery = await Context.Deliveries.FindAsync(delivery.Id);
            Assert.NotNull(existingDelivery); // Should still exist
        }
    }
}
