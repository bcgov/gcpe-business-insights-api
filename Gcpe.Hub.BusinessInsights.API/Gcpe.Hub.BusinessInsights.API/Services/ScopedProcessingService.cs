using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScrapySharp.Extensions;
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
                    await _dataSynchronizationService.SyncData();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.InnerException.Message);
                }

                await Task.Delay(21_600, stoppingToken);
            }
        }
    }
}
