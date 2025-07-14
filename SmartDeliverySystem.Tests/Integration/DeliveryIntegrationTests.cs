using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartDeliverySystem.Core.Entities;
using SmartDeliverySystem.Core.Enums;
using SmartDeliverySystem.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using System.Net;

namespace SmartDeliverySystem.Tests.Integration
{
    public class DeliveryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public DeliveryIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add DbContext using in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task FullDeliveryWorkflow_ShouldWorkEndToEnd()
        {
            // 1. Create vendor
            var vendorData = new
            {
                Name = "Test Vendor",
                Latitude = 50.0,
                Longitude = 30.0
            };

            var vendorResponse = await _client.PostAsJsonAsync("/api/vendor", vendorData);
            vendorResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var vendor = await vendorResponse.Content.ReadFromJsonAsync<Vendor>();
            vendor.Should().NotBeNull();

            // 2. Create store
            var storeData = new
            {
                Name = "Test Store",
                Latitude = 51.0,
                Longitude = 31.0
            };

            var storeResponse = await _client.PostAsJsonAsync("/api/store", storeData);
            storeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var store = await storeResponse.Content.ReadFromJsonAsync<Store>();
            store.Should().NotBeNull();

            // 3. Create product
            var productData = new
            {
                Name = "Test Product",
                Price = 25.0m,
                Description = "Test Description",
                VendorId = vendor!.Id
            };

            var productResponse = await _client.PostAsJsonAsync("/api/product", productData);
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = await productResponse.Content.ReadFromJsonAsync<Product>();
            product.Should().NotBeNull();

            // 4. Create delivery
            var deliveryData = new
            {
                VendorId = vendor.Id,
                StoreId = store!.Id,
                Items = new[]
                {
                    new { ProductId = product!.Id, Quantity = 4 }
                }
            };

            var deliveryResponse = await _client.PostAsJsonAsync("/api/delivery", deliveryData);
            deliveryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var delivery = await deliveryResponse.Content.ReadFromJsonAsync<Delivery>();
            delivery.Should().NotBeNull();
            delivery!.TotalAmount.Should().Be(100.0m); // 25 * 4

            // 5. Assign driver
            var driverData = new
            {
                DriverId = "DRV001",
                GpsTrackerId = "GPS001"
            };

            var assignResponse = await _client.PutAsJsonAsync($"/api/delivery/{delivery.Id}/assign-driver", driverData);
            assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 6. Update location
            var locationData = new
            {
                DeliveryId = delivery.Id,
                Latitude = 50.5,
                Longitude = 30.5,
                Notes = "En route"
            };

            var locationResponse = await _client.PostAsJsonAsync("/api/gps/update-location", locationData);
            locationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 7. Mark as delivered
            var statusResponse = await _client.PutAsJsonAsync($"/api/delivery/{delivery.Id}/status", (int)DeliveryStatus.Delivered);
            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 8. Verify store inventory was updated
            var inventoryResponse = await _client.GetAsync($"/api/store/{store.Id}/inventory");
            inventoryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var inventoryJson = await inventoryResponse.Content.ReadAsStringAsync();
            inventoryJson.Should().Contain("\"quantity\":4");

            // 9. Verify delivery is no longer in active deliveries
            var activeResponse = await _client.GetAsync("/api/delivery/active");
            activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var activeDeliveries = await activeResponse.Content.ReadFromJsonAsync<List<Delivery>>();
            activeDeliveries.Should().NotContain(d => d.Id == delivery.Id);
        }

