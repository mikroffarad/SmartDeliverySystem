using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Services
{
    public interface IDeliveryService
    {
        Task<DeliveryResponseDto> CreateDeliveryAsync(DeliveryRequestDto request);
        Task<Store> FindBestStoreAsync(int vendorId, List<ProductRequestDto> products);
        Task<Delivery?> GetDeliveryAsync(int deliveryId);
        Task<List<Delivery>> GetActiveDeliveriesAsync();
        Task<bool> UpdateDeliveryStatusAsync(int deliveryId, DeliveryStatus status);
        Task<bool> ProcessPaymentAsync(int deliveryId, PaymentDto payment);
        Task<bool> AssignDriverAsync(int deliveryId, AssignDriverDto dto);
    }
}