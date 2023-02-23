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

namespace Gcpe.Hub.BusinessInsights.API.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly ILocalDataRepository _localDataRepository;
        private readonly IDataSynchronizationService _dataSynchronizationService;
        private readonly ILogger _logger;
        private readonly IReportGenerationService _reportGenerationService;
        public PostsController(
            ILocalDataRepository localDataRepository,
            IConfiguration config,
            IDataSynchronizationService dataSynchronizationService,
            ILogger logger,
            IReportGenerationService reportGenerationService)
        {
            _localDataRepository = localDataRepository ?? throw new ArgumentNullException(nameof(localDataRepository));
            _dataSynchronizationService = dataSynchronizationService ?? throw new ArgumentNullException(nameof(dataSynchronizationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reportGenerationService = reportGenerationService ?? throw new ArgumentNullException(nameof(reportGenerationService));
        }

        [HttpGet("rollup")]
        public async Task<ActionResult<RollupReportDto>> GetTotals()
        {
            var releases = await _localDataRepository.GetAllNewsReleaseItems();

            if (!releases.Any()) return BadRequest("No translations available for date range.");

            List<NewsReleaseWithUrls> items = await CreateViewModel(releases);

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
            var releases = await _localDataRepository.GetNewsReleaseItemsInDateRangeAsync(startDate, endDate);

            if (!releases.Any()) return BadRequest("No translations available for date range.");

            List<NewsReleaseWithUrls> items = await CreateViewModel(releases);

            var report = _reportGenerationService.GenerateMonthlyReport(items);
            return Ok(report);
        }

        [HttpGet("translations")]
        public async Task<ActionResult<TranslationReportDto>> GetTranslationsForPreviousMonth()
        {
            var releases = await _localDataRepository.GetNewsReleasesForPreviousMonthAsync(); // no args returns the previous month

            if (!releases.Any()) return BadRequest("No translations available for date range.");
            List<NewsReleaseWithUrls> items = await CreateViewModel(releases);

            var report = _reportGenerationService.GenerateMonthlyReport(items);
            return Ok(report);
        }

        /// <summary>
        /// This action fetches translations for any month. The start and end dates must be the first of the previous month and the first of the current month, respectively.
        /// For example for November 2022, start and end dates would be 2022-11-01 and 2022-12-01. The URL would be https://localhost:5001/api/posts/sync?startDate=2022-11-01&endDate=2022-12-01
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        // [Authorize]
        [HttpGet("sync")]
        public async Task<ActionResult> SyncCustomRange([FromQuery] string startDate = "", [FromQuery] string endDate = "")
        {
            try
            {
                await _dataSynchronizationService.SyncData(startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException.Message);
                return StatusCode(StatusCodes.Status400BadRequest, $"Something went wrong: {ex.Message}");
            }

            return Ok();
        }

        [HttpGet("translations/history")]
        public async Task<ActionResult<TranslationReportDto>> GetHistory()
        {
            var nrItems = await _localDataRepository.GetAllNewsReleaseItems();

            if (!nrItems.Any()) return BadRequest("No releases available for date range.");

            var history = nrItems.Select(nr => nr.PublishDateTime).Select(d => new { d.Month, d.Year }).Distinct().ToList();

            var dates = history.Select(d => new
            {
                month = new DateTime(d.Year, d.Month, 1).AddMonths(-1).ToString("MMMM", CultureInfo.InvariantCulture),
                year = d.Year,
                start = new DateTime(d.Year, d.Month, 1).AddMonths(-1).ToString("yyyy-MM-dd"),
                end = new DateTime(d.Year, d.Month, 1).ToString("yyyy-MM-dd")
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
    }
}
