using System.Threading.Tasks;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IDataSynchronizationService
    {
        public Task SyncData();
        public Task SyncData(string startDate, string endDate);
    }
}
