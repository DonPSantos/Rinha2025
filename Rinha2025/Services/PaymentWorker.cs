using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rinha2025.Clients;
using Rinha2025.DTO;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace Rinha2025.Services
{
    public class PaymentWorker : BackgroundService
    {
        private readonly PaymentProcessorDefaultClient _ppdClient;
        private readonly PaymentProcessorFallbackClient _ppfClient;
        private readonly IDatabase _db;
        private IConnection _connection;
        private IChannel _channel;

        public PaymentWorker(PaymentProcessorDefaultClient ppdClient, PaymentProcessorFallbackClient ppfClient, IDatabase db)
        {
            _ppdClient = ppdClient;
            _ppfClient = ppfClient;
            _db = db;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "host-rabbit",
                UserName = "guest",
                Password = "guest",
                Port = 5672,
                VirtualHost = "/",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
            };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.QueueDeclareAsync(queue: "payment_queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var processorRequest = JsonSerializer.Deserialize<ProcessorRequest>(message);

                Console.WriteLine("Tentando o default");
                var responseDefault = await _ppdClient.PostPayment(processorRequest);
                if (responseDefault.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _db.HashSet("pagamentos-processados-default", [new HashEntry(Guid.NewGuid().ToString(), JsonSerializer.Serialize(processorRequest))]);
                    await _channel.BasicAckAsync(
                                    deliveryTag: ea.DeliveryTag,
                                    multiple: false);
                    Console.WriteLine("Deu certo no default");
                }
                else
                {
                    Console.WriteLine("Deu erro no default, tentando o fallback");
                    var responseFallback = await _ppfClient.PostPayment(processorRequest);
                    if (responseFallback.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _db.HashSet("pagamentos-processados-fallback", [new HashEntry(Guid.NewGuid().ToString(), JsonSerializer.Serialize(processorRequest))]);
                        await _channel.BasicAckAsync(
                                deliveryTag: ea.DeliveryTag,
                                multiple: false);
                        Console.WriteLine("Deu certo no fallback");
                    }
                    else
                    {
                        Console.WriteLine("Deu erro nos 2");
                        await _channel.BasicNackAsync(
                                deliveryTag: ea.DeliveryTag,
                                multiple: false,
                                requeue: true);
                        Console.WriteLine("Reenfileirado");
                    }
                }

                Console.WriteLine(" [x] Received {0}", message);
            };

            await _channel.BasicConsumeAsync(queue: "payment_queue",
                                     autoAck: false,
                                     consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}