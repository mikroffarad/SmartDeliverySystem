using Xunit;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Controllers;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace SmartDeliverySystem.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private (ProductsController controller, DeliveryContext context) GetController(string dbName)
        {
            var options = new DbContextOptionsBuilder<DeliveryContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new DeliveryContext(options);

            // Додаємо вендора для тестів
            context.Vendors.Add(new Vendor { Name = "Vendor1", ContactEmail = "v1@mail.com" });
            context.SaveChanges();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ProductDto, Product>();
                cfg.CreateMap<Product, ProductDto>();
            });
            var mapper = config.CreateMapper();

            var controller = new ProductsController(context, mapper);
            return (controller, context);
        }

        [Fact]
        public async Task CreateProduct_ReturnsCreated()
        {
            var (controller, context) = GetController("CreateProductDb");
            var vendorId = context.Vendors.First().Id;

            var dto = new ProductDto { Name = "Prod1", VendorId = vendorId, Price = 10, Category = "Cat", Weight = 1 };
            var result = await controller.CreateProduct(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task CreateProduct_InvalidVendor_ReturnsBadRequest()
        {
            var (controller, _) = GetController("InvalidVendorDb");
            var dto = new ProductDto { Name = "Prod1", VendorId = 999, Price = 10, Category = "Cat", Weight = 1 };

            var result = await controller.CreateProduct(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetProduct_ReturnsProduct()
        {
            var (controller, context) = GetController("GetProductDb");
            var vendorId = context.Vendors.First().Id;

            await controller.CreateProduct(new ProductDto { Name = "Prod1", VendorId = vendorId, Price = 10, Category = "Cat", Weight = 1 });
            var productId = context.Products.First().Id;

            var result = await controller.GetProduct(productId);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateProduct_ChangesData()
        {
            var (controller, context) = GetController("UpdateProductDb");
            var vendorId = context.Vendors.First().Id;

            await controller.CreateProduct(new ProductDto { Name = "Prod1", VendorId = vendorId, Price = 10, Category = "Cat", Weight = 1 });
            var productId = context.Products.First().Id;

            var updateDto = new ProductDto { Name = "Prod2", VendorId = vendorId, Price = 20, Category = "Cat2", Weight = 2 };
            var result = await controller.UpdateProduct(productId, updateDto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_RemovesProduct()
        {
            var (controller, context) = GetController("DeleteProductDb");
            var vendorId = context.Vendors.First().Id;

            await controller.CreateProduct(new ProductDto { Name = "Prod1", VendorId = vendorId, Price = 10, Category = "Cat", Weight = 1 });
            var productId = context.Products.First().Id;

            var result = await controller.DeleteProduct(productId);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
