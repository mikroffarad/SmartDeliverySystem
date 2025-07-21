using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Services
{
    public interface IDeliveryService
    {
        Task<DeliveryResponseDto> CreateDeliveryAsync(DeliveryRequestDto request);
        Task<FindBestStoreResponseDto> FindBestStoreForDeliveryAsync(int vendorId, List<ProductRequestDto> products);
        Task<Delivery?> GetDeliveryAsync(int deliveryId);
        Task<List<Delivery>> GetAllDeliveriesAsync();
        Task<List<object>> GetDeliveryProductsAsync(int deliveryId);
        Task<bool> UpdateDeliveryStatusAsync(int deliveryId, DeliveryStatus status);
        Task<bool> ProcessPaymentAsync(int deliveryId, PaymentDto payment);
        Task<bool> AssignDriverAsync(int deliveryId, AssignDriverDto dto); 
        Task<bool> UpdateLocationAsync(int deliveryId, LocationUpdateDto locationUpdate);
        Task<DeliveryTrackingDto?> GetDeliveryTrackingAsync(int deliveryId);
        Task<List<DeliveryTrackingDto>> GetAllActiveTrackingAsync();
        Task<bool> DeleteDeliveryAsync(int deliveryId);
    }
}
