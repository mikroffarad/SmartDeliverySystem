using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace SmartDeliverySystem.Services
{
    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusService> _logger;

        public ServiceBusService(ServiceBusClient client, ILogger<ServiceBusService> logger)
        {
            _client = client;
            _logger = logger;
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
