namespace SmartDeliverySystem.Services
{
    public interface IServiceBusService
    {
        Task SendDeliveryRequestAsync(object message);
        Task SendLocationUpdateAsync(object message);
    }
}
