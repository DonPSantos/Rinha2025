using Rinha2025.DTO;
using Rinha2025.Models;

namespace Rinha2025.Clients
{
    public interface IPaymentProcessor
    {
        Task<HealthCheckResult> GetHealthCheckResult();

        Task<HttpResponseMessage> PostPayment(ProcessorRequest request);
    }
}