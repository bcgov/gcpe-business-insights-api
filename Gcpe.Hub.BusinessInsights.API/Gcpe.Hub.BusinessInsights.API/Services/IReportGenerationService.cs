﻿using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public interface IReportGenerationService
    {
        public TranslationReportDto GenerateMonthlyReport(List<NewsReleaseItem> items, List<string> allMinistries, string environmentName);
        public RollupReportDto GenerateRollupReport(List<NewsReleaseItem> items);
    }
}
