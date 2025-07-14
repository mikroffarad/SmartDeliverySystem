using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Api.Controllers;
using SmartDeliverySystem.Core.Entities;
using SmartDeliverySystem.Infrastructure.Data;
using Xunit;
using FluentAssertions;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class ProductControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductController _controller;

        public ProductControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new ProductController(_context);
        }

        [Fact]
        public async Task GetAllProducts_ReturnsEmptyList_WhenNoProductsExist()
        {
            // Act
            var result = await _controller.GetAllProducts();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllProducts_ReturnsProductsList_WhenProductsExist()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Name = "Product 1", Price = 10.0m, VendorId = vendor.Id },
                new Product { Name = "Product 2", Price = 20.0m, VendorId = vendor.Id }
            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAllProducts();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Name == "Product 1");
            result.Should().Contain(p => p.Name == "Product 2");
        }

        [Fact]
        public async Task GetProductById_ReturnsProduct_WhenProductExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var product = new Product { Name = "Test Product", Price = 15.5m, VendorId = vendor.Id };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProductById(product.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var returnedProduct = actionResult?.Value as Product;
            returnedProduct.Should().NotBeNull();
            returnedProduct?.Name.Should().Be("Test Product");
            returnedProduct?.Price.Should().Be(15.5m);
        }

        [Fact]
        public async Task GetProductById_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Act
            var result = await _controller.GetProductById(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateProduct_ReturnsCreatedProduct_WhenValidData()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var productData = new
            {
                Name = "New Product",
                Price = 25.99m,
                Description = "Test Description",
                VendorId = vendor.Id
            };

            // Act
            var result = await _controller.CreateProduct(productData);

            // Assert
            var actionResult = result.Result as CreatedAtActionResult;
            actionResult.Should().NotBeNull();

            var createdProduct = actionResult?.Value as Product;
            createdProduct.Should().NotBeNull();
            createdProduct?.Name.Should().Be("New Product");
            createdProduct?.Price.Should().Be(25.99m);
            createdProduct?.Description.Should().Be("Test Description");
            createdProduct?.VendorId.Should().Be(vendor.Id);

            // Verify it's in database
            var productInDb = await _context.Products.FindAsync(createdProduct?.Id);
            productInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateProduct_ReturnsBadRequest_WhenVendorDoesNotExist()
        {
            // Arrange
            var productData = new
            {
                Name = "New Product",
                Price = 25.99m,
                Description = "Test Description",
                VendorId = 999 // Non-existent vendor
            };

            // Act
            var result = await _controller.CreateProduct(productData);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateProduct_ReturnsBadRequest_WhenInvalidPrice()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var productData = new
            {
                Name = "New Product",
                Price = -5.0m, // Invalid negative price
                Description = "Test Description",
                VendorId = vendor.Id
            };

            // Act
            var result = await _controller.CreateProduct(productData);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateProduct_ReturnsUpdatedProduct_WhenProductExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Original Name",
                Price = 10.0m,
                Description = "Original Description",
                VendorId = vendor.Id
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var updateData = new
            {
                Name = "Updated Name",
                Price = 15.0m,
                Description = "Updated Description"
            };

            // Act
            var result = await _controller.UpdateProduct(product.Id, updateData);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var updatedProduct = actionResult?.Value as Product;
            updatedProduct.Should().NotBeNull();
            updatedProduct?.Name.Should().Be("Updated Name");
            updatedProduct?.Price.Should().Be(15.0m);
            updatedProduct?.Description.Should().Be("Updated Description");
        }

        [Fact]
        public async Task UpdateProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var updateData = new
            {
                Name = "Updated Name",
                Price = 15.0m,
                Description = "Updated Description"
            };

            // Act
            var result = await _controller.UpdateProduct(999, updateData);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteProduct_ReturnsNoContent_WhenProductExists()
        {
            // Arrange
            var vendor = new Vendor { Name = "Test Vendor", Latitude = 50.0, Longitude = 30.0 };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            var product = new Product
            {
                Name = "To Delete",
                Price = 10.0m,
                VendorId = vendor.Id
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteProduct(product.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify it's deleted from database
            var deletedProduct = await _context.Products.FindAsync(product.Id);
            deletedProduct.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Act
            var result = await _controller.DeleteProduct(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetProductsByVendor_ReturnsVendorProducts_WhenVendorHasProducts()
        {
            // Arrange
            var vendor1 = new Vendor { Name = "Vendor 1", Latitude = 50.0, Longitude = 30.0 };
            var vendor2 = new Vendor { Name = "Vendor 2", Latitude = 51.0, Longitude = 31.0 };
            _context.Vendors.AddRange(vendor1, vendor2);
            await _context.SaveChangesAsync();

            var products = new List<Product>
            {
                new Product { Name = "Product 1", Price = 10.0m, VendorId = vendor1.Id },
                new Product { Name = "Product 2", Price = 20.0m, VendorId = vendor1.Id },
                new Product { Name = "Product 3", Price = 30.0m, VendorId = vendor2.Id }
            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProductsByVendor(vendor1.Id);

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();

            var vendorProducts = actionResult?.Value as List<Product>;
            vendorProducts.Should().HaveCount(2);
            vendorProducts.Should().AllSatisfy(p => p.VendorId.Should().Be(vendor1.Id));
            vendorProducts.Should().Contain(p => p.Name == "Product 1");
            vendorProducts.Should().Contain(p => p.Name == "Product 2");
        }

        [Fact]
        public async Task GetProductsByVendor_ReturnsNotFound_WhenVendorDoesNotExist()
        {
            // Act
            var result = await _controller.GetProductsByVendor(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
