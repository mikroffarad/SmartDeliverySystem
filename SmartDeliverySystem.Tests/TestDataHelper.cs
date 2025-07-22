using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Tests
{
    public static class TestDataHelper
    {
        private static int _vendorCounter = 0;
        private static int _storeCounter = 0;
        private static int _productCounter = 0;
        private static int _deliveryCounter = 0;

        public static Vendor CreateTestVendor(int? id = null, string name = "Test Vendor")
        {
            return new Vendor
            {
                Id = id ?? Interlocked.Increment(ref _vendorCounter),
                Name = $"{name}_{id ?? _vendorCounter}",
                Latitude = 50.4501,
                Longitude = 30.5234
            };
        }

        public static Store CreateTestStore(int? id = null, string name = "Test Store")
        {
            return new Store
            {
                Id = id ?? Interlocked.Increment(ref _storeCounter),
                Name = $"{name}_{id ?? _storeCounter}",
                Latitude = 50.4502,
                Longitude = 30.5235
            };
        }

        public static Product CreateTestProduct(int? id = null, int vendorId = 1, string name = "Test Product")
        {
            return new Product
            {
                Id = id ?? Interlocked.Increment(ref _productCounter),
                Name = $"{name}_{id ?? _productCounter}",
                Category = "Test Category",
                Weight = 1.5m,
                Price = 25.99m,
                VendorId = vendorId
            };
        }

        public static Delivery CreateTestDelivery(int? id = null, int vendorId = 1, int storeId = 1)
        {
            return new Delivery
            {
                Id = id ?? Interlocked.Increment(ref _deliveryCounter),
                VendorId = vendorId,
                StoreId = storeId,
                Status = DeliveryStatus.PendingPayment,
                TotalAmount = 100.00m,
                CreatedAt = DateTime.UtcNow,
                FromLatitude = 50.4501,
                FromLongitude = 30.5234,
                ToLatitude = 50.4502,
                ToLongitude = 30.5235
            };
        }
    }
}
