namespace SmartDeliverySystem.Services
{
    public interface ISignalRService
    {
        Task SendLocationUpdateAsync(int deliveryId, double latitude, double longitude, string? notes = null);
        Task SendDeliveryStatusUpdateAsync(int deliveryId, string status);
    }
}
