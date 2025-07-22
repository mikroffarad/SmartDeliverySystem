using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.Mappings;
using SmartDeliverySystem.DTOs;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Moq;

namespace SmartDeliverySystem.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Database will be configured by TestWebApplicationFactory

            // Add required services for the app
            services.AddAutoMapper(typeof(MappingProfile));

            // Add controllers from the main project
            services.AddControllers()
                .AddApplicationPart(typeof(SmartDeliverySystem.Controllers.DeliveryController).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Add API Explorer for controller discovery
            services.AddEndpointsApiExplorer();

            // Add real services (not mocks for integration tests)
            services.AddScoped<IDeliveryService, DeliveryService>();

            // For external services that we can't test in integration, use simple test implementations
            services.AddScoped<IServiceBusService, TestServiceBusService>();
            services.AddScoped<ISignalRService, TestSignalRService>();
            services.AddScoped<ITableStorageService, TestTableStorageService>();

            // Add a mock ServiceBusClient since it's external
            var mockServiceBusClient = new Mock<ServiceBusClient>();
            services.AddSingleton(mockServiceBusClient.Object);

            // Add logging
            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    // Test implementations for external services
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
    public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly string _databaseName;

        public TestWebApplicationFactory()
        {
            // Create unique database name for each factory instance
            _databaseName = $"TestDb_{Guid.NewGuid()}";
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TestStartup>();
                    webBuilder.UseEnvironment("Testing");
                    webBuilder.ConfigureServices(services =>
                    {
                        // Override the database with our test-specific one
                        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DeliveryContext>));
                        if (descriptor != null)
                            services.Remove(descriptor);

                        services.AddDbContext<DeliveryContext>(options =>
                            options.UseInMemoryDatabase(_databaseName));
                    });
                });
        }
    }
}
