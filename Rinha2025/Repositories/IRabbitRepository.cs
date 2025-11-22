using Rinha2025.DTO;

namespace Rinha2025.Repositories
{
    public interface IRabbitRepository
    {
        Task CriarMensagem(ProcessorRequest processorRequest);
    }
}