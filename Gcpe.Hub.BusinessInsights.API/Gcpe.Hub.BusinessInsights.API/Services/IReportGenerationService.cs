using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
using System;
using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IReportGenerationService
    {
        public TranslationReportDto GenerateMonthlyReport(List<NewsReleaseWithUrls> items);
        public RollupReportDto GenerateRollupReport(List<NewsReleaseWithUrls> items);

    }
}
