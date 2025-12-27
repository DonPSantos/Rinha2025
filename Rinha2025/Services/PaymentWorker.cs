using Microsoft.Data.Sqlite;
using Rinha2025.Clients;

namespace Rinha2025.Services
{
    public class PaymentWorker : BackgroundService
    {
        private readonly PaymentProcessorDefaultClient _ppdClient;
        private readonly PaymentProcessorFallbackClient _ppfClient;
        private readonly PaymentQueue _queue;
        private readonly PaymentRepository _paymentRepository;

        public PaymentWorker(PaymentProcessorDefaultClient ppdClient, PaymentProcessorFallbackClient ppfClient, PaymentQueue queue, PaymentRepository paymentRepository)
        {
            _ppdClient = ppdClient;
            _ppfClient = ppfClient;
            _queue = queue;
            _paymentRepository = paymentRepository;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            using var connection = new SqliteConnection($"Data Source={Environment.GetEnvironmentVariable("DB_PATH")}");

            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText = """
                CREATE TABLE paymentDefault (
                    CorrelationDd TEXT NOT NULL PRIMARY KEY,
                    Amount REAL NOT NULL,
                    RequestedAt TEXT NOT NULL
                );
            """;
            command.ExecuteNonQuery();

            command.CommandText = """
                CREATE TABLE paymentFallback (
                    CorrelationDd TEXT NOT NULL PRIMARY KEY,
                    Amount REAL NOT NULL,
                    RequestedAt TEXT NOT NULL
                );
            """;
            command.ExecuteNonQuery();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _queue.Reader;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await reader.WaitToReadAsync(stoppingToken))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                else
                {
                    reader.TryRead(out var processorRequest);
                    var responseDefault = await _ppdClient.PostPayment(processorRequest);
                    if (responseDefault.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        await _paymentRepository.SaveDefault(processorRequest);
                        Console.WriteLine("Deu certo no default");
                    }
                    else
                    {
                        Console.WriteLine("Deu erro no default, tentando o fallback");
                        var responseFallback = await _ppfClient.PostPayment(processorRequest);
                        if (responseFallback.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            await _paymentRepository.SaveFallback(processorRequest);
                            Console.WriteLine("Deu certo no fallback");
                        }
                        else
                        {
                            Console.WriteLine("Deu erro nos 2");
                            await _queue.Enqueue(processorRequest);
                            Console.WriteLine("Reenfileirado");
                        }
                    }
                }
            }
        }
    }
}