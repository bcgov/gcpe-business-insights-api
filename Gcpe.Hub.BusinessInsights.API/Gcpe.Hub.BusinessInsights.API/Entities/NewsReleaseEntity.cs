using Microsoft.EntityFrameworkCore.Metadata;
using System;

namespace Gcpe.Hub.BusinessInsights.API.Entities
{
    public class NewsReleaseEntity
    {
        public string Ministry { get; set; }
        public string Key { get; set; }
        public int ReleaseType { get; set; }
        public DateTimeOffset PublishDateTime { get; set; }
    }
}
