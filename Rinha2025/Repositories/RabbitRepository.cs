using RabbitMQ.Client;
using Rinha2025.DTO;
using System.Text;
using System.Text.Json;

namespace Rinha2025.Repositories
{
    public class RabbitRepository : IRabbitRepository
    {
        public async Task CriarMensagem(ProcessorRequest processorRequest)
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

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: "payment_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

            var json = JsonSerializer.Serialize(processorRequest);

            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "payment_queue", body);
        }
    }
}