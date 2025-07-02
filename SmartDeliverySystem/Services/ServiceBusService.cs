using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace SmartDeliverySystem.Services
{
    public interface IServiceBusService
    {
        Task SendDeliveryRequestAsync(object message);
        Task SendLocationUpdateAsync(object message);
    }

    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(ServiceBusClient client, ILogger<ServiceBusService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task SendDeliveryRequestAsync(object message)
        {
            var sender = _client.CreateSender("delivery-requests");
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody);

            await sender.SendMessageAsync(serviceBusMessage);
            _logger.LogInformation("Delivery request sent to Service Bus");
        }

        public async Task SendLocationUpdateAsync(object message)
        {
            var sender = _client.CreateSender("location-updates");
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody);

            await sender.SendMessageAsync(serviceBusMessage);
            _logger.LogInformation("Location update sent to Service Bus");
        }
    }
}
