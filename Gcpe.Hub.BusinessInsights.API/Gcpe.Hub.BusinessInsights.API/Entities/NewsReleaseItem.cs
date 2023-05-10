using Gcpe.Hub.BusinessInsights.API.Models;
using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Entities
{
    public class NewsReleaseItem : NewsReleaseEntity
    {
        public int Id { get; set; }
        public IList<Models.Url> Urls { get; set; } = new List<Models.Url>();
        public string Headline { get; set; } = string.Empty;
    }
}
