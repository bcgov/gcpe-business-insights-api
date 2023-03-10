using System;
using System.Collections.Generic;

namespace Gcpe.Hub.BusinessInsights.API.Models
{
    public class Translation
    {
        public string Key { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Ministry { get; set; } = string.Empty;
        public List<string> Urls { get; set; } = new List<string>();
        public DateTimeOffset PublishDateTime { get; set; }
    }
}
