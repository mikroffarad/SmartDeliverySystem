using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace SmartDeliverySystem.Azure.Functions
{
    public class ProcessDeliveryFunction
    {
        private readonly ILogger<ProcessDeliveryFunction> _logger;

        public ProcessDeliveryFunction(ILogger<ProcessDeliveryFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessDelivery")]
        public void Run([ServiceBusTrigger("delivery-requests", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
        {
            _logger.LogInformation("🚚 Нова доставка отримана!");
            _logger.LogInformation("Message ID: {MessageId}", message.MessageId);
            _logger.LogInformation("Message Body: {Body}", message.Body.ToString());

            try
            {
                // Тут можна додати логіку обробки доставки
                var deliveryData = JsonSerializer.Deserialize<object>(message.Body.ToString());
                _logger.LogInformation("✅ Доставка успішно оброблена!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Помилка обробки доставки");
                throw;
            }
        }
    }
}
