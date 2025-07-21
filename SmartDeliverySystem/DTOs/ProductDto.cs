using System.ComponentModel.DataAnnotations;

namespace SmartDeliverySystem.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Weight must be greater than 0")]
        public double Weight { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = string.Empty; 
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public int VendorId { get; set; }
    }
}
