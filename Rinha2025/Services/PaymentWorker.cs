using Rinha2025.Clients;
using Rinha2025.DTO;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rinha2025.Services
{
    public class PaymentWorker : BackgroundService
    {
        private readonly PaymentProcessorDefaultClient _ppdClient;
        private readonly PaymentProcessorFallbackClient _ppfClient;
        private readonly IConnectionMultiplexer _redis;
        private const string StreamName = "fila-pagamentos";
        private const string ConsumerGroupName = "processadores";
        private readonly string ConsumerName = Environment.GetEnvironmentVariable("WORKER_NAME");

        public PaymentWorker(PaymentProcessorDefaultClient ppdClient, PaymentProcessorFallbackClient ppfClient, IConnectionMultiplexer redis)
        {
            _ppdClient = ppdClient;
            _ppfClient = ppfClient;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    var db = _redis.GetDatabase();

                    StreamEntry[] mensagens;

                    mensagens = await db.StreamReadGroupAsync(
                        StreamName,
                        ConsumerGroupName,
                        ConsumerName,
                        StreamPosition.Beginning,
                        count: 1
                    );

                    if (mensagens.Length == 0)
                    {
                        mensagens = await db.StreamReadGroupAsync(
                            StreamName,
                            ConsumerGroupName,
                            ConsumerName,
                            StreamPosition.NewMessages,
                            count: 1
                        );
                    }

                    if (mensagens.Length > 0)
                    {
                        var request = ConverterStreamEntry(mensagens[0]);

                        var resultDefault = await _ppdClient.PostPayment(request);

                        if (resultDefault.IsSuccessStatusCode)
                        {
                            db.HashSet("pagamentos-processados-default", [new HashEntry(Guid.NewGuid().ToString(), JsonSerializer.Serialize(request))]);
                            await db.StreamAcknowledgeAsync(StreamName, ConsumerGroupName, mensagens[0].Id);
                        }
                        else
                        {
                            var resultFallback = await _ppfClient.PostPayment(request);
                            if (resultFallback.IsSuccessStatusCode)
                            {
                                db.HashSet("pagamentos-processados-fallback", [new HashEntry(Guid.NewGuid().ToString(), JsonSerializer.Serialize(request))]);
                                await db.StreamAcknowledgeAsync(StreamName, ConsumerGroupName, mensagens[0].Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar pagamento: {ex.Message}");
                }
            }
        }

        private ProcessorRequest ConverterStreamEntry(StreamEntry streamEntry)
        {
            return new ProcessorRequest
            {
                CorrelationId = Guid.Parse(streamEntry["CorrelationId"]),
                Amount = decimal.Parse(streamEntry["Amount"]),
                RequestedAt = DateTime.Parse(streamEntry["RequestedAt"])
            };
        }
    }
}