using Rinha2025.DTO;

namespace Rinha2025.Clients
{
    public class PaymentProcessorFallbackClient : IPaymentProcessor
    {
        private readonly HttpClient _http;

        public PaymentProcessorFallbackClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<HealthCheckResult> GetHealthCheckResult()
        {
            var url = "payments/service-health";
            return await _http.GetFromJsonAsync<HealthCheckResult>(url);
        }

        public async Task<HttpResponseMessage> PostPayment(ProcessorRequest request)
        {
            var url = "payments";
            return await _http.PostAsJsonAsync(url, request);
        }
    }
}