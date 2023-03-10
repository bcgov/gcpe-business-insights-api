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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Gcpe.Hub.BusinessInsights.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly ILocalDataRepository _localDataRepository;
        private readonly IHubBusinessInsightsRepository _hubBusinessInsightsRepository;
        private readonly IDataSynchronizationService _dataSynchronizationService;
        private readonly ILogger _logger;
        private readonly IReportGenerationService _reportGenerationService;
        private readonly IMemoryCache _memoryCache;
        public PostsController(
            ILocalDataRepository localDataRepository,
            IHubBusinessInsightsRepository hubBusinessInsightsRepository,
            IConfiguration config,
            IDataSynchronizationService dataSynchronizationService,
            ILogger logger,
            IReportGenerationService reportGenerationService,
            IMemoryCache memoryCache)
        {
            _localDataRepository = localDataRepository ?? throw new ArgumentNullException(nameof(localDataRepository));
            _hubBusinessInsightsRepository = hubBusinessInsightsRepository ?? throw new ArgumentNullException(nameof(hubBusinessInsightsRepository));
            _dataSynchronizationService = dataSynchronizationService ?? throw new ArgumentNullException(nameof(dataSynchronizationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reportGenerationService = reportGenerationService ?? throw new ArgumentNullException(nameof(reportGenerationService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        [HttpGet("rollup")]
        public async Task<ActionResult<RollupReportDto>> GetTotals()
        {
            var cacheKey = "rollup_items";
            if (!_memoryCache.TryGetValue(cacheKey, out List<NewsReleaseWithUrls> items))
            {
                var releases = await _localDataRepository.GetAllNewsReleaseItems();

                if (!releases.Any()) return BadRequest("No translations available for date range.");

                releases = releases.Where(release => release.PublishDateTime.Year == DateTimeOffset.Now.Year);
                items = await CreateViewModel(releases);
                _memoryCache.Set(cacheKey, items, TimeSpan.FromMinutes(5));
            }

            var report = _reportGenerationService.GenerateRollupReport(items);
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
            var cacheKey = $"items_{startDate}_{endDate}";
            if (!_memoryCache.TryGetValue(cacheKey, out List<NewsReleaseWithUrls> items))
            {
                var releases = await _localDataRepository.GetNewsReleaseItemsInDateRangeAsync(startDate, endDate);

                if (!releases.Any()) return BadRequest("No translations available for date range.");
                items = await CreateViewModel(releases);
                await GetHeadlines(items);
                _memoryCache.Set(cacheKey, items, TimeSpan.FromMinutes(5));
            }

            var report = _reportGenerationService.GenerateMonthlyReport(items);
            return Ok(report);
        }

        [HttpGet("translations")]
        public async Task<ActionResult<TranslationReportDto>> GetTranslationsForPreviousMonth()
        {
            var cacheKey = "items";
            if (!_memoryCache.TryGetValue(cacheKey, out List<NewsReleaseWithUrls> items))
            {
                var releases = await _localDataRepository.GetNewsReleasesForPreviousMonthAsync(); // no args returns the previous month

                if (!releases.Any()) return BadRequest("No translations available for date range.");

                items = await CreateViewModel(releases);
                await GetHeadlines(items);
                _memoryCache.Set(cacheKey, items, TimeSpan.FromMinutes(5));
            }

            var report = _reportGenerationService.GenerateMonthlyReport(items);
            return Ok(report);
        }

        private async Task GetHeadlines(List<NewsReleaseWithUrls> items)
        {
            foreach (var item in items)
            {
                var id = await _hubBusinessInsightsRepository.GetGuidForNewsRelease(item.NewsRelease.Key);
                var documentId = await _hubBusinessInsightsRepository.GetDocumentIdForNewsRelease(id);
                item.Headline = await _hubBusinessInsightsRepository.GetHeadlineForNewsRelease(documentId);
            }
        }

        /// <summary>
        /// This action fetches translations for any month. The start and end dates must be the first of the previous month and the first of the current month, respectively.
        /// For example for November 2022, start and end dates would be 2022-11-01 and 2022-12-01. The URL would be https://localhost:5001/api/posts/sync?startDate=2022-11-01&endDate=2022-12-01
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        // [Authorize]
        [HttpGet("sync")]
        public async Task<ActionResult> SyncCustomRange([FromQuery] string s = "", [FromQuery] string e = "")
        {
            try
            {
                if (!IsValidDateTimeString(s) || !IsValidDateTimeString(e)) return BadRequest("Something went wrong: Either the start or end date is in the wrong format. It should be dd-MM-yyyy.");

                await _dataSynchronizationService.SyncData(s, e); // s and e refer to start and end date, shortened for convenience
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest, $"Something went wrong: {ex.Message}");
            }

            return Ok($"synced hub news releases successfully start date: {s} end date: {e}");
        }

        [HttpGet("translations/history")]
        public async Task<ActionResult<TranslationReportDto>> GetHistory()
        {
            var nrItems = await _localDataRepository.GetAllNewsReleaseItems(includeUrls: true);

            if (!nrItems.Any()) return BadRequest("No releases available for date range.");

            var history = nrItems.Where(nr => nr.Urls.Any()).Select(nr => nr.PublishDateTime).Select(d => new { d.Month, d.Year }).Distinct().ToList();

            var dates = history.Select(d => new
            {
                month = new DateTime(d.Year, d.Month, 1).ToString("MMMM", CultureInfo.InvariantCulture),
                year = d.Year,
                start = new DateTime(d.Year, d.Month, 1).ToString("yyyy-MM-dd"),
                end = new DateTime(d.Year, d.Month, 1).AddMonths(1).ToString("yyyy-MM-dd")
            });

            return Ok(dates);
        }

        private async Task<List<NewsReleaseWithUrls>> CreateViewModel(IEnumerable<NewsReleaseItem> releases)
        {
            var items = new List<NewsReleaseWithUrls>();
            foreach (var rls in releases)
            {
                var urls = await _localDataRepository.GetUrlsForNewsRelease(rls.Id);
                if (urls.Any()) items.Add(new NewsReleaseWithUrls(rls, urls));
            }

            return items;
        }

        private bool IsValidDateTimeString(string datetime)
        {
            DateTime result = new DateTime();
            if (!DateTime.TryParseExact(datetime, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return false;

            return true;
        }
    }
}
