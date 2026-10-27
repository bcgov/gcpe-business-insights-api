using Gcpe.Hub.BusinessInsights.API.DbContexts;
using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Guards;
using Gcpe.Hub.BusinessInsights.API.Helpers;
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
            var today = BcTime.Today;

            var firstOfCurrentMonth = new DateTime(
                today.Year,
                today.Month,
                1);

            var firstOfPreviousMonth = firstOfCurrentMonth.AddMonths(-1);

            var startInstant = BcTime.FromLocal(firstOfPreviousMonth);
            var endInstant = BcTime.FromLocal(firstOfCurrentMonth);

            var newsReleaseEntities =
                await _dbContext.NewsReleaseEntities
                    .FromSqlInterpolated($@"
                        SELECT *
                        FROM (
                            SELECT
                                (SELECT DisplayName
                                 FROM [Gcpe.Hub].dbo.Ministry
                                 WHERE Id = MinistryId) AS ministry,
                                [Key],
                                PublishDateTime,
                                ReleaseType
                            FROM [Gcpe.Hub].dbo.NewsRelease nr
                            WHERE Id IN (
                                SELECT ReleaseId
                                FROM [Gcpe.Hub].dbo.NewsReleaseDocument nrd
                                WHERE nrd.Id IN (
                                    SELECT DocumentId
                                    FROM [Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                                    WHERE
                                        (BodyHtml LIKE '%translation%'
                                         OR BodyHtml LIKE '%translations%')
                                        OR
                                        (Subheadline LIKE '%translation%'
                                         OR Subheadline LIKE '%translations%')
                                )
                            )
                            AND nr.PublishDateTime >= {startInstant}
                            AND nr.PublishDateTime <= {endInstant}
                            AND nr.IsActive = 1

                            UNION

                            SELECT
                                (SELECT DisplayName
                                 FROM [Gcpe.Hub].dbo.Ministry
                                 WHERE Id = MinistryId) AS ministry,
                                [Key],
                                PublishDateTime,
                                ReleaseType
                            FROM [Gcpe.Hub].dbo.NewsRelease nr
                            WHERE HasTranslations = 1
                            AND nr.PublishDateTime >= {startInstant}
                            AND nr.PublishDateTime <= {endInstant}
                            AND nr.IsActive = 1
                            AND ReleaseType = 1
                        ) t
                        ORDER BY PublishDateTime")
                    .ToListAsync();

            return newsReleaseEntities;
        }

        public async Task<IEnumerable<NewsReleaseEntity>> GetNewsReleasesAsync(
            string startDate,
            string endDate)
        {
            DateGuard.ThrowIfNullOrWhitespace(startDate, endDate);
            DateGuard.ThrowIfNullOrEmpty(endDate, startDate);

            var start = DateTimeOffset.Parse(startDate);
            var end = DateTimeOffset.Parse(endDate);

            DateGuard.ThrowIfEndIsBeforeStart(start, end);
            DateGuard.ThrowIfStartAndEndAreEqual(start, end);
            DateGuard.ThrowIfNotFirstOfTheMonth(start, end);
            DateGuard.ThrowIfDateRangeNotOneMonth(start, end);

            // Treat supplied calendar dates as BC wall-clock dates,
            // regardless of the server's local time zone.
            var startBc = DateTime.SpecifyKind(
                start.Date,
                DateTimeKind.Unspecified);

            var endBc = DateTime.SpecifyKind(
                end.Date,
                DateTimeKind.Unspecified);

            var startInstant = BcTime.FromLocal(startBc);
            var endInstant = BcTime.FromLocal(endBc);

            var newsReleaseEntities =
                await _dbContext.NewsReleaseEntities
                    .FromSqlInterpolated($@"
                        SELECT *
                        FROM (
                            SELECT
                                (SELECT DisplayName
                                 FROM [Gcpe.Hub].dbo.Ministry
                                 WHERE Id = MinistryId) AS ministry,
                                [Key],
                                PublishDateTime,
                                ReleaseType
                            FROM [Gcpe.Hub].dbo.NewsRelease nr
                            WHERE Id IN (
                                SELECT ReleaseId
                                FROM [Gcpe.Hub].dbo.NewsReleaseDocument nrd
                                WHERE nrd.Id IN (
                                    SELECT DocumentId
                                    FROM [Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                                    WHERE
                                        (BodyHtml LIKE '%translation%'
                                         OR BodyHtml LIKE '%translations%')
                                        OR
                                        (Subheadline LIKE '%translation%'
                                         OR Subheadline LIKE '%translations%')
                                )
                            )
                            AND nr.PublishDateTime >= {startInstant}
                            AND nr.PublishDateTime <= {endInstant}
                            AND ReleaseType = 1

                            UNION

                            SELECT
                                (SELECT DisplayName
                                 FROM [Gcpe.Hub].dbo.Ministry
                                 WHERE Id = MinistryId) AS ministry,
                                [Key],
                                PublishDateTime,
                                ReleaseType
                            FROM [Gcpe.Hub].dbo.NewsRelease nr
                            WHERE HasTranslations = 1
                            AND nr.PublishDateTime >= {startInstant}
                            AND nr.PublishDateTime <= {endInstant}
                            AND ReleaseType = 1
                        ) t
                        ORDER BY PublishDateTime")
                    .ToListAsync();

            return newsReleaseEntities;
        }

        public async Task<IEnumerable<NewsReleaseEntity>> GetAllNewsReleasesAsync()
        {
            var newsReleaseEntities =
                await _dbContext.NewsReleaseEntities
                    .FromSqlRaw(@"
                        SELECT *
                        FROM (
                            SELECT
                                (SELECT DisplayName
                                 FROM [Gcpe.Hub].dbo.Ministry
                                 WHERE Id = MinistryId) AS ministry,
                                [Key],
                                PublishDateTime,
                                ReleaseType
                            FROM [Gcpe.Hub].dbo.NewsRelease nr
                            WHERE Id IN (
                                SELECT ReleaseId
                                FROM [Gcpe.Hub].dbo.NewsReleaseDocument nrd
                                WHERE nrd.Id IN (
                                    SELECT DocumentId
                                    FROM [Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                                    WHERE
                                        (BodyHtml LIKE '%translation%'
                                         OR BodyHtml LIKE '%translations%')
                                        OR
                                        (Subheadline LIKE '%translation%'
                                         OR Subheadline LIKE '%translations%')
                                )
                            )

                            UNION

                            SELECT
                                (SELECT DisplayName
                                 FROM [Gcpe.Hub].dbo.Ministry
                                 WHERE Id = MinistryId) AS ministry,
                                [Key],
                                PublishDateTime,
                                ReleaseType
                            FROM [Gcpe.Hub].dbo.NewsRelease nr
                            WHERE HasTranslations = 1
                            AND ReleaseType = 1
                        ) t
                        ORDER BY PublishDateTime")
                    .ToListAsync();

            return newsReleaseEntities;
        }

        public async Task<IEnumerable<NewsReleaseEntity>>
            GetNewsReleasesInDateRangeAsync(
                string startDate = "",
                string endDate = "")
        {
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);

            var startBc = DateTime.SpecifyKind(
                start.Date,
                DateTimeKind.Unspecified);

            var endBc = DateTime.SpecifyKind(
                end.Date,
                DateTimeKind.Unspecified);

            var startInstant = BcTime.FromLocal(startBc);
            var endInstant = BcTime.FromLocal(endBc);

            return await _dbContext.NewsReleaseEntities
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM (
                        SELECT
                            (SELECT DisplayName
                             FROM [Gcpe.Hub].dbo.Ministry
                             WHERE Id = MinistryId) AS ministry,
                            [Key],
                            PublishDateTime,
                            ReleaseType
                        FROM [Gcpe.Hub].dbo.NewsRelease nr
                        WHERE Id IN (
                            SELECT ReleaseId
                            FROM [Gcpe.Hub].dbo.NewsReleaseDocument nrd
                            WHERE nrd.Id IN (
                                SELECT DocumentId
                                FROM [Gcpe.Hub].dbo.NewsReleaseDocumentLanguage nrdl
                                WHERE
                                    (BodyHtml LIKE '%translation%'
                                     OR BodyHtml LIKE '%translations%')
                                    OR
                                    (Subheadline LIKE '%translation%'
                                     OR Subheadline LIKE '%translations%')
                            )
                        )
                        AND nr.PublishDateTime >= {startInstant}
                        AND nr.PublishDateTime < {endInstant}
                        AND nr.IsActive = 1
                        AND nr.IsPublished = 1
                        AND ReleaseType = 1

                        UNION

                        SELECT
                            (SELECT DisplayName
                             FROM [Gcpe.Hub].dbo.Ministry
                             WHERE Id = MinistryId) AS ministry,
                            [Key],
                            PublishDateTime,
                            ReleaseType
                        FROM [Gcpe.Hub].dbo.NewsRelease nr
                        WHERE HasTranslations = 1
                        AND nr.PublishDateTime >= {startInstant}
                        AND nr.PublishDateTime < {endInstant}
                        AND nr.IsActive = 1
                        AND nr.IsPublished = 1
                        AND ReleaseType = 1
                    ) t
                    ORDER BY PublishDateTime")
                .ToListAsync();
        }

        public async Task<string> GetGuidForNewsRelease(string key)
        {
            var release = await _dbContext.NewsRelease
                .FirstOrDefaultAsync(r => r.Key == key);

            return release.Id.ToString();
        }

        public async Task<string> GetDocumentIdForNewsRelease(
            string newsReleaseId)
        {
            var releaseId = Guid.Parse(newsReleaseId);

            var nrd = await _dbContext.NewsReleaseDocument
                .FirstOrDefaultAsync(
                    nrd => nrd.ReleaseId == releaseId);

            return nrd.Id.ToString();
        }

        public async Task<string> GetHeadlineForNewsRelease(
            string documentId)
        {
            var id = Guid.Parse(documentId);

            var nrdl = await _dbContext.NewsReleaseDocumentLanguage
                .FirstOrDefaultAsync(
                    nrdl => nrdl.DocumentId == id);

            return nrdl.Headline ?? "";
        }

        public async Task<List<string>> GetAllMinistriesAsync()
        {
            var allMinistries = await _dbContext.Ministry
                .Where(m => m.IsActive)
                .Select(m => m.DisplayName)
                .ToListAsync();

            return allMinistries
                .Except(excludedMinistries)
                .ToList();
        }
    }
}