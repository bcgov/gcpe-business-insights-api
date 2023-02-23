using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Entities
{
    public class NewsReleaseItem : NewsReleaseEntity
    {
        public int Id { get; set; }
        public IList<Url> Urls { get; set; } = new List<Url>();
    }
}
