using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartDeliverySystem.Models
{
    public class DeliveryProduct
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Delivery")]
        public int DeliveryId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        // Navigation properties
        public virtual Delivery Delivery { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
