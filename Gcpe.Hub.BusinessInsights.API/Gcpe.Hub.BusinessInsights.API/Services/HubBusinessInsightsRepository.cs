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
    public class HubBusinessInsightsRepository : IHubBusinessInsightsRepository
    {
        private readonly HubBusinessInsightsDbContext _dbContext;

        private List<string> excludedMinistries = new List<string>
        {
            "GCPE HQ", 
            "Environmental Assessment Office",
            "GCPE Media Relations",
            "BC Coroners Service",
            "Minister of State for Child Care",
            "Minister of State for Infrastructure and Transit",
            "Minister of State for Trade",
            "Minister of State for Workforce Development",
            "Joint Information Centre",
            "Joint Information Centre - Drought",
            "Joint Information Centre - Wildfires",
            "BC Wildfire Service",
            "Citizen Engagement",
            "Minister of State for Sustainable Forestry Innovation",
            "Minister of State for Child Care and Children and Youth with Support Needs",
            "Minister of State for Community Safety and Integrated Services",
            "Minister of State for Local Governments and Rural Communities"
        };

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
                and (nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime <= '{endDate}' )
                and nr.IsActive = 1

                UNION

                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where HasTranslations = 1
                and (nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime <= '{endDate}' )
                and nr.IsActive = 1
                and releaseType = 1
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
                and releaseType = 1

                UNION

                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where HasTranslations = 1
                and (nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime <= '{endDate}' )
                and releaseType = 1
                ) t
                ORDER BY PublishDateTime
            ").ToListAsync();

            return newsReleaseEntities;
        }

        public async Task<IEnumerable<NewsReleaseEntity>> GetAllNewsReleasesAsync()
        {
            var newsReleaseEntities = await _dbContext.NewsReleaseEntities.FromSqlRaw(@"
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

                UNION

                select (select DisplayName from [Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from [Gcpe.Hub].dbo.NewsRelease nr
                where HasTranslations = 1
                and releaseType = 1
                ) t
                ORDER BY PublishDateTime
            ").ToListAsync();

            return newsReleaseEntities;
        }

        public async Task<IEnumerable<NewsReleaseEntity>> GetNewsReleasesInDateRangeAsync(string startDate = "", string endDate = "")
        {
            return await _dbContext.NewsReleaseEntities
            .FromSqlRaw(@$"SELECT *
                            FROM(
                                select(select DisplayName from[Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from[Gcpe.Hub].dbo.NewsRelease nr
                                where Id in (
                                select ReleaseId from[Gcpe.Hub].dbo.NewsReleaseDocument nrd
                                where nrd.Id in (
                                select
                                    DocumentId
                                    from[Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                                    where(BodyHtml like '%translation%' or BodyHtml like '%translations%')
                                    or(Subheadline like '%translation%' or Subheadline like '%translations%')))
                                and(nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime < '{endDate}T00:00:00-07:00')
                                and nr.IsActive = 1
                                and nr.IsPublished = 1
                                and releaseType = 1

                                UNION

                                select(select DisplayName from[Gcpe.Hub].dbo.Ministry where Id = MinistryId) as ministry, [Key], PublishDateTime, ReleaseType from[Gcpe.Hub].dbo.NewsRelease nr
                                where HasTranslations = 1
                                and(nr.PublishDateTime >= '{startDate}' and nr.PublishDateTime < '{endDate}T00:00:00-07:00')
                                and nr.IsActive = 1
                                and nr.IsPublished = 1
                                and releaseType = 1
                                ) t
                                ORDER BY PublishDateTime")
                .ToListAsync();
        }

        public async Task<string> GetGuidForNewsRelease(string key)
        {
            var release = await _dbContext.NewsRelease.FirstOrDefaultAsync(r => r.Key == key);
            return release.Id.ToString();
        }

        public async Task<string> GetDocumentIdForNewsRelease(string newsReleaseId)
        {
            var nrd = await _dbContext.NewsReleaseDocument.FirstOrDefaultAsync(nrd => nrd.ReleaseId == Guid.Parse(newsReleaseId));
            return nrd.Id.ToString();
        }

        public async Task<string> GetHeadlineForNewsRelease(string documentId)
        {
            var nrdl = await _dbContext.NewsReleaseDocumentLanguage.FirstOrDefaultAsync(nrdl => nrdl.DocumentId == Guid.Parse(documentId));
            return nrdl.Headline ?? "";
        }

        public async Task<List<string>> GetAllMinistriesAsync()
        {
            var allMinistries = await _dbContext.Ministry.Where(m => m.IsActive).Select(m => m.DisplayName).ToListAsync();
            return allMinistries.Except(excludedMinistries).ToList();
        }

        private string FormatDateTime(DateTime dateTime) => dateTime.ToString("yyyy-MM-dd");
    }
}
