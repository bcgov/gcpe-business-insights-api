using Gcpe.Hub.BusinessInsights.API.DbContexts;
using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
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

        public void AddNewsReleaseEntityAsync(NewsReleaseEntity newsReleaseEntity)
        {
            var newsReleaseItem = new NewsReleaseItem
            {
                Key = newsReleaseEntity.Key,
                Ministry = newsReleaseEntity.Ministry,
                PublishDateTime = newsReleaseEntity.PublishDateTime,
                ReleaseType = newsReleaseEntity.ReleaseType
            };
            _dbContext.NewsReleaseItems.Add(newsReleaseItem);
        }

        public void AddTranslationItemAsync(TranslationItem translationItem)
        {
            _dbContext.TranslationItems.Add(translationItem);
        }

        public async Task<IEnumerable<NewsReleaseItem>> GetAllNewsReleaseItems()
        {
            return await _dbContext.NewsReleaseItems.ToListAsync();
        }

        public async Task<IEnumerable<NewsReleaseItem>> GetNewsReleaseItemsInDateRangeAsync(string startDate, string endDate)
        {
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

        public async Task<IEnumerable<Translation>> GetTranslationItemsAsync()
        {
            return await _dbContext.TranslationItems
                .Include(u => u.Urls).Select(t => new Translation { Key = t.Key, Ministry = t.Ministry, Urls = t.Urls.Select(u => u.Href).ToList() }).ToListAsync();
        }

        public async Task<IEnumerable<Translation>> GetTranslationItemsInDateRangeAsync(string startDate, string endDate)
        {
            if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate)) 
                throw new ArgumentException("Invalid start or end date.");

            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);

            return await _dbContext.TranslationItems
                .Where(ti => DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(start, new TimeSpan(-7, 0, 0))) > 0
                    && DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(end, new TimeSpan(-7, 0, 0))) < 0 ||
                    DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(start, new TimeSpan(-8, 0, 0))) > 0
                    && DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(end, new TimeSpan(-8, 0, 0))) < 0
                    )
                .Include(u => u.Urls).Select(t => new Translation { Key = t.Key, Ministry = t.Ministry, Urls = t.Urls.Select(u => u.Href).ToList(), PublishDateTime = t.PublishDateTime }).ToListAsync();
        }

        public async Task<IEnumerable<Translation>> GetTranslationItemsInDateRangeAsync()
        {
            var today = DateTime.Today;
            var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);

            // just for readibility
            var start = firstOfPreviousMonth;
            var end = firstOfCurrentMonth;

            return await _dbContext.TranslationItems
                .Where(ti => DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(start, new TimeSpan(-7, 0, 0))) > 0
                    && DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(end, new TimeSpan(-7, 0, 0))) < 0 ||
                    DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(start, new TimeSpan(-8, 0, 0))) > 0
                    && DateTimeOffset.Compare(ti.PublishDateTime, new DateTimeOffset(end, new TimeSpan(-8, 0, 0))) < 0
                    )
                .Include(u => u.Urls).Select(t => new Translation { Key = t.Key, Ministry = t.Ministry, Urls = t.Urls.Select(u => u.Href).ToList(), PublishDateTime = t.PublishDateTime }).ToListAsync();
        }

        public async Task<IEnumerable<TranslationItem>> GetTranslationsForPreviousMonthAsync()
        {
            var today = DateTime.Today;
            var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);

            var startDate = firstOfPreviousMonth.ToString("yyyy-MM-dd");
            var endDate = firstOfCurrentMonth.ToString("yyyy-MM-dd");

            return await _dbContext.TranslationItems
                .FromSqlRaw(@$"
                    SELECT * FROM TranslationItems WHERE PublishDateTime >= '{startDate}' and PublishDateTime <= '{endDate}';
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

        public async void RemoveResultsForPreviousMonthAsync(IEnumerable<NewsReleaseItem> newsReleaseItems, IEnumerable<TranslationItem> translationItems, IEnumerable<Url> urls)
        {
            var listOfNewsReleaseIds = String.Join(',', newsReleaseItems.Select(nr => $"{nr.Id}").ToList());
            var sql = $@"DELETE FROM [NewsReleaseItems] WHERE Id in ({listOfNewsReleaseIds})";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);

            var listOfTranslationIds = String.Join(',', translationItems.Select(tr => $"{tr.Id}").ToList());
            sql = $@"DELETE FROM [TranslationItems] WHERE Id in ({listOfTranslationIds})";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);

            var listOfUrlIds = String.Join(',', urls.Select(u => $"{u.Id}").ToList());
            sql = $@"DELETE FROM [Urls] WHERE Id in ({listOfUrlIds})";
            await _dbContext.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync() >= 0;
        }
    }
}
