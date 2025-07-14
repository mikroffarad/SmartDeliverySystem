using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetVendors_ShouldReturnSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/vendor");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetStores_ShouldReturnSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/store");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetProducts_ShouldReturnSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/product");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetActiveDeliveries_ShouldReturnSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/delivery/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateVendor_ShouldReturnCreatedStatusCode_WhenValidData()
        {
            // Arrange
            var vendorData = new
            {
                Name = "Test Vendor",
                Latitude = 50.4501,
                Longitude = 30.5234
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/vendor", vendorData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateStore_ShouldReturnCreatedStatusCode_WhenValidData()
        {
            // Arrange
            var storeData = new
            {
                Name = "Test Store",
                Latitude = 50.4501,
                Longitude = 30.5234
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/store", storeData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task CreateProduct_ShouldReturnBadRequest_WhenVendorNotExists()
        {
            // Arrange
            var productData = new
            {
                Name = "Test Product",
                Price = 100.0m,
                VendorId = 99999 // Non-existent vendor
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/product", productData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetVendor_ShouldReturnNotFound_WhenVendorNotExists()
        {
            // Act
            var response = await _client.GetAsync("/api/vendor/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetStore_ShouldReturnNotFound_WhenStoreNotExists()
        {
            // Act
            var response = await _client.GetAsync("/api/store/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetProduct_ShouldReturnNotFound_WhenProductNotExists()
        {
            // Act
            var response = await _client.GetAsync("/api/product/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task FullWorkflow_CreateVendorAndProduct_ShouldWork()
        {
            // 1. Create vendor
            var vendorData = new
            {
                Name = "Workflow Vendor",
                Latitude = 50.4501,
                Longitude = 30.5234
            };

            var vendorResponse = await _client.PostAsJsonAsync("/api/vendor", vendorData);
            vendorResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var vendorLocation = vendorResponse.Headers.Location;
            vendorLocation.Should().NotBeNull();

            // 2. Get the created vendor
            var getVendorResponse = await _client.GetAsync(vendorLocation);
            getVendorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var vendorJson = await getVendorResponse.Content.ReadAsStringAsync();
            vendorJson.Should().Contain("Workflow Vendor");

            // 3. Extract vendor ID from location header
            var vendorId = vendorLocation!.ToString().Split('/').Last();

            // 4. Create product for this vendor
            var productData = new
            {
                Name = "Workflow Product",
                Price = 25.99m,
                VendorId = int.Parse(vendorId)
            };

            var productResponse = await _client.PostAsJsonAsync("/api/product", productData);
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task UpdateLocation_ShouldReturnBadRequest_WhenDeliveryNotExists()
        {
            // Arrange
            var locationData = new
            {
                DeliveryId = 99999,
                Latitude = 50.4501,
                Longitude = 30.5234,
                Notes = "Test location"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/gps/update-location", locationData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetLocationHistory_ShouldReturnNotFound_WhenDeliveryNotExists()
        {
            // Act
            var response = await _client.GetAsync("/api/gps/location-history/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
