using Gcpe.Hub.BusinessInsights.API.DbContexts;
using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Guards;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class LocalDataRepository : ILocalDataRepository
    {
        private readonly LocalDbContext _dbContext;

        public LocalDataRepository(LocalDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public NewsReleaseItem AddNewsReleaseEntity(NewsReleaseEntity newsReleaseEntity)
        {
            var newsReleaseItem = new NewsReleaseItem
            {
                Key = newsReleaseEntity.Key,
                Ministry = newsReleaseEntity.Ministry,
                PublishDateTime = newsReleaseEntity.PublishDateTime,
                ReleaseType = newsReleaseEntity.ReleaseType
            };
            _dbContext.NewsReleaseItems.Add(newsReleaseItem);
            return newsReleaseItem;
        }

        public void AddUrl(Url url)
        {
            _dbContext.Urls.Add(url);
        }

        public async Task<IEnumerable<NewsReleaseItem>> GetAllNewsReleaseItems()
        {
            return await _dbContext.NewsReleaseItems.ToListAsync();
        }

        public async Task<IEnumerable<NewsReleaseItem>> GetNewsReleaseItemsInDateRangeAsync(string startDate = "", string endDate = "")
        {
            DateGuard.ThrowIfNullOrWhitespace(startDate, endDate);
            DateGuard.ThrowIfNullOrEmpty(endDate, startDate);

            var start = DateTimeOffset.Parse(startDate);
            var end = DateTimeOffset.Parse(endDate);

            DateGuard.ThrowIfEndIsBeforeStart(start, end);
            DateGuard.ThrowIfStartAndEndAreEqual(start, end);
            DateGuard.ThrowIfNotFirstOfTheMonth(start, end);
            DateGuard.ThrowIfDateRangeNotOneMonth(start, end);

            return await _dbContext.NewsReleaseItems
                .FromSqlRaw(@$"
                    SELECT * FROM NewsReleaseItems WHERE PublishDateTime >= '{startDate}' and PublishDateTime <= '{endDate}';
                ").ToListAsync();
        }

        public async Task<IEnumerable<NewsReleaseItem>> GetNewsReleasesForPreviousMonthAsync()
        {
            var today = DateTime.Today;
            var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);

            var startDate = firstOfPreviousMonth.ToString("yyyy-MM-dd");
            var endDate = firstOfCurrentMonth.ToString("yyyy-MM-dd");

            return await _dbContext.NewsReleaseItems
                .FromSqlRaw(@$"
                    SELECT * FROM NewsReleaseItems WHERE PublishDateTime >= '{startDate}' and PublishDateTime <= '{endDate}';
                ").ToListAsync();
        }

        public async Task<IEnumerable<Url>> GetUrlsForPreviousMonthAsync()
        {
            var today = DateTime.Today;
            var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);

            var startDate = firstOfPreviousMonth.ToString("yyyy-MM-dd");
            var endDate = firstOfCurrentMonth.ToString("yyyy-MM-dd");

            return await _dbContext.Urls
                .FromSqlRaw(@$"
                    SELECT * FROM Urls WHERE PublishDateTime >= '{startDate}' and PublishDateTime <= '{endDate}';
                ").ToListAsync();
        }

        public async void DeleteNewsReleasesAsync(IEnumerable<NewsReleaseItem> newsReleaseItems)
        {
            var listOfNewsReleaseIds = String.Join(',', newsReleaseItems.Select(nr => $"{nr.Id}").ToList());
            var sql = $@"DELETE FROM [NewsReleaseItems] WHERE Id in ({listOfNewsReleaseIds})";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task<IEnumerable<Url>> GetUrlsForNewsRelease(int newsReleaseItemId)
        {
            return await _dbContext.Urls.Where(u => u.NewsReleaseItemId == newsReleaseItemId).ToListAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync() >= 0;
        }
    }
}
