using System;

namespace Gcpe.Hub.BusinessInsights.API.Models
{
    public class Url
    {
        public int Id { get; set; }
        public string Href { get; set; }
        public DateTimeOffset PublishDateTime { get; set; }
    }
}
