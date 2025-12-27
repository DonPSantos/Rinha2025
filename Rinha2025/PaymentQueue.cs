using Rinha2025.DTO;
using System.Threading.Channels;

namespace Rinha2025
{
    /// <summary>
    /// Objeto que representa a fila de pagamentos a serem processados.
    /// </summary>
    public class PaymentQueue
    {
        /// <summary>
        /// propriedade interna que controla a fila em memoria
        /// </summary>
        private readonly Channel<ProcessorRequest> _channel = Channel.CreateUnbounded<ProcessorRequest>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        /// <summary>
        /// Permite adicionar itens na fila
        /// </summary>
        public ValueTask Enqueue(ProcessorRequest processorRequest) => _channel.Writer.WriteAsync(processorRequest);

        /// <summary>
        /// Permite ler itens da fila
        /// </summary>
        public ChannelReader<ProcessorRequest> Reader => _channel.Reader;
    }
}