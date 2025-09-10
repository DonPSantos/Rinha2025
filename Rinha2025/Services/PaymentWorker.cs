using Rinha2025.Clients;
using Rinha2025.DTO;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Channels;

namespace Rinha2025.Services
{
    public class PaymentWorker : BackgroundService
    {
        private readonly PaymentProcessorDefaultClient _ppdClient;
        private readonly PaymentProcessorFallbackClient _ppfClient;
        private readonly ChannelReader<ProcessorRequest> _reader;
        private readonly ChannelWriter<ProcessorRequest> _writer;
        private readonly IDatabase _db;

        public PaymentWorker(PaymentProcessorDefaultClient ppdClient, PaymentProcessorFallbackClient ppfClient, ChannelReader<ProcessorRequest> reader, ChannelWriter<ProcessorRequest> writer, IDatabase db)
        {
            _ppdClient = ppdClient;
            _ppfClient = ppfClient;
            _reader = reader;
            _writer = writer;
            _db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ProcessorRequest request;

                if (_reader.TryRead(out request))
                {
                    var resultDefault = await _ppdClient.PostPayment(request);

                    if (resultDefault.IsSuccessStatusCode)
                    {
                        _db.HashSet("pagamentos-processados-default", [new HashEntry(Guid.NewGuid().ToString(), JsonSerializer.Serialize(request))]);
                    }
                    else
                    {
                        var resultFallback = await _ppfClient.PostPayment(request);
                        if (resultFallback.IsSuccessStatusCode)
                        {
                            _db.HashSet("pagamentos-processados-fallback", [new HashEntry(Guid.NewGuid().ToString(), JsonSerializer.Serialize(request))]);
                        }
                        else
                        {
                            _writer.TryWrite(request);
                        }
                    }
                }
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}