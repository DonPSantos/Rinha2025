using Microsoft.AspNetCore.Mvc;
using Rinha2025.DTO;
using Rinha2025.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Rinha2025.Controllers
{
    [Route("payments-summary")]
    public class PaymentsSummaryController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;

        public PaymentsSummaryController(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetPaymentsSummaryRequest request)
        {
            var db = _redis.GetDatabase();

            var dlist = await db.HashGetAllAsync("pagamentos-processados-default");
            var flist = await db.HashGetAllAsync("pagamentos-processados-fallback");

            return Ok(new GetPaymentsSummaryResult(dlist, flist));
        }
    }
}