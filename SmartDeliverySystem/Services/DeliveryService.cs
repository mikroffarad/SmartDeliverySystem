using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly DeliveryContext _context;
        private readonly ILogger<DeliveryService> _logger;
        private readonly IMapper _mapper;


        public DeliveryService(DeliveryContext context, ILogger<DeliveryService> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<DeliveryResponseDto> CreateDeliveryAsync(DeliveryRequestDto request)
        {
            _logger.LogInformation("Creating new delivery for vendor {VendorId}", request.VendorId);

            // Check that all products belong to the vendor
            var productIds = request.Products.Select(p => p.ProductId).ToList();
            var vendorProductIds = await _context.Products
                .Where(p => p.VendorId == request.VendorId && productIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            if (vendorProductIds.Count != productIds.Count)
            {
                _logger.LogWarning("One or more products do not belong to vendor {VendorId}", request.VendorId);
                throw new InvalidOperationException("One or more products do not belong to the vendor.");
            }            // Find best store
            var bestStore = await FindBestStoreAsync(request.VendorId, request.Products);
            if (bestStore == null)
                throw new InvalidOperationException("No suitable store found for delivery.");

            // Calculate total amount
            var totalAmount = await CalculateTotalAmountAsync(request.Products);

            // Get vendor and store coordinates
            var vendor = await _context.Vendors.FindAsync(request.VendorId);
            double? fromLat = vendor?.Latitude;
            double? fromLon = vendor?.Longitude;
            double? toLat = bestStore.Latitude;
            double? toLon = bestStore.Longitude;

            // Create delivery
            var delivery = new Delivery
            {
                VendorId = request.VendorId,
                StoreId = bestStore.Id,
                TotalAmount = totalAmount,
                Status = DeliveryStatus.Pending,
                FromLatitude = fromLat,
                FromLongitude = fromLon,
                ToLatitude = toLat,
                ToLongitude = toLon
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
            var vendor = await _context.Vendors.FindAsync(vendorId);
            if (vendor == null)
                throw new ArgumentException("Vendor not found");

            var activeStores = await _context.Stores
                .Where(s => s.IsActive)
                .ToListAsync();

            if (!activeStores.Any())
                throw new InvalidOperationException("No active stores available");

            // Завантажити StoreProducts для всіх активних магазинів
            var storeProducts = await _context.Set<StoreProduct>()
                .Where(sp => activeStores.Select(s => s.Id).Contains(sp.StoreId))
                .ToListAsync();

            // Знайти магазини, у яких є всі потрібні продукти у потрібній кількості
            var suitableStores = activeStores.Where(store =>
                products.All(req =>
                    storeProducts.Any(sp =>
                        sp.StoreId == store.Id &&
                        sp.ProductId == req.ProductId &&
                        sp.Quantity >= req.Quantity
                    )
                )
            ).ToList();

            if (!suitableStores.Any())
                throw new InvalidOperationException("No store has all required products in sufficient quantity");

            // Комбінований критерій: відстань - коеф * залишок потрібних товарів
            double stockWeight = 0.01; // налаштуйте під себе
            var bestStore = suitableStores
                .Select(store =>
                {
                    var distance = CalculateDistance(vendor.Latitude, vendor.Longitude, store.Latitude, store.Longitude);
                    var totalStock = products.Sum(req =>
                        storeProducts.First(sp => sp.StoreId == store.Id && sp.ProductId == req.ProductId).Quantity
                    );
                    double score = distance - stockWeight * totalStock;
                    return new { Store = store, Score = score };
                })
                .OrderBy(x => x.Score)
                .First().Store;

            return bestStore;
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

        public async Task<bool> ProcessPaymentAsync(int deliveryId, PaymentDto payment)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null || delivery.Status == DeliveryStatus.Paid)
                return false;

            if (delivery.TotalAmount != payment.Amount)
                throw new InvalidOperationException($"Payment amount mismatch. Expected: {delivery.TotalAmount}, received: {payment.Amount}");

            delivery.Status = DeliveryStatus.Paid;
            delivery.PaymentDate = DateTime.UtcNow;
            delivery.PaymentMethod = payment.PaymentMethod;
            delivery.PaidAmount = payment.Amount;

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
        public async Task<bool> AssignDriverAsync(int deliveryId, AssignDriverDto dto)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
                return false;

            // Assign driver details
            delivery.DriverId = dto.DriverId;
            delivery.GpsTrackerId = dto.GpsTrackerId;
            delivery.Type = dto.DeliveryType;
            delivery.Status = DeliveryStatus.Assigned;
            delivery.AssignedAt = DateTime.UtcNow;

            // Coordinates are already set when delivery was created, no need to update them
            // FromLatitude, FromLongitude, ToLatitude, ToLongitude were set in CreateDeliveryAsync

            await _context.SaveChangesAsync();

            _logger.LogInformation("Driver {DriverId} assigned to delivery {DeliveryId} with GPS tracker {GpsTrackerId}",
                dto.DriverId, deliveryId, dto.GpsTrackerId);

            return true;
        }

        public async Task<bool> UpdateLocationAsync(int deliveryId, LocationUpdateDto locationUpdate)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null)
                return false;

            // Update current location
            delivery.CurrentLatitude = locationUpdate.Latitude;
            delivery.CurrentLongitude = locationUpdate.Longitude;
            delivery.LastLocationUpdate = DateTime.UtcNow;
            delivery.TrackingNotes = locationUpdate.Notes;

            // Add to location history
            var locationHistory = new DeliveryLocationHistory
            {
                DeliveryId = deliveryId,
                Latitude = locationUpdate.Latitude,
                Longitude = locationUpdate.Longitude,
                Speed = locationUpdate.Speed,
                Notes = locationUpdate.Notes,
                Timestamp = DateTime.UtcNow
            };

            _context.DeliveryLocationHistory.Add(locationHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Location updated for delivery {DeliveryId}: {Lat}, {Lon}",
                deliveryId, locationUpdate.Latitude, locationUpdate.Longitude);

            return true;
        }

        public async Task<DeliveryTrackingDto?> GetDeliveryTrackingAsync(int deliveryId)
        {
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery == null)
                return null;

            var locationHistory = await _context.DeliveryLocationHistory
                .Where(h => h.DeliveryId == deliveryId)
                .OrderBy(h => h.Timestamp)
                .Select(h => new LocationHistoryDto
                {
                    Latitude = h.Latitude,
                    Longitude = h.Longitude,
                    Timestamp = h.Timestamp,
                    Notes = h.Notes,
                    Speed = h.Speed
                })
                .ToListAsync();

            return new DeliveryTrackingDto
            {
                DeliveryId = delivery.Id,
                DriverId = delivery.DriverId ?? string.Empty,
                GpsTrackerId = delivery.GpsTrackerId ?? string.Empty,
                Status = delivery.Status,
                CurrentLatitude = delivery.CurrentLatitude,
                CurrentLongitude = delivery.CurrentLongitude,
                LastLocationUpdate = delivery.LastLocationUpdate,
                FromLatitude = delivery.FromLatitude,
                FromLongitude = delivery.FromLongitude,
                ToLatitude = delivery.ToLatitude,
                ToLongitude = delivery.ToLongitude,
                LocationHistory = locationHistory
            };
        }

        public async Task<List<DeliveryTrackingDto>> GetAllActiveTrackingAsync()
        {
            var activeDeliveries = await _context.Deliveries
                .Where(d => d.Status == DeliveryStatus.Assigned ||
                           d.Status == DeliveryStatus.InTransit)
                .ToListAsync();

            var trackingList = new List<DeliveryTrackingDto>();

            foreach (var delivery in activeDeliveries)
            {
                var tracking = await GetDeliveryTrackingAsync(delivery.Id);
                if (tracking != null)
                    trackingList.Add(tracking);
            }

            return trackingList;
        }
    }
}
