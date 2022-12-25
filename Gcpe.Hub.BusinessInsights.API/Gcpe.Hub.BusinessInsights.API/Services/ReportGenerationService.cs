using Gcpe.Hub.BusinessInsights.API.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class ReportGenerationService : IReportGenerationService
    {
        public TranslationReportDto GenerateMonthlyReport(IEnumerable<Translation> translationItems)
        {
            var pdfCount = 0;
            foreach (var item in translationItems)
            {
                foreach (var doc in item.Urls)
                {
                    pdfCount++;
                }
            }

            var ministryFrequency = new Dictionary<string, int>();
            foreach (var item in translationItems)
            {
                var ministry = item.Ministry;

                if (!ministryFrequency.ContainsKey(ministry)) ministryFrequency.Add(ministry, 0);
                ministryFrequency[ministry] += 1;
            }

            var bestKey = ministryFrequency.First().Key;
            var bestValue = ministryFrequency.First().Value;

            foreach (var p in ministryFrequency.Skip(1))
            {
                if (p.Value >= bestValue)
                {
                    bestKey = p.Key;
                    bestValue = p.Value;
                }
            }

            var ministryFrequencyResults = new List<TranslationReportDto.MinistryFrequencyItem>();

            foreach (var item in ministryFrequency)
            {
                ministryFrequencyResults.Add(new TranslationReportDto.MinistryFrequencyItem { Ministry = item.Key, Count = item.Value });
            }

            var languages = new List<string>();
            foreach (var item in translationItems)
            {
                foreach (var doc in item.Urls)
                {

                    MatchCollection mc
                    = Regex.Matches(doc, "Arabic|Chinese|Farsi|French|Hebrew|Hindi|Indonesian|Japanese|Korean|Punjabi|Somali|Spanish|Swahili|Tagalog|Ukrainian|Urdu|Vietnamese");

                    foreach (Match m in mc)
                    {
                        languages.Add(m.ToString());
                    }

                }
            }

            var languageFrequency = new Dictionary<string, int>();

            foreach (var item in languages)
            {
                if (!languageFrequency.ContainsKey(item)) languageFrequency.Add(item, 0);
                languageFrequency[item] += 1;
            }

            var bestLangKey = languageFrequency.First().Key;
            var bestLangValue = languageFrequency.First().Value;

            foreach (var p in languageFrequency.Skip(1))
            {
                if (p.Value >= bestValue)
                {
                    bestKey = p.Key;
                    bestValue = p.Value;
                }
            }

            var languageFrequencyResults = new List<TranslationReportDto.LanguageFrequencyItem>();

            foreach (var item in languageFrequency)
            {
                languageFrequencyResults.Add(new TranslationReportDto.LanguageFrequencyItem { Language = item.Key, Count = item.Value });
            }

            var report = new TranslationReportDto
            {
                MonthName = translationItems.FirstOrDefault().PublishDateTime.ToString("MMMM"),
                NewsReleaseVolumeByMonth = translationItems.Count(),
                Translations = translationItems.OrderBy(ti => ti.Ministry),
                TranslationsVolumeByMonth = pdfCount,
                ReleasesTranslatedByMinistry = ministryFrequencyResults.OrderByDescending(i => i.Count),
                LanguageCounts = languageFrequencyResults.OrderByDescending(i => i.Count)
            };

            return report;
        }

        public RollupReportDto GenerateRollupReport(IEnumerable<Translation> translationItems)
        {
            var pdfCount = 0;
            foreach (var item in translationItems)
            {
                foreach (var doc in item.Urls)
                {
                    pdfCount++;
                }
            }

            var ministryFrequency = new Dictionary<string, int>();
            foreach (var item in translationItems)
            {
                var ministry = item.Ministry;

                if (!ministryFrequency.ContainsKey(ministry)) ministryFrequency.Add(ministry, 0);
                ministryFrequency[ministry] += 1;
            }

            var bestKey = ministryFrequency.First().Key;
            var bestValue = ministryFrequency.First().Value;

            foreach (var p in ministryFrequency.Skip(1))
            {
                if (p.Value >= bestValue)
                {
                    bestKey = p.Key;
                    bestValue = p.Value;
                }
            }

            var ministryFrequencyResults = new List<RollupReportDto.MinistryFrequencyItem>();

            foreach (var item in ministryFrequency)
            {
                ministryFrequencyResults.Add(new RollupReportDto.MinistryFrequencyItem { Ministry = item.Key, Count = item.Value });
            }

            var languages = new List<string>();
            foreach (var item in translationItems)
            {
                foreach (var doc in item.Urls)
                {

                    MatchCollection mc
                    = Regex.Matches(doc, "Arabic|Chinese|Farsi|French|Hebrew|Hindi|Indonesian|Japanese|Korean|Punjabi|Somali|Spanish|Swahili|Tagalog|Ukrainian|Urdu|Vietnamese");

                    foreach (Match m in mc)
                    {
                        languages.Add(m.ToString());
                    }

                }
            }

            var languageFrequency = new Dictionary<string, int>();

            foreach (var item in languages)
            {
                if (!languageFrequency.ContainsKey(item)) languageFrequency.Add(item, 0);
                languageFrequency[item] += 1;
            }

            var bestLangKey = languageFrequency.First().Key;
            var bestLangValue = languageFrequency.First().Value;

            foreach (var p in languageFrequency.Skip(1))
            {
                if (p.Value >= bestValue)
                {
                    bestKey = p.Key;
                    bestValue = p.Value;
                }
            }

            var languageFrequencyResults = new List<RollupReportDto.LanguageFrequencyItem>();

            foreach (var item in languageFrequency)
            {
                languageFrequencyResults.Add(new RollupReportDto.LanguageFrequencyItem { Language = item.Key, Count = item.Value });
            }

            var report = new RollupReportDto
            {
                NewsReleaseVolumeByMonth = translationItems.Count(),
                TranslationsVolumeByMonth = pdfCount,
                ReleasesTranslatedByMinistry = ministryFrequencyResults.OrderByDescending(i => i.Count),
                LanguageCounts = languageFrequencyResults.OrderByDescending(i => i.Count)
            };

            return report;
        }
    }
}
