using Microsoft.AspNetCore.Mvc;
using Rinha2025.DTO;
using Rinha2025.Models;
using Rinha2025.Repositories;

namespace Rinha2025.Controllers
{
    [Route("payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IRabbitRepository _repository;

        public PaymentsController(IRabbitRepository repository)
        {
            _repository = repository;
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

            await _repository.CriarMensagem(processorRequest);

            return Ok("Payments");
        }
    }
}