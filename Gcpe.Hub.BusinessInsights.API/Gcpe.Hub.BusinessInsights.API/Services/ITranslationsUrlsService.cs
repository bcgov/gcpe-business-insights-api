using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Gcpe.Hub.BusinessInsights.API.Models;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface ITranslationsUrlsService
    {
        Task<IEnumerable<Translation>> GetTranslationsFor(string key, CancellationToken cancellationToken);
    }
}
