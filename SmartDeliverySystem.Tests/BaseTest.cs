using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Mappings;
using Moq;

namespace SmartDeliverySystem.Tests
{
    public abstract class BaseTest : IDisposable
    {
        protected readonly DeliveryContext Context;
        protected readonly IMapper Mapper;

        protected BaseTest()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<DeliveryContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new DeliveryContext(options);

            // Setup AutoMapper
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            Mapper = config.CreateMapper();
        }

        protected Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
