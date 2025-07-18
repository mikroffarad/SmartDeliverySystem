using SmartDeliverySystem.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartDeliverySystem.DTOs
{
    public class DeliveryRequestDto
    {
        [Required]
        public int VendorId { get; set; }

        [Required]
        public int StoreId { get; set; }

        [Required]
        public List<ProductRequestDto> Products { get; set; } = new();
    }

    public class ProductRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class RouteIndexDto
    {
        public int Index { get; set; }
    }
}
