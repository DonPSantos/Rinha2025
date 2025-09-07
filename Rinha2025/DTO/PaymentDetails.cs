namespace Rinha2025.DTO
{
    public class PaymentDetails
    {
        public Guid CorrelationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}