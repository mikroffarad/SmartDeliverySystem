using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Xunit;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using System.Net;
using SmartDeliverySystem.Services;

namespace SmartDeliverySystem.Tests.Integration
{
    public class DeliveryIntegrationTests : IClassFixture<TestWebApplicationFactory<TestStartup>>
    {
        private readonly TestWebApplicationFactory<TestStartup> _factory;
        private readonly HttpClient _client;

        public DeliveryIntegrationTests(TestWebApplicationFactory<TestStartup> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateDelivery_EndToEnd_Success()
        {
            // Arrange - Setup test data
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DeliveryContext>();

            // Clean database before test
            await CleanDatabase(context);

            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            context.Vendors.Add(vendor);
            context.Stores.Add(store);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var request = new DeliveryRequestDto
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = product.Id, Quantity = 2 }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/delivery/request", content);

            // Assert - Add debugging for 400 error
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Request failed with {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var deliveryResponse = JsonSerializer.Deserialize<DeliveryResponseDto>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(deliveryResponse);
            Assert.Equal(store.Id, deliveryResponse.StoreId);
            Assert.Equal(product.Price * 2, deliveryResponse.TotalAmount);
        }

        [Fact]
        public async Task GetAllDeliveries_ReturnsDeliveries()
        {
            // Arrange - Setup test data
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DeliveryContext>();

            // Clean database before test
            await CleanDatabase(context);

            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var delivery = TestDataHelper.CreateTestDelivery(vendorId: vendor.Id, storeId: store.Id);

            context.Vendors.Add(vendor);
            context.Stores.Add(store);
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync("/api/delivery/all");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var deliveries = JsonSerializer.Deserialize<List<DeliveryDto>>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(deliveries);
            Assert.Single(deliveries);
        }

        [Fact]
        public async Task PaymentFlow_EndToEnd_Success()
        {
            // Arrange - Setup delivery
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DeliveryContext>();

            // Clean database before test
            await CleanDatabase(context);

            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var delivery = TestDataHelper.CreateTestDelivery(vendorId: vendor.Id, storeId: store.Id);
            delivery.Status = DeliveryStatus.PendingPayment;
            delivery.TotalAmount = 100.00m;

            context.Vendors.Add(vendor);
            context.Stores.Add(store);
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            var payment = new PaymentDto
            {
                Amount = 100.00m,
                PaymentMethod = "Credit Card"
            };

            var json = JsonSerializer.Serialize(payment);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Use correct HTTP method (POST)
            var response = await _client.PostAsync($"/api/delivery/{delivery.Id}/pay", content);

            // Assert with debugging
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Payment failed with {response.StatusCode}: {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            // Wait a bit and clear context tracking to force fresh query
            await Task.Delay(100);
            context.ChangeTracker.Clear();

            // Verify status was updated in the same context
            var updatedDelivery = await context.Deliveries.FindAsync(delivery.Id);
            Assert.NotNull(updatedDelivery);
            Assert.Equal(DeliveryStatus.Paid, updatedDelivery.Status);
            Assert.Equal(payment.Amount, updatedDelivery.PaidAmount);
            Assert.Equal(payment.PaymentMethod, updatedDelivery.PaymentMethod);
        }
        private static async Task CleanDatabase(DeliveryContext context)
        {
            // Remove all entities to have a clean state for each test
            context.Deliveries.RemoveRange(context.Deliveries);
            context.DeliveryProducts.RemoveRange(context.DeliveryProducts);
            context.DeliveryLocationHistory.RemoveRange(context.DeliveryLocationHistory);
            context.Products.RemoveRange(context.Products);
            context.Vendors.RemoveRange(context.Vendors);
            context.Stores.RemoveRange(context.Stores);

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetNonExistentDelivery_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/delivery/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task VendorCrud_EndToEnd_Success()
        {
            // Create Vendor
            var createDto = new VendorDto
            {
                Name = "Integration Test Vendor",
                Latitude = 50.4501,
                Longitude = 30.5234
            };

            var json = JsonSerializer.Serialize(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var createResponse = await _client.PostAsync("/api/vendors", content);
            createResponse.EnsureSuccessStatusCode();

            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            var createdVendor = JsonSerializer.Deserialize<Vendor>(createResponseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(createdVendor);
            Assert.Equal(createDto.Name, createdVendor.Name);

            // Get Vendor
            var getResponse = await _client.GetAsync($"/api/vendors/{createdVendor.Id}");
            getResponse.EnsureSuccessStatusCode();

            // Update Vendor
            createDto.Name = "Updated Vendor";
            var updateJson = JsonSerializer.Serialize(createDto);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

            var updateResponse = await _client.PutAsync($"/api/vendors/{createdVendor.Id}", updateContent);
            updateResponse.EnsureSuccessStatusCode();

            // Delete Vendor
            var deleteResponse = await _client.DeleteAsync($"/api/vendors/{createdVendor.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // Verify deletion
            var getAfterDeleteResponse = await _client.GetAsync($"/api/vendors/{createdVendor.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
        }
        [Fact]
        public async Task Debug_CheckAvailableRoutes()
        {
            // Check if controllers are registered
            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Try to get DeliveryService directly
            var deliveryService = serviceProvider.GetService<IDeliveryService>();
            Assert.NotNull(deliveryService);

            // Debug test to see what routes are available
            var response = await _client.GetAsync("/");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Try different endpoints to see which ones work
            var deliveryResponse = await _client.GetAsync("/api/delivery/all");
            var vendorsResponse = await _client.GetAsync("/api/vendors");
            var storesResponse = await _client.GetAsync("/api/stores");
            var productsResponse = await _client.GetAsync("/api/products");

            // Get response content for failed requests
            string deliveryContent = await deliveryResponse.Content.ReadAsStringAsync();
            string vendorsContent = await vendorsResponse.Content.ReadAsStringAsync();
            string storesContent = await storesResponse.Content.ReadAsStringAsync();
            string productsContent = await productsResponse.Content.ReadAsStringAsync();

            // This test will help us understand what's happening
            Assert.True(true, $"DeliveryService registered: {deliveryService != null}\n" +
                             $"Root: {response.StatusCode}\n" +
                             $"Delivery: {deliveryResponse.StatusCode} - {deliveryContent}\n" +
                             $"Vendors: {vendorsResponse.StatusCode} - {vendorsContent}\n" +
                             $"Stores: {storesResponse.StatusCode} - {storesContent}\n" +
                             $"Products: {productsResponse.StatusCode} - {productsContent}");
        }

        [Fact]
        public async Task Debug_CreateDelivery_ShowError()
        {
            // Arrange - Setup test data
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DeliveryContext>();

            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            context.Vendors.Add(vendor);
            context.Stores.Add(store);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Verify data was saved
            var savedVendor = await context.Vendors.FindAsync(vendor.Id);
            var savedStore = await context.Stores.FindAsync(store.Id);
            var savedProduct = await context.Products.FindAsync(product.Id);

            var request = new DeliveryRequestDto
            {
                VendorId = vendor.Id,
                StoreId = store.Id,
                Products = new List<ProductRequestDto>
                {
                    new ProductRequestDto { ProductId = product.Id, Quantity = 2 }
                }
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/delivery/request", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Debug information
            Assert.True(true, $"Status: {response.StatusCode}\n" +
                             $"Vendor exists: {savedVendor != null}\n" +
                             $"Store exists: {savedStore != null}\n" +
                             $"Product exists: {savedProduct != null}\n" +
                             $"Request JSON: {json}\n" +
                             $"Response: {responseContent}");
        }

        [Fact]
        public async Task Debug_TestSimpleEndpoints()
        {
            // Test if basic GET endpoints work first
            var vendorsResponse = await _client.GetAsync("/api/vendors");
            var storesResponse = await _client.GetAsync("/api/stores");
            var productsResponse = await _client.GetAsync("/api/products");
            var deliveriesResponse = await _client.GetAsync("/api/delivery/all");

            Assert.True(true, $"Vendors: {vendorsResponse.StatusCode}, " +
                             $"Stores: {storesResponse.StatusCode}, " +
                             $"Products: {productsResponse.StatusCode}, " +
                             $"Deliveries: {deliveriesResponse.StatusCode}");
        }

        [Fact]
        public async Task Debug_CheckDeliveryEndpoints()
        {
            // Test all delivery endpoints to see which ones exist
            var endpoints = new[]
            {
                "/api/delivery/all",
                "/api/delivery/1",
                "/api/delivery/1/pay",
                "/api/delivery/request"
            };

            var results = new List<string>();

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await _client.GetAsync(endpoint);
                    results.Add($"{endpoint}: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    results.Add($"{endpoint}: ERROR - {ex.Message}");
                }
            }

            // Also test POST to request
            try
            {
                var request = new { VendorId = 1, StoreId = 1, Products = new[] { new { ProductId = 1, Quantity = 1 } } };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/delivery/request", content);
                results.Add($"POST /api/delivery/request: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                results.Add($"POST /api/delivery/request: ERROR - {ex.Message}");
            }

            Assert.True(true, string.Join("\n", results));
        }

        [Fact]
        public async Task Debug_PaymentFlow_ShowDetails()
        {
            // Arrange - Setup delivery
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DeliveryContext>();

            var vendor = TestDataHelper.CreateTestVendor();
            var store = TestDataHelper.CreateTestStore();
            var delivery = TestDataHelper.CreateTestDelivery(vendorId: vendor.Id, storeId: store.Id);
            delivery.Status = DeliveryStatus.PendingPayment;
            delivery.TotalAmount = 100.00m;

            context.Vendors.Add(vendor);
            context.Stores.Add(store);
            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            // Verify delivery was saved
            var savedDelivery = await context.Deliveries.FindAsync(delivery.Id);

            var payment = new PaymentDto
            {
                Amount = 100.00m,
                PaymentMethod = "Credit Card"
            };

            var json = JsonSerializer.Serialize(payment);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/delivery/{delivery.Id}/pay", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Check status immediately after payment
            var immediateCheck = await context.Deliveries.FindAsync(delivery.Id);

            // Use fresh context to check status
            using var freshScope = _factory.Services.CreateScope();
            var freshContext = freshScope.ServiceProvider.GetRequiredService<DeliveryContext>();
            var freshDelivery = await freshContext.Deliveries.FindAsync(delivery.Id);

            Assert.True(true, $"Payment Response: {response.StatusCode}\n" +
                             $"Response Content: {responseContent}\n" +
                             $"Delivery ID: {delivery.Id}\n" +
                             $"Saved Delivery Status: {savedDelivery?.Status}\n" +
                             $"Immediate Check Status: {immediateCheck?.Status}\n" +
                             $"Fresh Context Status: {freshDelivery?.Status}\n" +
                             $"Fresh Context Paid Amount: {freshDelivery?.PaidAmount}\n" +
                             $"Fresh Context Payment Method: {freshDelivery?.PaymentMethod}");
        }
    }

    // Test implementations for external services (for this test file only)
    public class TestServiceBusService : IServiceBusService
    {
        public Task SendDeliveryRequestAsync(object message) => Task.CompletedTask;
        public Task SendLocationUpdateAsync(object message) => Task.CompletedTask;
    }

    public class TestSignalRService : ISignalRService
    {
        public Task SendLocationUpdateAsync(int deliveryId, double latitude, double longitude, string? notes = null) => Task.CompletedTask;
        public Task SendDeliveryStatusUpdateAsync(int deliveryId, string status) => Task.CompletedTask;
    }

    public class TestTableStorageService : ITableStorageService
    {
        public Task<List<LocationHistoryDto>> GetLocationHistoryAsync(int deliveryId) => Task.FromResult(new List<LocationHistoryDto>());
    }
}
