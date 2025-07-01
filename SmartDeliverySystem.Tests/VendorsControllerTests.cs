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
    public class VendorsControllerTests
    {
        private VendorsController GetController(string dbName)
        {
            var options = new DbContextOptionsBuilder<DeliveryContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new DeliveryContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<VendorDto, Vendor>();
                cfg.CreateMap<Vendor, VendorDto>();
                cfg.CreateMap<Vendor, VendorWithProductsDto>();
            });
            var mapper = config.CreateMapper();

            return new VendorsController(context, mapper);
        }

        [Fact]
        public async Task CreateVendor_ReturnsCreated()
        {
            var controller = GetController("CreateVendorDb");
            var dto = new VendorDto { Name = "Test", ContactEmail = "test@mail.com", Address = "Test address" };

            var result = await controller.CreateVendor(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task CreateVendor_DuplicateName_ReturnsBadRequest()
        {
            var controller = GetController("DuplicateVendorDb");
            await controller.CreateVendor(new VendorDto { Name = "Test", ContactEmail = "a@mail.com", Address = "Test address" });

            var result = await controller.CreateVendor(new VendorDto { Name = "Test", ContactEmail = "b@mail.com", Address = "Test address" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetVendor_ReturnsVendor()
        {
            var controller = GetController("GetVendorDb");
            var create = await controller.CreateVendor(new VendorDto { Name = "Test", ContactEmail = "test@mail.com", Address = "Test address" });
            var created = (create.Result as CreatedAtActionResult).Value as Vendor;

            var result = await controller.GetVendor(created.Id);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateVendor_ChangesData()
        {
            var controller = GetController("UpdateVendorDb");
            var create = await controller.CreateVendor(new VendorDto { Name = "Test", ContactEmail = "test@mail.com", Address = "Test address" });
            var created = (create.Result as CreatedAtActionResult).Value as Vendor;

            var updateDto = new VendorDto { Name = "Test2", ContactEmail = "test2@mail.com", Address = "Test address 2" };
            var result = await controller.UpdateVendor(created.Id, updateDto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteVendor_RemovesVendor()
        {
            var controller = GetController("DeleteVendorDb");
            var create = await controller.CreateVendor(new VendorDto { Name = "Test", ContactEmail = "test@mail.com", Address = "Test address" });
            var created = (create.Result as CreatedAtActionResult).Value as Vendor;

            var result = await controller.DeleteVendor(created.Id);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
