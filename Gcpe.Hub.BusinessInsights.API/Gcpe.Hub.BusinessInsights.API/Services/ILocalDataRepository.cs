using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface ILocalDataRepository
    {
        void AddNewsReleaseEntityAsync(NewsReleaseEntity newsReleaseEntity);
        void AddTranslationItemAsync(TranslationItem translationItem);
        Task<IEnumerable<NewsReleaseItem>> GetNewsReleasesForPreviousMonthAsync();
        Task<IEnumerable<TranslationItem>> GetTranslationsForPreviousMonthAsync();
        Task<IEnumerable<Url>> GetUrlsForPreviousMonthAsync();
        void RemoveResultsForPreviousMonthAsync(IEnumerable<NewsReleaseItem> newsReleaseItems, IEnumerable<TranslationItem> translationItems, IEnumerable<Url> urls);
        Task<IEnumerable<Translation>> GetTranslationItemsAsync();
        Task<IEnumerable<Translation>> GetTranslationItemsInDateRangeAsync(string startDate, string endDate);
        Task<IEnumerable<Translation>> GetTranslationItemsInDateRangeAsync();
        Task<IEnumerable<NewsReleaseItem>> GetNewsReleaseItemsInDateRangeAsync(string startDate, string endDate);
        Task<bool> SaveChangesAsync();
    }
}
