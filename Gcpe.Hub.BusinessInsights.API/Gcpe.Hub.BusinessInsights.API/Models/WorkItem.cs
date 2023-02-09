using System.Text.Json.Serialization;

namespace Gcpe.Hub.BusinessInsights.API.Models
{
    public class WorkItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        public dynamic Fields { get; set; }
    }
}
