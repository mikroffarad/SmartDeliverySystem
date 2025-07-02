using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SmartDeliverySystem.Azure.Functions
{
    public class TestFunction
    {
        private readonly ILogger<TestFunction> _logger;

        public TestFunction(ILogger<TestFunction> logger)
        {
            _logger = logger;
        }

        [Function("TestFunction")]
        public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation("Test function executed at: {Time}", DateTime.Now);
            _logger.LogInformation("Azure Functions is working correctly!");
        }
    }
}
