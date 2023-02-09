using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gcpe.Hub.BusinessInsights.API.Models
{
    public class WorkItemTypeResponse
    {
        [JsonPropertyName("workItems")]
        public List<WorkItem> WorkItems { get; set; }
    }
}
