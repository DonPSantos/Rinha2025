using Rinha2025.DTO;
using System.Text.Json;

namespace Rinha2025.Models
{
    public class GetPaymentsSummaryResult
    {
        public GetPaymentsSummaryResult(List<ProcessorRequest> paymentsDefault, List<ProcessorRequest> paymantsFallback, DateTime from, DateTime to)
        {
            Default = new Totals
            {
                totalRequests = paymentsDefault.Count,
                totalAmount = paymentsDefault.Sum(x => x.Amount)
            };

            Fallback = new Totals
            {
                totalRequests = paymantsFallback.Where(x => x.RequestedAt > from && x.RequestedAt < to).Count(),
                totalAmount = paymantsFallback.Where(x => x.RequestedAt > from && x.RequestedAt < to).Sum(x => x.Amount)
            };
        }

        public Totals Default { get; set; }
        public Totals Fallback { get; set; }
    }
}