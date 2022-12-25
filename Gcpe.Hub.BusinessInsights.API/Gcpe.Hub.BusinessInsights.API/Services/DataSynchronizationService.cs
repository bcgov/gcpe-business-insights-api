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

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class DataSynchronizationService : IDataSynchronizationService
    {
        private readonly IHubBusinessInsightsRepository _hubBusinessInsightsRepository;
        private readonly ILocalDataRepository _localDataRepository;
        private readonly IConfiguration _config;
        public DataSynchronizationService(
            IHubBusinessInsightsRepository hubBusinessInsightsRepository,
            ILocalDataRepository localDataRepository,
            IConfiguration config)
        {
            _hubBusinessInsightsRepository = hubBusinessInsightsRepository ?? throw new ArgumentNullException(nameof(hubBusinessInsightsRepository));
            _localDataRepository = localDataRepository;
            _config = config;
        }

        public async Task SyncData()
        {
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesAsync();
            var localNewsReleaseEntities = await _localDataRepository.GetNewsReleasesForPreviousMonthAsync();
            var isLocalCacheUpToDate = newsReleaseEntities.Count() == localNewsReleaseEntities.Count();

            if (!isLocalCacheUpToDate)
            {
                // clean up incomplete results and re-fetch
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

                foreach (var entity in newsReleaseEntities)
                {
                    _localDataRepository.AddNewsReleaseEntityAsync(entity);
                }
                await _localDataRepository.SaveChangesAsync();

                var loadingTasks = new List<Task<Translation>>();

                foreach (var entity in newsReleaseEntities)
                {
                    var blobs = new List<BlobItem>();
                    if (entity.ReleaseType == 3) // factsheet
                    {
                        BlobContainerClient container = new BlobContainerClient(_config["CloudAccountConnectionString"], "translations");
                        var resultSegment = container.GetBlobsByHierarchyAsync(prefix: $"factsheets/{entity.Key}", delimiter: "/");
                        await foreach (BlobHierarchyItem blob in resultSegment)
                        {
                            blobs.Add(blob.Blob);
                        }

                        var b = blobs;
                    }

                    var today = DateTime.Today;
                    var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                    var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);
                    var skipAddingTask = entity.ReleaseType == 3 && blobs.Any(blob => blob.Properties.LastModified < firstOfPreviousMonth);

                    var loadTask = GetLinks(entity.Key, entity.Ministry, entity.PublishDateTime);
                    if (!skipAddingTask) loadingTasks.Add(loadTask);
                }
                var translationsTasks = await Task.WhenAll(loadingTasks);

                var results = translationsTasks.ToList();
                results = results.Where(i => i?.Key != null).ToList(); // filter out nulls

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
            }
        }

        public async Task SyncData(string startDate = "", string endDate = "")
        {
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesAsync(startDate, endDate);
            var localNewsReleaseEntities = await _localDataRepository.GetNewsReleaseItemsInDateRangeAsync(startDate, endDate);
            var isLocalCacheUpToDate = newsReleaseEntities.Count() == localNewsReleaseEntities.Count();

            if (!isLocalCacheUpToDate)
            {
                // clean up incomplete results and re-fetch
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

                foreach (var entity in newsReleaseEntities)
                {
                    _localDataRepository.AddNewsReleaseEntityAsync(entity);
                }
                await _localDataRepository.SaveChangesAsync();

                var loadingTasks = new List<Task<Translation>>();

                foreach (var entity in newsReleaseEntities)
                {
                    var blobs = new List<BlobItem>();
                    if (entity.ReleaseType == 3) // factsheet
                    {
                        BlobContainerClient container = new BlobContainerClient(_config["CloudAccountConnectionString"], "translations");
                        var resultSegment = container.GetBlobsByHierarchyAsync(prefix: $"factsheets/{entity.Key}", delimiter: "/");
                        await foreach (BlobHierarchyItem blob in resultSegment)
                        {
                            blobs.Add(blob.Blob);
                        }

                        var b = blobs;
                    }

                    var today = DateTime.Today;
                    var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
                    var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);
                    var skipAddingTask = entity.ReleaseType == 3 && blobs.Any(blob => blob.Properties.LastModified < firstOfPreviousMonth);

                    var loadTask = GetLinks(entity.Key, entity.Ministry, entity.PublishDateTime);
                    if (!skipAddingTask) loadingTasks.Add(loadTask);
                }
                var translationsTasks = await Task.WhenAll(loadingTasks);

                var results = translationsTasks.ToList();
                results = results.Where(i => i?.Key != null).ToList(); // filter out nulls

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
            }
        }

        private static async Task<Translation> GetLinks(string releaseId, string ministry, DateTimeOffset publishDateTime)
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
                Console.WriteLine(ex.Message);
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
