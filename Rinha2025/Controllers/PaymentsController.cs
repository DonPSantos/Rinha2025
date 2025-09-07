using Microsoft.AspNetCore.Mvc;
using Rinha2025.Clients;
using Rinha2025.DTO;
using Rinha2025.Models;
using StackExchange.Redis;

namespace Rinha2025.Controllers
{
    [Route("payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentProcessorDefaultClient _ppdClient;
        private readonly PaymentProcessorFallbackClient _ppfClient;
        private readonly IConnectionMultiplexer _redis;
        private const string StreamName = "fila-pagamentos";

        public PaymentsController(PaymentProcessorDefaultClient ppdClient, PaymentProcessorFallbackClient ppfClient, IConnectionMultiplexer redis)
        {
            _ppdClient = ppdClient;
            _ppfClient = ppfClient;
            _redis = redis;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PostPaymentsRequest request)
        {
            var processorRequest = new ProcessorRequest
            {
                Amount = request.Amount,
                CorrelationId = request.CorrelationId,
                RequestedAt = DateTime.UtcNow
            };
            var db = _redis.GetDatabase();
            var id = await db.StreamAddAsync(StreamName, new NameValueEntry[]
            {
                new NameValueEntry("CorrelationId", processorRequest.CorrelationId.ToString()),
                new NameValueEntry("Amount", processorRequest.Amount.ToString()),
                new NameValueEntry("RequestedAt", processorRequest.RequestedAt.ToString("o"))
            });

            return Ok("Payments");
        }
    }
}