        [Fact]
        public async Task GetActiveDeliveries_ShouldReturnOnlyActiveDeliveries()
        {
            // Create test data using API endpoints
            var vendor = await CreateTestVendor("API Vendor", 50.0, 30.0);
            var store = await CreateTestStore("API Store", 51.0, 31.0);
            var product = await CreateTestProduct("API Product", 15.0m, vendor.Id);

            // Create multiple deliveries with different statuses
            var delivery1 = await CreateTestDelivery(vendor.Id, store.Id, product.Id, 2);
            var delivery2 = await CreateTestDelivery(vendor.Id, store.Id, product.Id, 3);
            var delivery3 = await CreateTestDelivery(vendor.Id, store.Id, product.Id, 1);

            // Set delivery1 to InTransit
            await _client.PutAsJsonAsync($"/api/delivery/{delivery1.Id}/status", (int)DeliveryStatus.InTransit);

            // Set delivery3 to Delivered
            await _client.PutAsJsonAsync($"/api/delivery/{delivery3.Id}/status", (int)DeliveryStatus.Delivered);

            // Get active deliveries
            var response = await _client.GetAsync("/api/delivery/active");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var activeDeliveries = await response.Content.ReadFromJsonAsync<List<Delivery>>();
            activeDeliveries.Should().HaveCount(2); // Only Pending and InTransit should be returned
            activeDeliveries.Should().Contain(d => d.Id == delivery1.Id);
            activeDeliveries.Should().Contain(d => d.Id == delivery2.Id);
            activeDeliveries.Should().NotContain(d => d.Id == delivery3.Id);
        }

        [Fact]
        public async Task UpdateLocation_ShouldRejectInvalidCoordinates()
        {
            var vendor = await CreateTestVendor("GPS Vendor", 50.0, 30.0);
            var store = await CreateTestStore("GPS Store", 51.0, 31.0);
            var product = await CreateTestProduct("GPS Product", 10.0m, vendor.Id);
            var delivery = await CreateTestDelivery(vendor.Id, store.Id, product.Id, 1);

            // Assign driver to make delivery InTransit
            var driverData = new { DriverId = "DRV001", GpsTrackerId = "GPS001" };
            await _client.PutAsJsonAsync($"/api/delivery/{delivery.Id}/assign-driver", driverData);

            // Try to update with invalid coordinates
            var invalidLocationData = new
            {
                DeliveryId = delivery.Id,
                Latitude = 200.0, // Invalid latitude
                Longitude = 30.5,
                Notes = "Invalid location"
            };

            var response = await _client.PostAsJsonAsync("/api/gps/update-location", invalidLocationData);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateDelivery_ShouldRejectInvalidData()
        {
            // Try to create delivery with non-existent vendor
            var invalidDeliveryData = new
            {
                VendorId = 999,
                StoreId = 1,
                Items = new[]
                {
                    new { ProductId = 1, Quantity = 1 }
                }
            };

            var response = await _client.PostAsJsonAsync("/api/delivery", invalidDeliveryData);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // Helper methods
        private async Task<Vendor> CreateTestVendor(string name, double lat, double lng)
        {
            var data = new { Name = name, Latitude = lat, Longitude = lng };
            var response = await _client.PostAsJsonAsync("/api/vendor", data);
            return (await response.Content.ReadFromJsonAsync<Vendor>())!;
        }

        private async Task<Store> CreateTestStore(string name, double lat, double lng)
        {
            var data = new { Name = name, Latitude = lat, Longitude = lng };
            var response = await _client.PostAsJsonAsync("/api/store", data);
            return (await response.Content.ReadFromJsonAsync<Store>())!;
        }

        private async Task<Product> CreateTestProduct(string name, decimal price, int vendorId)
        {
            var data = new { Name = name, Price = price, VendorId = vendorId };
            var response = await _client.PostAsJsonAsync("/api/product", data);
            return (await response.Content.ReadFromJsonAsync<Product>())!;
        }

        private async Task<Delivery> CreateTestDelivery(int vendorId, int storeId, int productId, int quantity)
        {
            var data = new
            {
                VendorId = vendorId,
                StoreId = storeId,
                Items = new[] { new { ProductId = productId, Quantity = quantity } }
            };
            var response = await _client.PostAsJsonAsync("/api/delivery", data);
            return (await response.Content.ReadFromJsonAsync<Delivery>())!;
        }
    }
}
