using System.Threading.Tasks;
using System.Threading;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IScopedProcessingService
    {
        Task DoWork(CancellationToken stoppingToken);
    }
}
