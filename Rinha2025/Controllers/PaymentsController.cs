using Microsoft.AspNetCore.Mvc;
using Rinha2025.DTO;
using Rinha2025.Models;
using System.Threading.Channels;

namespace Rinha2025.Controllers
{
    [Route("payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly ChannelWriter<ProcessorRequest> _writer;

        public PaymentsController(ChannelWriter<ProcessorRequest> writer)
        {
            _writer = writer;
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

            await _writer.WriteAsync(processorRequest);

            return Ok("Payments");
        }
    }
}