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
    public class StoresControllerTests
    {
        private StoresController GetController(string dbName)
        {
            var options = new DbContextOptionsBuilder<DeliveryContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new DeliveryContext(options);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<StoreDto, Store>();
                cfg.CreateMap<Store, StoreDto>();
            });
            var mapper = config.CreateMapper();

            return new StoresController(context, mapper);
        }

        [Fact]
        public async Task CreateStore_ReturnsCreated()
        {
            var controller = GetController("CreateStoreDb");
            var dto = new StoreDto { Name = "Store1", Address = "Addr" };

            var result = await controller.CreateStore(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task CreateStore_DuplicateName_ReturnsBadRequest()
        {
            var controller = GetController("DuplicateStoreDb");
            await controller.CreateStore(new StoreDto { Name = "Store1", Address = "Addr" });

            var result = await controller.CreateStore(new StoreDto { Name = "Store1", Address = "Addr2" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetStore_ReturnsStore()
        {
            var controller = GetController("GetStoreDb");
            var create = await controller.CreateStore(new StoreDto { Name = "Store1", Address = "Addr" });
            var created = (create.Result as CreatedAtActionResult).Value as Store;

            var result = await controller.GetStore(created.Id);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateStore_ChangesData()
        {
            var controller = GetController("UpdateStoreDb");
            var create = await controller.CreateStore(new StoreDto { Name = "Store1", Address = "Addr" });
            var created = (create.Result as CreatedAtActionResult).Value as Store;

            var updateDto = new StoreDto { Name = "Store2", Address = "Addr2" };
            var result = await controller.UpdateStore(created.Id, updateDto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteStore_RemovesStore()
        {
            var controller = GetController("DeleteStoreDb");
            var create = await controller.CreateStore(new StoreDto { Name = "Store1", Address = "Addr" });
            var created = (create.Result as CreatedAtActionResult).Value as Store;

            var result = await controller.DeleteStore(created.Id);

            Assert.IsType<NoContentResult>(result);
        }
    }
}