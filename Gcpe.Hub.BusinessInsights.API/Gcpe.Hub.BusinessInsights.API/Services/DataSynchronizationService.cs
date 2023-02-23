using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Azure;
using Gcpe.Hub.BusinessInsights.API.Entities;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class DataSynchronizationService : IDataSynchronizationService
    {
        private readonly IHubBusinessInsightsRepository _hubBusinessInsightsRepository;
        private readonly ILocalDataRepository _localDataRepository;
        private readonly ILogger<DataSynchronizationService> _logger;
        private readonly IConfiguration _config;
        public DataSynchronizationService(
            IHubBusinessInsightsRepository hubBusinessInsightsRepository,
            ILocalDataRepository localDataRepository,
            ILogger<DataSynchronizationService> logger,
            IConfiguration config)
        {
            _hubBusinessInsightsRepository = hubBusinessInsightsRepository ?? throw new ArgumentNullException(nameof(hubBusinessInsightsRepository));
            _localDataRepository = localDataRepository;
            _config = config;
            _logger = logger;
        }

        public async Task SyncData()
        {
            var timer = new Stopwatch();
            _logger.LogInformation("Starting data sync...");
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesAsync();
            var localNewsReleaseEntities = await _localDataRepository.GetNewsReleasesForPreviousMonthAsync();
            var isLocalCacheUpToDate = newsReleaseEntities.Count() == localNewsReleaseEntities.Count();

            if (isLocalCacheUpToDate) _logger.LogInformation("NR cache for previous month is up-to-date, skipping sync...");

            if (!isLocalCacheUpToDate)
            {
                // clean up incomplete results and re-fetch
                _logger.LogInformation("NR cache is out-dated for previous month, removing old entries and fetching latest NRs...");
                if (localNewsReleaseEntities.Any())
                {
                    var localNRs = await _localDataRepository.GetNewsReleasesForPreviousMonthAsync();
                    _localDataRepository.DeleteNewsReleasesAsync(localNRs);
                }

                _logger.LogInformation("Adding latest NRs for previous month to cache...");
                foreach (var entity in newsReleaseEntities)
                {
                    var newItem = _localDataRepository.AddNewsReleaseEntity(entity);
                    var urls = await GetBlobs(entity.Key, entity.ReleaseType, entity.PublishDateTime);
                    foreach (var url in urls)
                    {
                        var u = new Url { Href = $"{_config["AzureTranslationsUrl"]}{url}", PublishDateTime = entity.PublishDateTime };
                        newItem.Urls.Add(u);
                    }
                }
                await _localDataRepository.SaveChangesAsync();
                _logger.LogInformation($"Completed sync between cache and news.gov.bc.ca in: {timer.ElapsedMilliseconds} milliseconds.");
            }
        }

        public async Task SyncData(string startDate = "", string endDate = "")
        {
            var timer = new Stopwatch();
            _logger.LogInformation("Starting data sync...");
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesAsync(startDate, endDate);
            var localNewsReleaseEntities = await _localDataRepository.GetNewsReleaseItemsInDateRangeAsync(startDate, endDate);
            var isLocalCacheUpToDate = newsReleaseEntities.Count() == localNewsReleaseEntities.Count();

            if (isLocalCacheUpToDate) _logger.LogInformation("NR cache for date range is up-to-date, skipping custom sync...");

            if(!isLocalCacheUpToDate)
            {
                // clean up incomplete results and re-fetch
                _logger.LogInformation("NR cache is out-dated for custom date range, removing old entries and fetching latest NRs...");
                if (localNewsReleaseEntities.Any())
                {
                    var localNRs = await _localDataRepository.GetNewsReleaseItemsInDateRangeAsync(startDate, endDate);
                    _localDataRepository.DeleteNewsReleasesAsync(localNRs);
                }

                _logger.LogInformation("Adding latest NRs for custom date range to cache...");
                foreach (var entity in newsReleaseEntities)
                {
                    var newItem = _localDataRepository.AddNewsReleaseEntity(entity);
                    var urls = await GetBlobs(entity.Key, entity.ReleaseType, entity.PublishDateTime);
                    foreach (var url in urls)
                    {
                        var u = new Url { Href = $"{_config["AzureTranslationsUrl"]}{url}", PublishDateTime = entity.PublishDateTime };
                        newItem.Urls.Add(u);
                    }
                }
                await _localDataRepository.SaveChangesAsync();
                _logger.LogInformation($"Completed sync between cache and news.gov.bc.ca in: {timer.ElapsedMilliseconds} milliseconds.");
            }

        }

        private async Task<List<string>> GetBlobs(string releaseId, int releaseType, DateTimeOffset publishDateTime)
        {
            var blobNames = new List<string>();
            try
            {

                BlobServiceClient blobServiceClient = new BlobServiceClient(_config["CloudAccountConnectionString"]);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("translations");
                var parentDir = "releases";
                if (releaseType == 3) parentDir = "factsheets";
                AsyncPageable<BlobItem> blobs = containerClient.GetBlobsAsync(prefix: $"{parentDir}/{releaseId}");

                await foreach (var blob in blobs)
                {
                    // publish and modified aren't in alignment, need to revisit later
                    // var isModifiedValid = blob.Properties.LastModified.Value.Month == publishDateTime.Month;
                    // if(isModifiedValid) blobNames.Add(blob.Name);
                    blobNames.Add(blob.Name);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Something went wrong communicating with the Azure Blob: " + e.InnerException.Message);
            }

            return blobNames;
        }
    }
}
