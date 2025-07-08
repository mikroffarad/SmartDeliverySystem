using SmartDeliverySystem.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartDeliverySystem.DTOs
{
    public class DeliveryRequestDto
    {
        public int VendorId { get; set; }
        public List<ProductRequestDto> Products { get; set; } = new();
    }

    public class ProductRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class DeliveryRequestManualDto
    {
        [Required]
        public int VendorId { get; set; }

        [Required]
        public int StoreId { get; set; }

        [Required]
        public List<ProductRequestDto> Products { get; set; } = new();
    }
}
