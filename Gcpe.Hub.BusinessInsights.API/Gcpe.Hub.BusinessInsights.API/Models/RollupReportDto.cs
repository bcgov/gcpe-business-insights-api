﻿using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Models
{
    public class RollupReportDto
    {
        public List<MonthWithPdfCount> monthsWithPdfCounts { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public int NewsReleaseVolumeByMonth { get; set; }
        public int TranslationsVolumeByMonth { get; set; }
        public int MinistryTranslationsVolume { get; set; }
        public IEnumerable<MinistryFrequencyItem> ReleasesTranslatedByMinistry { get; set; }
        public IEnumerable<LanguageFrequencyItem> LanguageCounts { get; set; }
        public class MinistryFrequencyItem
        {
            public string Ministry { get; set; }
            public int Count { get; set; }
        }

        public class LanguageFrequencyItem
        {
            public string Language { get; set; }
            public int Count { get; set; }
        }
    }
}
