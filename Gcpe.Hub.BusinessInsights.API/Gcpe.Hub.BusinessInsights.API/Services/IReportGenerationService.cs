using Gcpe.Hub.BusinessInsights.API.Models;
using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IReportGenerationService
    {
        public TranslationReportDto GenerateMonthlyReport(IEnumerable<Translation> translationItems);
        public RollupReportDto GenerateRollupReport(IEnumerable<Translation> translationItems);

    }
}
