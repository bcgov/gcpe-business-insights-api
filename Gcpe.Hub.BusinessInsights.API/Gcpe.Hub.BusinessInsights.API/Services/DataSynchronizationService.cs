using Gcpe.Hub.BusinessInsights.API.Models;
using HtmlAgilityPack;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using ScrapySharp.Extensions;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Gcpe.Hub.BusinessInsights.API.Entities;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
                    var localTranslations = await _localDataRepository.GetTranslationsForPreviousMonthAsync();
                    var localUrls = await _localDataRepository.GetUrlsForPreviousMonthAsync();

                    _localDataRepository.RemoveResultsForPreviousMonthAsync(
                    localNewsReleaseEntities,
                    localTranslations,
                    localUrls
                    );
                }

                _logger.LogInformation("Adding latest NRs for previous month to cache...");
                foreach (var entity in newsReleaseEntities)
                {
                    _localDataRepository.AddNewsReleaseEntityAsync(entity);
                }
                await _localDataRepository.SaveChangesAsync();

                var loadingTasks = new List<Task<Translation>>();

                _logger.LogInformation("Checking Azure Blob to filter out NRs whose timestamp was modified outside of the previous month...");
                foreach (var entity in newsReleaseEntities)
                {
                    var blobs = new List<BlobItem>();
                    if (entity.ReleaseType == 3) // factsheet
                    {
                        try
                        {
                            BlobContainerClient container = new BlobContainerClient(_config["CloudAccountConnectionString"], "translations");
                            var resultSegment = container.GetBlobsByHierarchyAsync(prefix: $"factsheets/{entity.Key}", delimiter: "/");
                            await foreach (BlobHierarchyItem blob in resultSegment)
                            {
                                blobs.Add(blob.Blob);
                            }

                            var b = blobs;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Something went wrong communicating with the Azure Blob: " + e.InnerException.Message);
                        }
                    }

                    var today = DateTime.Today;
                    var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                    var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);
                    var skipAddingTask = entity.ReleaseType == 3 && blobs.Any(blob => blob.Properties.LastModified < firstOfPreviousMonth);

                    var loadTask = GetLinks(entity.Key, entity.Ministry, entity.PublishDateTime);
                    if (!skipAddingTask) loadingTasks.Add(loadTask);
                }
                _logger.LogInformation("Scraping urls from news.gov.bc.ca...");
                var translationsTasks = await Task.WhenAll(loadingTasks);

                var results = translationsTasks.ToList();
                var allTranslations = results;
                var translationsNullKeys = results.Where(i => i?.Key != null).ToList();
                var filtered = allTranslations.Except(translationsNullKeys);
                _logger.LogInformation($"Filtered out scraped {filtered.Count()} translations with null keys...");
                results = results.Where(i => i?.Key != null).ToList(); // filter out nulls

                _logger.LogInformation("Finished scraping URLs, saving to cache...");
                foreach (var result in results)
                {
                    var newTranslation = new TranslationItem { Key = result.Key, Ministry = result.Ministry, PublishDateTime = result.PublishDateTime };
                    foreach (var url in result.Urls)
                    {
                        newTranslation.Urls.Add(new Url { Href = url, PublishDateTime = result.PublishDateTime });
                    }

                    _localDataRepository.AddTranslationItemAsync(newTranslation);
                }
                await _localDataRepository.SaveChangesAsync();
                _logger.LogInformation($"Completed sync between cache and news.gov.bc.ca in: {timer.ElapsedMilliseconds} milliseconds.");
            }
        }

        public async Task SyncData(string startDate = "", string endDate = "")
        {
            _logger.LogInformation("Starting data sync...");
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesAsync(startDate, endDate);
            var localNewsReleaseEntities = await _localDataRepository.GetNewsReleaseItemsInDateRangeAsync(startDate, endDate);
            var isLocalCacheUpToDate = newsReleaseEntities.Count() == localNewsReleaseEntities.Count();

            if (isLocalCacheUpToDate) _logger.LogInformation("NR cache for date range is up-to-date, skipping sync...");

            if (!isLocalCacheUpToDate)
            {
                // clean up incomplete results and re-fetch
                _logger.LogInformation("NR cache is out-dated for date range, removing old entries and fetching latest NRs...");
                if (localNewsReleaseEntities.Any())
                {
                    var localTranslations = await _localDataRepository.GetTranslationsForPreviousMonthAsync();
                    var localUrls = await _localDataRepository.GetUrlsForPreviousMonthAsync();

                    _localDataRepository.RemoveResultsForPreviousMonthAsync(
                    localNewsReleaseEntities,
                    localTranslations,
                    localUrls
                    );
                }

                _logger.LogInformation("Adding latest NRs for date range to cache...");
                foreach (var entity in newsReleaseEntities)
                {
                    _localDataRepository.AddNewsReleaseEntityAsync(entity);
                }
                await _localDataRepository.SaveChangesAsync();

                var loadingTasks = new List<Task<Translation>>();

                _logger.LogInformation("Checking Azure Blob to filter out NRs whose timestamp was modified outside of date range...");
                foreach (var entity in newsReleaseEntities)
                {
                    var blobs = new List<BlobItem>();
                    if (entity.ReleaseType == 3) // factsheet
                    {
                        try
                        {
                            BlobContainerClient container = new BlobContainerClient(_config["CloudAccountConnectionString"], "translations");
                            var resultSegment = container.GetBlobsByHierarchyAsync(prefix: $"factsheets/{entity.Key}", delimiter: "/");
                            await foreach (BlobHierarchyItem blob in resultSegment)
                            {
                                blobs.Add(blob.Blob);
                            }

                            var b = blobs;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Something went wrong communicating with the Azure Blob: " + e.InnerException.Message);
                        }
                    }

                    var today = DateTime.Today;
                    var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                    var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);
                    var skipAddingTask = entity.ReleaseType == 3 && blobs.Any(blob => blob.Properties.LastModified < firstOfPreviousMonth);

                    var loadTask = GetLinks(entity.Key, entity.Ministry, entity.PublishDateTime);
                    if (!skipAddingTask) loadingTasks.Add(loadTask);
                }
                _logger.LogInformation("Scraping urls from news.gov.bc.ca...");
                var translationsTasks = await Task.WhenAll(loadingTasks);

                var results = translationsTasks.ToList();
                var allTranslations = results;
                var translationsNullKeys = results.Where(i => i?.Key != null).ToList();
                var filtered = allTranslations.Except(translationsNullKeys);
                _logger.LogInformation($"Filtered out scraped {filtered.Count()} translations with null keys...");
                results = results.Where(i => i?.Key != null).ToList(); // filter out nulls

                _logger.LogInformation("Finished scraping URLs, saving to cache...");
                foreach (var result in results)
                {
                    var newTranslation = new TranslationItem { Key = result.Key, Ministry = result.Ministry, PublishDateTime = result.PublishDateTime };
                    foreach (var url in result.Urls)
                    {
                        newTranslation.Urls.Add(new Url { Href = url, PublishDateTime = result.PublishDateTime });
                    }

                    _localDataRepository.AddTranslationItemAsync(newTranslation);
                }
                await _localDataRepository.SaveChangesAsync();
                _logger.LogInformation($"Completed sync between cache and news.gov.bc.ca for selected date range.");
            }
        }

        private async Task<Translation> GetLinks(string releaseId, string ministry, DateTimeOffset publishDateTime)
        {
            HtmlDocument doc = new HtmlDocument();

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://news.gov.bc.ca/releases/{releaseId}");
                    var content = await response.Content.ReadAsStringAsync();
                    doc.LoadHtml(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong scraping news.gov.bc.ca: {ex.InnerException.Message}");
            }

            var translation = new Translation { Key = releaseId, Ministry = ministry, PublishDateTime = publishDateTime };
            var pageLinks = doc?.DocumentNode?.SelectNodes("//a[starts-with(@href, 'https://bcgovnews.azureedge.net/translations/')]");

            if (pageLinks == null) return null;

            foreach (HtmlNode link in pageLinks)
            {
                translation.Urls.Add(link.GetAttributeValue("href"));
            }

            translation.Urls = translation.Urls.Distinct().ToList();

            return translation;
        }
    }
}
