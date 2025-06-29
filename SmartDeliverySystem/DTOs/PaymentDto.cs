namespace SmartDeliverySystem.DTOs {
    public class PaymentDto {
        public decimal Amount {get ; set; }
        public string PaymentMethod { get; set ;} = string.Empty;
    }
}
