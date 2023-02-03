using Gcpe.Hub.BusinessInsights.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IAzureDevOpsService
    {
        public Task GetProjects();
        public Task<List<WorkItem>> GetWorkItems();
    }
}
