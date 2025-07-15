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
                Status = DeliveryStatus.PendingPayment, // Статус: очікується оплата
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
                TotalAmount = totalAmount,
            };
        }

        public async Task<DeliveryResponseDto> CreateDeliveryManualAsync(DeliveryRequestManualDto request)
        {
            _logger.LogInformation("Creating manual delivery for vendor {VendorId} to store {StoreId}",
                request.VendorId, request.StoreId);

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
            }

            // Validate that the selected store exists
            var store = await _context.Stores.FindAsync(request.StoreId);
            if (store == null)
                throw new InvalidOperationException($"Store with ID {request.StoreId} not found.");

            // Calculate total amount
            var totalAmount = await CalculateTotalAmountAsync(request.Products);

            // Get vendor and store coordinates
            var vendor = await _context.Vendors.FindAsync(request.VendorId);
            double? fromLat = vendor?.Latitude;
            double? fromLon = vendor?.Longitude;
            double? toLat = store.Latitude;
            double? toLon = store.Longitude;            // Create delivery
            var delivery = new Delivery
            {
                VendorId = request.VendorId,
                StoreId = store.Id,
                TotalAmount = totalAmount,
                Status = DeliveryStatus.PendingPayment,
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
            await _context.SaveChangesAsync(); return new DeliveryResponseDto
            {
                DeliveryId = delivery.Id,
                StoreId = store.Id,
                StoreName = store.Name,
                TotalAmount = totalAmount,
            };
        }
        public async Task<Store> FindBestStoreAsync(int vendorId, List<ProductRequestDto> products)
        {
            var vendor = await _context.Vendors.FindAsync(vendorId);
            if (vendor == null)
                throw new ArgumentException("Vendor not found");

            var activeStores = await _context.Stores
                .ToListAsync();

            if (!activeStores.Any())
                throw new InvalidOperationException("No active stores available");

            // Завантажити StoreProducts для всіх активних магазинів
            var storeProducts = await _context.Set<StoreProduct>()
                .Where(sp => activeStores.Select(s => s.Id).Contains(sp.StoreId))
                .ToListAsync();

            // Детальна діагностика
            _logger.LogInformation("Finding best store among {StoreCount} stores for {ProductCount} products",
                activeStores.Count, products.Count);

            foreach (var product in products)
            {
                _logger.LogInformation("Product to deliver: {ProductId}, quantity: {Quantity}",
                    product.ProductId, product.Quantity);
            }

            // Критерії вибору найкращого магазину:
            // 1. Відстань від вендора до магазину (менша відстань = кращий бал)
            // 2. Загальна кількість товарів у магазині (менша кількість = кращий бал, навіть 0)
            // Формула: Score = (Distance * DistanceWeight) + (TotalInventory * InventoryWeight)
            // Магазин з найменшим балом вибирається

            double distanceWeight = 1.0;     // Вага для відстані (км)
            double inventoryWeight = 0.001;  // Вага для інвентарю (менша кількість = кращий бал)

            var bestStore = activeStores
                .Select(store =>
                {
                    // Розрахунок відстані від вендора до магазину
                    var distance = CalculateDistance(vendor.Latitude, vendor.Longitude, store.Latitude, store.Longitude);

                    // Загальна кількість усіх товарів у магазині (може бути 0)
                    var totalInventory = storeProducts
                        .Where(sp => sp.StoreId == store.Id)
                        .Sum(sp => sp.Quantity);

                    // Комбінований бал: відстань + (кількість товарів * вага)
                    // Менша кількість товарів = кращий бал (навіть якщо 0)
                    double score = (distance * distanceWeight) + (totalInventory * inventoryWeight);

                    _logger.LogInformation("Store {StoreId} ({StoreName}): Distance={Distance:F2}km, Inventory={Inventory}, Score={Score:F3}",
                        store.Id, store.Name, distance, totalInventory, score);

                    return new
                    {
                        Store = store,
                        Score = score,
                        Distance = distance,
                        TotalInventory = totalInventory
                    };
                })
                .OrderBy(x => x.Score)
                .First();

            _logger.LogInformation("✅ Selected store: {StoreName}, Distance: {Distance:F2}km, " +
                                 "Total inventory: {Inventory} items, Score: {Score:F3}",
                bestStore.Store.Name, bestStore.Distance, bestStore.TotalInventory, bestStore.Score);

            return bestStore.Store;
        }
        public async Task<FindBestStoreResponseDto> FindBestStoreForDeliveryAsync(int vendorId, List<ProductRequestDto> products)
        {
            _logger.LogInformation("Finding best store for vendor {VendorId}", vendorId);

            // Use existing logic from FindBestStoreAsync
            var bestStore = await FindBestStoreAsync(vendorId, products);
            var vendor = await _context.Vendors.FindAsync(vendorId);
            if (vendor == null)
                throw new InvalidOperationException("Vendor not found.");

            var distance = CalculateDistance(vendor.Latitude, vendor.Longitude, bestStore.Latitude, bestStore.Longitude);

            return new FindBestStoreResponseDto
            {
                StoreId = bestStore.Id,
                StoreName = bestStore.Name,
                Distance = distance
            };
        }
        public async Task<Delivery?> GetDeliveryAsync(int deliveryId)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Vendor)
                .Include(d => d.Store)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            return delivery;
        }
        public async Task<List<Delivery>> GetActiveDeliveriesAsync()
        {
            return await _context.Deliveries
                .Include(d => d.Vendor)
                .Include(d => d.Store)
                .Where(d => d.Status == DeliveryStatus.InTransit || d.Status == DeliveryStatus.Assigned) // Включаємо Assigned
                .ToListAsync();
        }

        public async Task<List<Delivery>> GetAllDeliveriesAsync()
        {
            return await _context.Deliveries
                .Include(d => d.Vendor)
                .Include(d => d.Store)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateDeliveryStatusAsync(int deliveryId, DeliveryStatus status)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Products)
                .ThenInclude(dp => dp.Product)
                .FirstOrDefaultAsync(d => d.Id == deliveryId); if (delivery == null)
            {
                _logger.LogWarning("Delivery {DeliveryId} not found when trying to update status", deliveryId);
                return false;
            }

            var oldStatus = delivery.Status;
            delivery.Status = status;

            _logger.LogInformation("Updating delivery {DeliveryId} status from {OldStatus} to {NewStatus}",
                deliveryId, oldStatus, status);

            // If delivery is marked as delivered, add products to store inventory
            if (status == DeliveryStatus.Delivered && oldStatus != DeliveryStatus.Delivered)
            {
                delivery.DeliveredAt = DateTime.UtcNow;

                // Save delivery status first
                await _context.SaveChangesAsync();

                // Add products to inventory
                await AddProductsToStoreInventoryAsync(delivery);

                _logger.LogInformation("Delivery {DeliveryId} completed and products added to store {StoreId} inventory",
                    deliveryId, delivery.StoreId);
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }
        private async Task AddProductsToStoreInventoryAsync(Delivery delivery)
        {
            _logger.LogInformation("Starting inventory update for delivery {DeliveryId} to store {StoreId}",
                delivery.Id, delivery.StoreId);

            if (delivery.Products == null || !delivery.Products.Any())
            {
                _logger.LogWarning("Delivery {DeliveryId} has no products to add to inventory", delivery.Id);

                // Try to load products directly from database
                var dbProducts = await _context.DeliveryProducts
                    .Include(dp => dp.Product)
                    .Where(dp => dp.DeliveryId == delivery.Id)
                    .ToListAsync();

                if (dbProducts.Any())
                {
                    _logger.LogInformation("Found {ProductCount} products in database for delivery {DeliveryId}",
                        dbProducts.Count, delivery.Id);

                    // Process products from database
                    foreach (var deliveryProduct in dbProducts)
                    {
                        await ProcessProductToInventory(delivery.StoreId, deliveryProduct);
                    }

                    var changesCount = await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved {ChangesCount} inventory changes for delivery {DeliveryId}",
                        changesCount, delivery.Id);
                }
                return;
            }

            foreach (var deliveryProduct in delivery.Products)
            {
                await ProcessProductToInventory(delivery.StoreId, deliveryProduct);
            }

            // Save changes after updating inventory
            try
            {
                var changesCount = await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved {ChangesCount} inventory changes for delivery {DeliveryId}",
                    changesCount, delivery.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save inventory changes for delivery {DeliveryId}", delivery.Id);
                throw;
            }
        }

        private async Task ProcessProductToInventory(int storeId, DeliveryProduct deliveryProduct)
        {
            var existingInventory = await _context.StoreProducts
                .FirstOrDefaultAsync(sp => sp.StoreId == storeId && sp.ProductId == deliveryProduct.ProductId);

            if (existingInventory != null)
            {
                existingInventory.Quantity += deliveryProduct.Quantity;
                _logger.LogInformation("Updated inventory for product {ProductId}: +{Quantity}",
                    deliveryProduct.ProductId, deliveryProduct.Quantity);
            }
            else
            {
                var newInventory = new StoreProduct
                {
                    StoreId = storeId,
                    ProductId = deliveryProduct.ProductId,
                    Quantity = deliveryProduct.Quantity
                };
                _context.StoreProducts.Add(newInventory);
                _logger.LogInformation("Added new product {ProductId} to store inventory: {Quantity} units",
                    deliveryProduct.ProductId, deliveryProduct.Quantity);
            }
        }

        private async Task RemoveProductsFromStoreInventoryAsync(Delivery delivery)
        {
            _logger.LogInformation("Removing products from store inventory for delivery {DeliveryId}", delivery.Id);

            // Get delivery products from database if not loaded
            if (delivery.Products == null || !delivery.Products.Any())
            {
                delivery.Products = await _context.DeliveryProducts
                    .Include(dp => dp.Product)
                    .Where(dp => dp.DeliveryId == delivery.Id)
                    .ToListAsync();
            }

            foreach (var deliveryProduct in delivery.Products)
            {
                var storeProduct = await _context.StoreProducts
                    .FirstOrDefaultAsync(sp => sp.StoreId == delivery.StoreId && sp.ProductId == deliveryProduct.ProductId);

                if (storeProduct != null)
                {
                    // Decrease quantity
                    storeProduct.Quantity -= deliveryProduct.Quantity;

                    // If quantity becomes 0 or negative, remove the record
                    if (storeProduct.Quantity <= 0)
                    {
                        _context.StoreProducts.Remove(storeProduct);
                        _logger.LogInformation("Removed product {ProductId} from store {StoreId} inventory (quantity was {Quantity})",
                            deliveryProduct.ProductId, delivery.StoreId, storeProduct.Quantity + deliveryProduct.Quantity);
                    }
                    else
                    {
                        _logger.LogInformation("Decreased product {ProductId} quantity in store {StoreId} by {Quantity}",
                            deliveryProduct.ProductId, delivery.StoreId, deliveryProduct.Quantity);
                    }
                }
                else
                {
                    _logger.LogWarning("Product {ProductId} not found in store {StoreId} inventory when trying to remove",
                        deliveryProduct.ProductId, delivery.StoreId);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully removed products from store inventory for delivery {DeliveryId}", delivery.Id);
        }

        public async Task<bool> ProcessPaymentAsync(int deliveryId, PaymentDto payment)
        {
            var delivery = await _context.Deliveries.FindAsync(deliveryId);
            if (delivery == null || delivery.Status == DeliveryStatus.Paid)
                return false;

            // Remove strict amount check - allow partial payments or overpayments
            delivery.Status = DeliveryStatus.Paid;
            delivery.PaymentDate = DateTime.UtcNow;
            delivery.PaymentMethod = payment.PaymentMethod;
            delivery.PaidAmount = payment.Amount; await _context.SaveChangesAsync();
            _logger.LogInformation("Payment processed for delivery {DeliveryId}: ${PaidAmount} (Expected: ${TotalAmount}) via {PaymentMethod}",
                deliveryId, payment.Amount, delivery.TotalAmount, payment.PaymentMethod);
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
            if (delivery.Status != DeliveryStatus.Paid)
                throw new InvalidOperationException("Delivery must be paid before assigning driver");            // Assign driver details
            delivery.DriverId = dto.DriverId;
            delivery.GpsTrackerId = dto.GpsTrackerId;
            delivery.Status = DeliveryStatus.InTransit; // Статус: в дорозі після призначення водія
            delivery.AssignedAt = DateTime.UtcNow;
            // Встановлюємо початкові координати GPS-трекера на координати вендора
            delivery.CurrentLatitude = delivery.FromLatitude;
            delivery.CurrentLongitude = delivery.FromLongitude;
            delivery.LastLocationUpdate = DateTime.UtcNow;
            // Додаємо запис у історію переміщень
            var locationHistory = new DeliveryLocationHistory
            {
                DeliveryId = deliveryId,
                Latitude = delivery.FromLatitude ?? 0,
                Longitude = delivery.FromLongitude ?? 0,
                Timestamp = DateTime.UtcNow,
                Notes = "Початок доставки (від вендора)",
                Speed = 0
            };
            _context.DeliveryLocationHistory.Add(locationHistory);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Driver {DriverId} assigned to delivery {DeliveryId} with GPS tracker {GpsTrackerId}",
                dto.DriverId, deliveryId, dto.GpsTrackerId);
            return true;
        }
        public async Task<bool> UpdateLocationAsync(int deliveryId, LocationUpdateDto locationUpdate)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Products)
                .ThenInclude(dp => dp.Product)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery == null)
                return false;            // Update current location
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

            // Check if delivery reached destination and auto-complete
            if (delivery.ToLatitude.HasValue && delivery.ToLongitude.HasValue &&
                delivery.Status == DeliveryStatus.InTransit)
            {
                var latDiff = Math.Abs(delivery.ToLatitude.Value - locationUpdate.Latitude);
                var lngDiff = Math.Abs(delivery.ToLongitude.Value - locationUpdate.Longitude);

                if (latDiff < 0.0005 && lngDiff < 0.0005)
                {
                    _logger.LogInformation("Delivery {DeliveryId} reached destination store. Marking as delivered.", deliveryId);

                    // Save current location first
                    await _context.SaveChangesAsync();

                    // Mark as delivered (this will trigger inventory update)
                    var statusUpdated = await UpdateDeliveryStatusAsync(deliveryId, DeliveryStatus.Delivered);
                    if (statusUpdated)
                    {
                        _logger.LogInformation("Delivery {DeliveryId} automatically marked as delivered upon arrival", deliveryId);
                    }
                    return true;
                }
            }

            await _context.SaveChangesAsync();
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
                .ToListAsync(); return new DeliveryTrackingDto
                {
                    DeliveryId = delivery.Id,
                    VendorId = delivery.VendorId, // Додаємо ID вендора
                    StoreId = delivery.StoreId, // Додаємо ID магазину
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
                    VendorLatitude = delivery.FromLatitude, // Додаємо координати вендора
                    VendorLongitude = delivery.FromLongitude, // Додаємо координати вендора
                    StoreLatitude = delivery.ToLatitude, // Додаємо координати магазину
                    StoreLongitude = delivery.ToLongitude, // Додаємо координати магазину
                    LocationHistory = locationHistory
                };
        }
        public async Task<List<DeliveryTrackingDto>> GetAllActiveTrackingAsync()
        {
            var activeDeliveries = await _context.Deliveries
                .Where(d => d.Status == DeliveryStatus.Assigned ||
                           d.Status == DeliveryStatus.InTransit ||
                           d.Status == DeliveryStatus.Paid) // Include Paid status
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
        public async Task<List<object>> GetDeliveryProductsAsync(int deliveryId)
        {
            try
            {
                var delivery = await _context.Deliveries
                    .Include(d => d.Products)
                    .ThenInclude(dp => dp.Product)
                    .FirstOrDefaultAsync(d => d.Id == deliveryId);

                if (delivery == null)
                {
                    _logger.LogWarning("Delivery {DeliveryId} not found", deliveryId);
                    return new List<object>();
                }

                if (delivery.Products == null || !delivery.Products.Any())
                {
                    _logger.LogWarning("No products found for delivery {DeliveryId}", deliveryId);
                    return new List<object>();
                }

                return delivery.Products.Select(dp => new
                {
                    id = dp.Product.Id,
                    name = dp.Product.Name,
                    category = dp.Product.Category,
                    weight = dp.Product.Weight,
                    price = dp.Product.Price,
                    quantity = dp.Quantity
                }).ToList<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for delivery {DeliveryId}", deliveryId);
                return new List<object>();
            }
        }
        public async Task<bool> DeleteDeliveryAsync(int deliveryId)
        {
            try
            {
                var delivery = await _context.Deliveries
                    .Include(d => d.Products)
                    .Include(d => d.LocationHistory)
                    .FirstOrDefaultAsync(d => d.Id == deliveryId);

                if (delivery == null)
                {
                    _logger.LogWarning("Delivery {DeliveryId} not found for deletion", deliveryId);
                    return false;
                }

                // Check if delivery is in transit - don't allow deletion
                if (delivery.Status == DeliveryStatus.InTransit)
                {
                    _logger.LogWarning("Cannot delete delivery {DeliveryId} that is in transit", deliveryId);
                    return false;
                }

                _logger.LogInformation("Deleting delivery {DeliveryId} with status {Status}", deliveryId, delivery.Status);

                // If delivery was completed, remove products from store inventory
                if (delivery.Status == DeliveryStatus.Delivered)
                {
                    await RemoveProductsFromStoreInventoryAsync(delivery);
                }

                // Remove from context - EF will handle cascade deletion for related entities
                _context.Deliveries.Remove(delivery);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted delivery {DeliveryId}", deliveryId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting delivery {DeliveryId}", deliveryId);
                return false;
            }
        }
    }
}
