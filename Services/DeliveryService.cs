using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly DeliveryContext _context;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(DeliveryContext context, ILogger<DeliveryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DeliveryResponseDto> CreateDeliveryAsync(DeliveryRequestDto request)
        {
            _logger.LogInformation("Creating new delivery for vendor {VendorId}", request.VendorId);

            // Find best store
            var bestStore = await FindBestStoreAsync(request.VendorId, request.Products);

            // Calculate total amount
            var totalAmount = await CalculateTotalAmountAsync(request.Products);

            // Create delivery
            var delivery = new Delivery
            {
                VendorId = request.VendorId,
                StoreId = bestStore.Id,
                TotalAmount = totalAmount,
                Status = DeliveryStatus.Pending
            };

            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            // Add products to delivery
            var deliveryProducts = request.Products.Select(p => new DeliveryProduct
            {
                DeliveryId = delivery.Id,
                ProductId = p.ProductId,
                Quantity = p.Quantity
            }).ToList();

            _context.DeliveryProducts.AddRange(deliveryProducts);
            await _context.SaveChangesAsync();

            return new DeliveryResponseDto
            {
                DeliveryId = delivery.Id,
                StoreId = bestStore.Id,
                StoreName = bestStore.Name,
                StoreAddress = bestStore.Address,
                TotalAmount = totalAmount,
                EstimatedDeliveryTime = "2-3 hours" // Static so far
            };
        }

        public async Task<Store> FindBestStoreAsync(int vendorId, List<ProductRequestDto> products)
        {
            // Simple algorithm - find a nearest active store
            var vendor = await _context.Vendors.FindAsync(vendorId);
            if (vendor == null)
                throw new ArgumentException("Vendor not found");

            var activeStores = await _context.Stores
                .Where(s => s.IsActive)
                .ToListAsync();

            if (!activeStores.Any())
                throw new InvalidOperationException("No active stores available");

            // Find a nearest store (by coordinates)
            var nearestStore = activeStores.OrderBy(store =>
                CalculateDistance(vendor.Latitude, vendor.Longitude, store.Latitude, store.Longitude))
                .First();

            return nearestStore;
        }

        public async Task<Delivery?> GetDeliveryAsync(int deliveryId)
        {
            return await _context.Deliveries
                .Include(d => d.Vendor)
                .Include(d => d.Store)
                .Include(d => d.Products)
                .ThenInclude(dp => dp.Product)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);
        }

        public async Task<List<Delivery>> GetActiveDeliveriesAsync()
        {
            return await _context.Deliveries
                .Include(d => d.Vendor)
                .Include(d => d.Store)
                .Where(d => d.Status != DeliveryStatus.Delivered && d.Status != DeliveryStatus.Cancelled)
                .ToListAsync();
        }

        public async Task<bool> UpdateDeliveryStatusAsync(int deliveryId, DeliveryStatus status)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
                return false;

            delivery.Status = status;

            if (status == DeliveryStatus.Assigned && delivery.AssignedAt == null)
                delivery.AssignedAt = DateTime.UtcNow;

            if (status == DeliveryStatus.Delivered && delivery.DeliveredAt == null)
                delivery.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<decimal> CalculateTotalAmountAsync(List<ProductRequestDto> products)
        {
            decimal total = 0;

            foreach (var productRequest in products)
            {
                var product = await _context.Products.FindAsync(productRequest.ProductId);
                if (product != null)
                {
                    total += product.Price * productRequest.Quantity;
                }
            }

            return total;
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Simple distance calculation (haversine formula simplified)
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = 6371 * c; // Earth's radius in km

            return distance;
        }
    }
}