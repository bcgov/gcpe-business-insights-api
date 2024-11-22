using Gcpe.Hub.BusinessInsights.API.Models;
using Gcpe.Hub.BusinessInsights.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Gcpe.Hub.BusinessInsights.API.Entities;
using System.Collections.Generic;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;
using Url = Gcpe.Hub.BusinessInsights.API.Models.Url;
using Gcpe.Hub.BusinessInsights.API.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace Gcpe.Hub.BusinessInsights.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly IHubBusinessInsightsRepository _hubBusinessInsightsRepository;
        private readonly ILogger _logger;
        private readonly IReportGenerationService _reportGenerationService;
        private readonly IConfiguration _config;
        private readonly LocalBusinessInsightsDbContext _localContext;
		private readonly IWebHostEnvironment _env;

		public PostsController(
            IHubBusinessInsightsRepository hubBusinessInsightsRepository,
            IConfiguration config,
            ILogger logger,
            IReportGenerationService reportGenerationService,
            LocalBusinessInsightsDbContext localContext,
            IWebHostEnvironment env)
        {
            _hubBusinessInsightsRepository = hubBusinessInsightsRepository ?? throw new ArgumentNullException(nameof(hubBusinessInsightsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reportGenerationService = reportGenerationService ?? throw new ArgumentNullException(nameof(reportGenerationService));
            _config = config ?? throw new ArgumentNullException(nameof(_config));
            _localContext = localContext ?? throw new ArgumentNullException(nameof(localContext));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        // leaving this here in case we need a rollup report in the future, but currently unused
        [HttpGet("rollup")]
        public async Task<ActionResult<RollupReportDto>> GetTotals()
        {
            //var newsReleaseEntities = await _hubBusinessInsightsRepository.GetAllNewsReleasesAsync();
            //newsReleaseEntities = newsReleaseEntities.Where(release => release.PublishDateTime.Year == DateTimeOffset.Now.Year);
            //var items = newsReleaseEntities.Select(r => new NewsReleaseItem
            //{
            //    Key = r.Key,
            //    Ministry = r.Ministry,
            //    PublishDateTime = r.PublishDateTime,
            //    ReleaseType = r.ReleaseType
            //});

            //var releaseItems = items.ToList();
            //foreach (var release in releaseItems)
            //{
            //    var urls = await GetBlobs(release.Key, release.ReleaseType, release.PublishDateTime);
            //    foreach (var url in urls)
            //    {
            //        var u = new Url { Href = $"{_config["AzureTranslationsUrl"]}{url}", PublishDateTime = release.PublishDateTime };
            //        release.Urls.Add(u);
            //    }
            //}
            var releaseItems = _localContext.NewsReleaseItems.Include(u => u.Urls).ToList();
            // releaseItems = releaseItems.Where(i => i.Urls.Any()).ToList();
            var report = _reportGenerationService.GenerateRollupReport(releaseItems);
            return Ok(report);
        }

        [HttpGet("translations")]
        public async Task<ActionResult<TranslationReportDto>> GetTranslationsForPreviousMonth()
        {
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesAsync(); // no args returns the previous month
            var items = newsReleaseEntities.Select(r => new NewsReleaseItem
            {
                Key = r.Key,
                Ministry = r.Ministry,
                PublishDateTime = r.PublishDateTime,
                ReleaseType = r.ReleaseType
            });

            var releaseItems = items.ToList();
            foreach (var release in releaseItems)
            {
                var urls = await GetBlobs(release.Key, release.ReleaseType, release.PublishDateTime);
                foreach (var url in urls)
                {
                    var u = new Url { Href = $"{_config["AzureTranslationsUrl"]}{url}", PublishDateTime = release.PublishDateTime };
                    release.Urls.Add(u);
                }
            }
            releaseItems = releaseItems.Where(i => i.Urls.Count > 0).ToList();
            await GetHeadlines(releaseItems);

            var report = _reportGenerationService.GenerateMonthlyReport(releaseItems, await _hubBusinessInsightsRepository.GetAllMinistriesAsync(), _env.EnvironmentName);
            return Ok(report);
        }

        /// <summary>
        /// This action returns translations for any month from cached data. The start and end dates must be the first of the previous month and the first of the current month, respectively.
        /// For example for November 2022, start and end dates would be 2022-11-01 and 2022-12-01. The URL would be https://localhost:5001/api/posts/translations/custom?startDate=2022-11-01&endDate=2022-12-01
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("translations/custom")]
        public async Task<ActionResult<TranslationReportDto>> GetTranslations([FromQuery] string startDate = "", [FromQuery] string endDate = "")
        {
            var newsReleaseEntities = await _hubBusinessInsightsRepository.GetNewsReleasesInDateRangeAsync(startDate, endDate);
            var items = newsReleaseEntities.Select(r => new NewsReleaseItem
            {
                Key = r.Key,
                Ministry = r.Ministry,
                PublishDateTime = r.PublishDateTime,
                ReleaseType = r.ReleaseType
            });

            var releaseItems = items.ToList();
            foreach (var release in releaseItems)
            {
                var urls = await GetBlobs(release.Key, release.ReleaseType, release.PublishDateTime);
                foreach (var url in urls)
                {
                    var u = new Url { Href = $"{_config["AzureTranslationsUrl"]}{url}", PublishDateTime = release.PublishDateTime };
                    release.Urls.Add(u);
                }
            }
            releaseItems = releaseItems.Where(i => i.Urls.Count > 0).ToList();
            
            await GetHeadlines(releaseItems);

            var report = _reportGenerationService.GenerateMonthlyReport(releaseItems, await _hubBusinessInsightsRepository.GetAllMinistriesAsync(), _env.EnvironmentName);
            return Ok(report);
        }

        private async Task GetHeadlines(List<NewsReleaseItem> items)
        {
            foreach (var item in items)
            {
                var id = await _hubBusinessInsightsRepository.GetGuidForNewsRelease(item.Key);
                var documentId = await _hubBusinessInsightsRepository.GetDocumentIdForNewsRelease(id);
                item.Headline = await _hubBusinessInsightsRepository.GetHeadlineForNewsRelease(documentId);
            }
        }

        [HttpGet("translations/history")]
        public ActionResult GetHistory()
        {
            int startingYear = 2021;
            int currentYear = DateTime.Now.Year;
            int numberOfMonths = (currentYear - startingYear + 1) * 12;

            var history = Enumerable.Range(0, numberOfMonths)
                .Select(i => new DateCollection
                {
                    Month = DateTime.Now.AddMonths(i).Month,
                    Year = startingYear + (i / 12)
                })
                .ToList();

            var dates = history.Select(d => new
            {
                month = new DateTime(d.Year, d.Month, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                year = d.Year,
                start = new DateTime(d.Year, d.Month, 1).ToString("yyyy-MM-dd"),
                end = new DateTime(d.Year, d.Month, 1).AddMonths(1).ToString("yyyy-MM-dd")
            });

            var sortedDates = dates.OrderBy(d => d.year).ThenBy(d => d.month);
            var groupedDates = sortedDates
                .GroupBy(d => d.year)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(d => DateTime.ParseExact(d.month, "MMMM", CultureInfo.InvariantCulture).Month).ThenBy(d => d.month).ToList()
                );
            return Ok(groupedDates);
        }

		private async Task<List<string>> GetBlobs(string releaseId, int releaseType, DateTimeOffset publishDateTime)
        {
            var blobNames = new List<string>();
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(_config["CloudAccountConnectionString"]);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("translations");
                var parentDir = "releases";
                // if (releaseType == 3) parentDir = "factsheets";
                AsyncPageable<BlobItem> blobs = containerClient.GetBlobsAsync(prefix: $"{parentDir}/{releaseId}");

                await foreach (var blob in blobs)
                {
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
