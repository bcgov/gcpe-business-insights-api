using Gcpe.Hub.BusinessInsights.API.DbContexts;
using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Guards;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class HubBusinessInsightsRepository : IHubBusinessInsightsRepository
    {
        private readonly HubBusinessInsightsDbContext _dbContext;

        public HubBusinessInsightsRepository(
            HubBusinessInsightsDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<NewsReleaseEntity>> GetNewsReleasesAsync()
        {
            var today = DateTime.Today;
            var firstOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);

            var startDate = FormatDateTime(firstOfPreviousMonth);
            var endDate = FormatDateTime(firstOfCurrentMonth);

            var newsReleaseEntities = await _dbContext.NewsReleaseEntities.FromSqlRaw(@$"
                SELECT  * 
                FROM (
                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where Id in (
                select ReleaseId from [Gcpe.Hub].dbo.NewsReleaseDocument nrd
                where nrd.Id in (
                select 
                    DocumentId
                    from [Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                    where (BodyHtml like '%translation%' or BodyHtml like '%translations%')
                    or (Subheadline like '%translation%' or Subheadline like '%translations%')))
                and (nr.PublishDateTime >= '{firstOfPreviousMonth}' and nr.PublishDateTime <= '{firstOfCurrentMonth}' )

                UNION

                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where HasTranslations = 1
                and (nr.PublishDateTime >= '{firstOfPreviousMonth}' and nr.PublishDateTime <= '{firstOfCurrentMonth}' )
                ) t
                ORDER BY PublishDateTime
            ").ToListAsync();

            return newsReleaseEntities;
        }

        public async Task<IEnumerable<NewsReleaseEntity>> GetNewsReleasesAsync(string startDate, string endDate)
        {
            DateGuard.ThrowIfNullOrWhitespace(startDate, endDate);
            DateGuard.ThrowIfNullOrEmpty(endDate, startDate);

            var start = DateTimeOffset.Parse(startDate);
            var end = DateTimeOffset.Parse(endDate);

            DateGuard.ThrowIfEndIsBeforeStart(start, end);
            DateGuard.ThrowIfStartAndEndAreEqual(start, end);
            DateGuard.ThrowIfNotFirstOfTheMonth(start, end);
            DateGuard.ThrowIfDateRangeNotOneMonth(start, end);

            var newsReleaseEntities = await _dbContext.NewsReleaseEntities.FromSqlRaw(@$"
                SELECT  * 
                FROM (
                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where Id in (
                select ReleaseId from [Gcpe.Hub].dbo.NewsReleaseDocument nrd
                where nrd.Id in (
                select 
                    DocumentId
                    from [Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                    where (BodyHtml like '%translation%' or BodyHtml like '%translations%')
                    or (Subheadline like '%translation%' or Subheadline like '%translations%')))
                and (nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime <= '{endDate}' )

                UNION

                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where HasTranslations = 1
                and (nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime <= '{endDate}' )
                ) t
                ORDER BY PublishDateTime
            ").ToListAsync();

            return newsReleaseEntities;
        }

        private string FormatDateTime(DateTime dateTime) => dateTime.ToString("yyyy-MM-dd");
    }
}
