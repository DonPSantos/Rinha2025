namespace Rinha2025.DTO
{
    public record ProcessorRequest
    {
        public Guid CorrelationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}