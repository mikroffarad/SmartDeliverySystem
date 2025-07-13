namespace SmartDeliverySystem.DTOs
{
    public class VendorWithProductsDto : VendorDto
    {
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();
    }
}
