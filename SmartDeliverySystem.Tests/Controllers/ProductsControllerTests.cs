using Xunit;
using Microsoft.AspNetCore.Mvc;
using SmartDeliverySystem.Controllers;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class ProductsControllerTests : BaseTest
    {
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _controller = new ProductsController(Context, Mapper);
        }

        [Fact]
        public async Task GetProducts_ReturnsAllProducts()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var product1 = TestDataHelper.CreateTestProduct(1, vendor.Id, "Product1");
            var product2 = TestDataHelper.CreateTestProduct(2, vendor.Id, "Product2");

            Context.Vendors.Add(vendor);
            Context.Products.AddRange(product1, product2);
            await Context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var products = Assert.IsType<List<ProductDto>>(okResult.Value);
            Assert.Equal(2, products.Count);
        }

        [Fact]
        public async Task GetProduct_ExistingProduct_ReturnsProduct()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            Context.Vendors.Add(vendor);
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(product.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProduct = Assert.IsType<ProductDto>(okResult.Value);
            Assert.Equal(product.Id, returnedProduct.Id);
            Assert.Equal(product.Name, returnedProduct.Name);
        }

        [Fact]
        public async Task GetProduct_NonExistentProduct_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetProduct(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateProduct_ValidProduct_ReturnsCreatedResult()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            Context.Vendors.Add(vendor);
            await Context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Name = "New Product",
                Category = "Electronics",
                Weight = 2.5,
                Price = 199.99m,
                VendorId = vendor.Id
            };

            // Act
            var result = await _controller.CreateProduct(productDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var product = Assert.IsType<ProductDto>(createdResult.Value);
            Assert.Equal(productDto.Name, product.Name);
            Assert.Equal(productDto.VendorId, product.VendorId);

            // Verify it was saved to database
            var savedProduct = await Context.Products.FindAsync(product.Id);
            Assert.NotNull(savedProduct);
            Assert.Equal(productDto.Name, savedProduct.Name);
        }

        [Fact]
        public async Task CreateProduct_NonExistentVendor_ReturnsBadRequest()
        {
            // Arrange
            var productDto = new ProductDto
            {
                Name = "New Product",
                Category = "Electronics",
                Weight = 2.5,
                Price = 199.99m,
                VendorId = 999 // Non-existent vendor
            };

            // Act
            var result = await _controller.CreateProduct(productDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Vendor with ID 999 not found", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task UpdateProduct_ValidProduct_ReturnsNoContent()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            Context.Vendors.Add(vendor);
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            var updateDto = new ProductDto
            {
                Id = product.Id,
                Name = "Updated Product",
                Category = "Updated Category",
                Weight = 3.0,
                Price = 299.99m,
                VendorId = vendor.Id
            };

            // Act
            var result = await _controller.UpdateProduct(product.Id, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var updatedProduct = await Context.Products.FindAsync(product.Id);
            Assert.Equal(updateDto.Name, updatedProduct.Name);
            Assert.Equal(updateDto.Category, updatedProduct.Category);
            Assert.Equal(updateDto.Price, updatedProduct.Price);
        }

        [Fact]
        public async Task UpdateProduct_NonExistentProduct_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new ProductDto
            {
                Id = 999,
                Name = "Updated Product",
                Category = "Updated Category",
                Weight = 3.0,
                Price = 299.99m,
                VendorId = 1
            };

            // Act
            var result = await _controller.UpdateProduct(999, updateDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_ValidProduct_ReturnsNoContent()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);

            Context.Vendors.Add(vendor);
            Context.Products.Add(product);
            await Context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteProduct(product.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var deletedProduct = await Context.Products.FindAsync(product.Id);
            Assert.Null(deletedProduct);
        }

        [Fact]
        public async Task DeleteProduct_NonExistentProduct_ReturnsNotFound()
        {
            // Act
            var result = await _controller.DeleteProduct(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_HasDeliveries_ReturnsBadRequest()
        {
            // Arrange
            var vendor = TestDataHelper.CreateTestVendor();
            var product = TestDataHelper.CreateTestProduct(vendorId: vendor.Id);
            var delivery = TestDataHelper.CreateTestDelivery(vendorId: vendor.Id);
            var deliveryProduct = new DeliveryProduct
            {
                DeliveryId = delivery.Id,
                ProductId = product.Id,
                Quantity = 1
            };

            Context.Vendors.Add(vendor);
            Context.Products.Add(product);
            Context.Deliveries.Add(delivery);
            Context.DeliveryProducts.Add(deliveryProduct);
            await Context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteProduct(product.Id);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("is associated with deliveries", badRequestResult.Value.ToString());
        }
    }
}
