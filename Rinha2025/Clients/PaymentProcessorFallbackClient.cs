using Polly;
using Polly.Registry;
using Rinha2025.DTO;

namespace Rinha2025.Clients
{
    public class PaymentProcessorFallbackClient : IPaymentProcessor
    {
        private readonly HttpClient _http;
        private readonly ResiliencePipeline _pipeline;

        public PaymentProcessorFallbackClient(HttpClient http, ResiliencePipelineProvider<string> pipelineProvider)
        {
            _http = http;
            _pipeline = pipelineProvider.GetPipeline("circuit-pipeline");
        }

        public async Task<HealthCheckResult> GetHealthCheckResult()
        {
            var url = "payments/service-health";
            return await _http.GetFromJsonAsync<HealthCheckResult>(url);
        }

        public async Task<HttpResponseMessage> PostPayment(ProcessorRequest request)
        {
            var url = "payments";

            //return await _pipeline.ExecuteAsync(async token => await _http.PostAsJsonAsync(url, request));
            return await _http.PostAsJsonAsync(url, request);
        }
    }
}