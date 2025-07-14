using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Controllers;
using SmartDeliverySystem.Models;
using SmartDeliverySystem.Data;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class StoreControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly StoreController _controller;

        public StoreControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new StoreController(_context);
        }

        [Fact]
        public async Task GetAllStores_ReturnsEmptyList_WhenNoStoresExist()
        {
            // Act
            var result = await _controller.GetAllStores();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllStores_ReturnsStoresList_WhenStoresExist()
        {
            // Arrange
            var stores = new List<Store>
            {
                new Store { Name = "Store 1", Latitude = 50.0, Longitude = 30.0 },
                new Store { Name = "Store 2", Latitude = 51.0, Longitude = 31.0 }
            };

            _context.Stores.AddRange(stores);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAllStores();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(s => s.Name == "Store 1");
            result.Should().Contain(s => s.Name == "Store 2");
        }

        [Fact]
        public async Task GetStore_ReturnsStore_WhenStoreExists()
        {
            // Arrange
            var store = new Store { Name = "Test Store", Latitude = 50.0, Longitude = 30.0 };
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetStore(store.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var returnedStore = actionResult!.Value as Store;
            returnedStore.Should().NotBeNull();
            returnedStore!.Name.Should().Be("Test Store");
        }

        [Fact]
        public async Task GetStore_ReturnsNotFound_WhenStoreDoesNotExist()
        {
            // Act
            var result = await _controller.GetStore(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateStore_ReturnsCreatedStore_WhenValidData()
        {
            // Arrange
            var storeData = new { Name = "New Store", Latitude = 50.5, Longitude = 30.5 };

            // Act
            var result = await _controller.CreateStore(storeData);

            // Assert
            var actionResult = result.Result as CreatedAtActionResult;
            actionResult.Should().NotBeNull();

            var createdStore = actionResult!.Value as Store;
            createdStore.Should().NotBeNull();
            createdStore!.Name.Should().Be("New Store");

            // Verify it's in database
            var storeInDb = await _context.Stores.FindAsync(createdStore.Id);
            storeInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateStore_ReturnsUpdatedStore_WhenStoreExists()
        {
            // Arrange
            var store = new Store { Name = "Original Name", Latitude = 50.0, Longitude = 30.0 };
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var updateData = new { Name = "Updated Name", Latitude = 51.0, Longitude = 31.0 };

            // Act
            var result = await _controller.UpdateStore(store.Id, updateData);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var updatedStore = actionResult!.Value as Store;
            updatedStore.Should().NotBeNull();
            updatedStore!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task DeleteStore_ReturnsNoContent_WhenStoreExists()
        {
            // Arrange
            var store = new Store { Name = "To Delete", Latitude = 50.0, Longitude = 30.0 };
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteStore(store.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify it's deleted from database
            var deletedStore = await _context.Stores.FindAsync(store.Id);
            deletedStore.Should().BeNull();
        }

        [Fact]
        public async Task GetStoreInventory_ReturnsInventory_WhenStoreHasInventory()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            var store = new Store { Name = "Test Store", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var product = new Product { Name = "Test Product", Price = 10.0m, VendorId = vendor.Id };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var inventoryItem = new StoreInventory
            {
                StoreId = store.Id,
                ProductId = product.Id,
                Quantity = 50
            };
            _context.StoreInventories.Add(inventoryItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetStoreInventory(store.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var inventory = actionResult!.Value as IEnumerable<object>;
            inventory.Should().NotBeNull();
            inventory.Should().HaveCountGreaterThan(0);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
