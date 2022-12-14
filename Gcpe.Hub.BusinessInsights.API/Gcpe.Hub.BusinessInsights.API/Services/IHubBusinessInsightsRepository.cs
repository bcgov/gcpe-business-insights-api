using Gcpe.Hub.BusinessInsights.API.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IHubBusinessInsightsRepository
    {
        Task<IEnumerable<NewsReleaseEntity>> GetNewsReleasesAsync();
        Task<IEnumerable<NewsReleaseEntity>> GetNewsReleasesAsync(string startDate, string endDate);
    }
}
