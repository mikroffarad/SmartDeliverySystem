namespace SmartDeliverySystem.Services
{
    public interface IServiceBusService 
    {
        Task SendLocationUpdateAsync(object message);
    }
}
