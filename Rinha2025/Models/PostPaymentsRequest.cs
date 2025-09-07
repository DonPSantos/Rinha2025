namespace Rinha2025.Models
{
    public class PostPaymentsRequest
    {
        public Guid CorrelationId { get; set; }
        public decimal Amount { get; set; }
    }
}