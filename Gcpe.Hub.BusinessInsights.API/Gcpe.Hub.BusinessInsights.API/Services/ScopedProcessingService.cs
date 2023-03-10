using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class ScopedProcessingService : IScopedProcessingService
    {
        private readonly IDataSynchronizationService _dataSynchronizationService;
        private readonly ILogger _logger;

        public ScopedProcessingService(
            IDataSynchronizationService dataSynchronizationService,
            ILogger logger)
        {
            _dataSynchronizationService = dataSynchronizationService ?? throw new ArgumentNullException(nameof(dataSynchronizationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting data synchronization service, running every 15 minutes...");
                    await _dataSynchronizationService.SyncData();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.InnerException.Message);
                }

                await Task.Delay(900000, stoppingToken); // 21_600 for 20 seconds
            }
        }
    }
}
