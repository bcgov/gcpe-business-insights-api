using Gcpe.Hub.BusinessInsights.API.Entities;
using Gcpe.Hub.BusinessInsights.API.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        private readonly Dictionary<string, int> _allMonths = new Dictionary<string, int>()
        {
            { "Jan", 1 },
            { "Feb", 2 },
            { "Mar", 3 },
            { "Apr", 4 },
            { "May", 5 },
            { "Jun", 6 },
            { "Jul", 7 },
            { "Aug", 8 },
            { "Sep", 9 },
            { "Oct", 10 },
            { "Nov", 11 },
            { "Dec", 12 },
        };

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
                if (!ministryFrequency.ContainsKey(ministry))
                {
                    ministryFrequency.Add(ministry, item.Urls.Count());
                    continue;
                }

                ministryFrequency[ministry] += item.Urls.Count();
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
                if (!languageFrequency.ContainsKey(item))
                {
                    languageFrequency.Add(item, 0);
                    continue;
                }
                languageFrequency[item] += 1;
            }

            var bestLangKey = languageFrequency.First().Key;
            var bestLangValue = languageFrequency.First().Value;

            foreach (var p in languageFrequency.Skip(1))
            {
                if (p.Value >= bestLangValue)
                {
                    bestLangKey = p.Key;
                    bestLangValue = p.Value;
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
                    Headline = i.Headline ?? "",
                    Ministry = i.NewsRelease.Ministry,
                    PublishDateTime = i.NewsRelease.PublishDateTime,
                    Urls = i.Urls.Select(u => u.Href).ToList()
                }),
                TranslationsVolumeByMonth = pdfCount,
                ReleasesTranslatedByMinistry = ministryFrequencyResults.OrderByDescending(i => i.Count),
                LanguageCounts = languageFrequencyResults.OrderByDescending(i => i.Count),
                MinistryTranslationsVolume = ministryFrequencyResults.Select(c => c.Count).Sum()
            };

            return report;
        }

        public RollupReportDto GenerateRollupReport(List<NewsReleaseWithUrls> items)
        {
            var dates = items.Select(i => i.NewsRelease.PublishDateTime).Select(d => new { d.Month, d.Year }).Distinct().ToList();

            var dateRanges = dates.Select(d => new
            {
                month = new DateTime(d.Year, d.Month, 1).ToString("MMM", CultureInfo.InvariantCulture),
                year = d.Year,
                start = new DateTime(d.Year, d.Month, 1).ToString("yyyy-MM-dd"),
                end = new DateTime(d.Year, d.Month, 1).AddMonths(1).ToString("yyyy-MM-dd")
            });

            List<MonthWithPdfCount> monthsWithPdfCounts = new List<MonthWithPdfCount>();
            foreach (var range in dateRanges)
            {
                var itemsInRange = items
                    .Where(i => i.NewsRelease.PublishDateTime >= DateTimeOffset.Parse(range.start)
                        && i.NewsRelease.PublishDateTime <= DateTimeOffset.Parse(range.end)).ToList();

                var month = $"{range.month}-{range.year % 100}";

                var pdfs = 0;
                foreach (var item in itemsInRange)
                {
                    foreach (var doc in item.Urls)
                    {
                        pdfs++;
                    }
                }

                var monthWithPdfCount = new MonthWithPdfCount { Month = month, PdfCount = pdfs };
                monthsWithPdfCounts.Add(monthWithPdfCount);
            }
            monthsWithPdfCounts = monthsWithPdfCounts.OrderBy(i => _allMonths[i.Month.Substring(0, i.Month.IndexOf('-'))]).ToList();

            // add months without data
            for (var idx = monthsWithPdfCounts.Count + 1; idx <= 12; idx++)
            {
                var date = new DateTime(DateTime.Now.Year, idx, 1);
                var month = date.ToString("MMM", CultureInfo.InvariantCulture);
                var year = date.ToString("yy");
                monthsWithPdfCounts.Add(new MonthWithPdfCount { Month = $"{month}-{year}", PdfCount = 0 });
            }

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
                if (!ministryFrequency.ContainsKey(ministry))
                {
                    ministryFrequency.Add(ministry, item.Urls.Count());
                    continue;
                }

                ministryFrequency[ministry] += item.Urls.Count();
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
                monthsWithPdfCounts = monthsWithPdfCounts,
                Year = items.FirstOrDefault().NewsRelease.PublishDateTime.Year.ToString(),
                NewsReleaseVolumeByMonth = items.Count(),
                TranslationsVolumeByMonth = pdfCount,
                ReleasesTranslatedByMinistry = ministryFrequencyResults.OrderByDescending(i => i.Count),
                LanguageCounts = languageFrequencyResults.OrderByDescending(i => i.Count),
                MinistryTranslationsVolume = ministryFrequencyResults.Select(c => c.Count).Sum()
            };

            return report;
        }
    }
}
