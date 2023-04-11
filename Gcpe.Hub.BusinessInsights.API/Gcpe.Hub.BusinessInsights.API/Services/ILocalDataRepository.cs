using Gcpe.Hub.BusinessInsights.API.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface ILocalDataRepository
    {
        NewsReleaseItem AddNewsReleaseEntity(NewsReleaseEntity newsReleaseEntity);
        Task<IEnumerable<NewsReleaseItem>> GetAllNewsReleaseItems(bool includeUrls = false);
        Task<IEnumerable<NewsReleaseItem>> GetNewsReleasesForPreviousMonthAsync();
        Task<IEnumerable<Url>> GetUrlsForPreviousMonthAsync();
        void DeleteNewsReleasesAsync(IEnumerable<NewsReleaseItem> newsReleaseItems);
        Task<IEnumerable<NewsReleaseItem>> GetNewsReleaseItemsInDateRangeAsync(string startDate, string endDate, bool includeUrls = false);
        void AddUrl(Url url);
        Task<IEnumerable<Url>> GetUrlsForNewsRelease(int NewsReleaseItemId);
        Task<bool> SaveChangesAsync();
    }
}
