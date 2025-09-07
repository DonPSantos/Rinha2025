using Rinha2025.DTO;
using StackExchange.Redis;
using System.Text.Json;

namespace Rinha2025.Models
{
    public class GetPaymentsSummaryResult
    {
        public GetPaymentsSummaryResult(HashEntry[] paymentsDefault, HashEntry[] paymantsFallback, DateTime from, DateTime to)
        {
            var defaultBrute = new List<ProcessorRequest>();
            var fallbackBrute = new List<ProcessorRequest>();

            foreach (var item in paymentsDefault)
            {
                defaultBrute.Add(JsonSerializer.Deserialize<ProcessorRequest>(item.Value));
            }

            foreach (var item in paymantsFallback)
            {
                fallbackBrute.Add(JsonSerializer.Deserialize<ProcessorRequest>(item.Value));
            }

            Default = new Totals
            {
                totalRequests = defaultBrute.Count,
                totalAmount = defaultBrute.Sum(x => x.Amount)
            };

            Fallback = new Totals
            {
                totalRequests = fallbackBrute.Where(x => x.RequestedAt > from && x.RequestedAt < to).Count(),
                totalAmount = fallbackBrute.Where(x => x.RequestedAt > from && x.RequestedAt < to).Sum(x => x.Amount)
            };
        }

        public Totals Default { get; set; }
        public Totals Fallback { get; set; }
    }
}