using Gcpe.Hub.BusinessInsights.API.Entities;
using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Models
{
    public class NewsReleaseWithUrls
    {
        public NewsReleaseItem NewsRelease { get; set; }
        public string Headline { get; set; } = string.Empty;
        public IEnumerable<Url> Urls { get; set; }
        public NewsReleaseWithUrls(NewsReleaseItem newsRelease, IEnumerable<Url> urls)
        {
            NewsRelease = newsRelease;
            Urls = urls;
        }
    }
}
