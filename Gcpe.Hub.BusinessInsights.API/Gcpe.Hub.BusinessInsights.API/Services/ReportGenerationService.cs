using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class ReportGenerationService : IReportGenerationService
    {
        private readonly List<string> _allLanguages = new List<string>()
        {
            "Arabic", "Chinese(traditional)", "Chinese(simplified)", "Farsi", "French", "Hebrew", "Hindi", "Indonesian", "Japanese", "Korean", "Punjabi", "Somali", "Spanish", "Swahili", "Tagalog", "Ukrainian", "Urdu", "Vietnamese"
        };

        private readonly string _languages = @"Arabic|Chinese\(traditional\)|Chinese\(simplified\)|Chinese_Simplified|Chinese_Traditional|Farsi|French|Hebrew|Hindi|Indonesian|Japanese|Korean|Punjabi|Somali|Spanish|Swahili|Tagalog|Ukrainian|Urdu|Vietnamese";

        public List<string> AllLanguages
        {
            get { return _allLanguages; }
        }

        public TranslationReportDto GenerateMonthlyReport(List<NewsReleaseWithUrls> items)
        {
            var pdfCount = 0;
            foreach (var item in items)
            {
                foreach (var doc in item.Urls)
                {
                    pdfCount++;
                }
            }

            var ministryFrequency = new Dictionary<string, int>();
            foreach (var item in items)
            {
                var ministry = item.NewsRelease.Ministry;

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
            foreach (var item in items)
            {
                foreach (var doc in item.Urls)
                {

                    MatchCollection mc
                    = Regex.Matches(doc.Href, _languages);

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

            var noLanguageResults = _allLanguages.Except(languages);

            noLanguageResults.ToList().ForEach(l => languageFrequencyResults.Add(new TranslationReportDto.LanguageFrequencyItem { Language = l, Count = 0 }));

            var report = new TranslationReportDto
            {
                MonthName = items.FirstOrDefault().NewsRelease.PublishDateTime.ToString("MMMM"),
                Year = items.FirstOrDefault().NewsRelease.PublishDateTime.Year.ToString(),
                NewsReleaseVolumeByMonth = items.Count,
                Translations = items.OrderBy(i => i.NewsRelease.Key)
                .Select(i => new Translation
                {
                    Key = i.NewsRelease.Key,
                    Ministry = i.NewsRelease.Ministry,
                    PublishDateTime = i.NewsRelease.PublishDateTime,
                    Urls = i.Urls.Select(u => u.Href).ToList()
                }),
                TranslationsVolumeByMonth = pdfCount,
                ReleasesTranslatedByMinistry = ministryFrequencyResults.OrderByDescending(i => i.Count),
                LanguageCounts = languageFrequencyResults.OrderByDescending(i => i.Count)
            };

            return report;
        }

        public RollupReportDto GenerateRollupReport(List<NewsReleaseWithUrls> items)
        {
            var pdfCount = 0;
            foreach (var item in items)
            {
                foreach (var doc in item.Urls)
                {
                    pdfCount++;
                }
            }

            var ministryFrequency = new Dictionary<string, int>();
            foreach (var item in items)
            {
                var ministry = item.NewsRelease.Ministry;

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
            foreach (var item in items)
            {
                foreach (var doc in item.Urls)
                {

                    MatchCollection mc
                    = Regex.Matches(doc.Href, _languages);

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

            var noLanguageResults = _allLanguages.Except(languages);

            noLanguageResults.ToList().ForEach(l => languageFrequencyResults.Add(new RollupReportDto.LanguageFrequencyItem { Language = l, Count = 0 }));

            var report = new RollupReportDto
            {
                Year = items.FirstOrDefault().NewsRelease.PublishDateTime.Year.ToString(),
                NewsReleaseVolumeByMonth = items.Count(),
                TranslationsVolumeByMonth = pdfCount,
                ReleasesTranslatedByMinistry = ministryFrequencyResults.OrderByDescending(i => i.Count),
                LanguageCounts = languageFrequencyResults.OrderByDescending(i => i.Count)
            };

            return report;
        }
    }
}
