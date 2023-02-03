using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gcpe.Hub.BusinessInsights.API.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Gcpe.Hub.BusinessInsights.API.Services
{
    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly IConfiguration _config;
        public AzureDevOpsService(IConfiguration config)
        {
            _config = config;
        }

        public async Task GetProjects()
        {
            try
            {
                var personalaccesstoken = _config["AzureDevopsAccessToken"];

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            System.Text.Encoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", personalaccesstoken))));

                    using (HttpResponseMessage response = await client.GetAsync(
                                "https://dev.azure.com/gcpe/_apis/projects"))
                    {
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine(responseBody);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task<List<WorkItem>> GetWorkItems()
        {
            var personalAccessToken = _config["AzureDevopsAccessToken"];

            var workItems = await GetWorkItems("https://dev.azure.com/gcpe", personalAccessToken, "ops");

            return workItems.Where(i => i.Fields != null).ToList();
        }

        private async Task<List<WorkItem>> GetWorkItems(string organizationUrl, string personalAccessToken, string projectName)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", personalAccessToken))));


            var str = "{\"query\": \"Select [System.Id], [System.Title], [System.State] From WorkItems Where [System.WorkItemType] = 'Task'\"}";

            var content = new StringContent(str, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{organizationUrl}/{projectName}/_apis/wit/wiql?api-version=5.1", content);

            var json = await response.Content.ReadAsStringAsync();
            var workItemTypeResponse = JsonSerializer.Deserialize<WorkItemTypeResponse>(json);

            // add additional fields to this array
            var fields = new List<string>();
            fields.Add("System.Title");

            // Send a request to get the work item details
            response = client.PostAsync($"{organizationUrl}/{projectName}/_apis/wit/workitemsbatch?api-version=5.1",
                new StringContent(JsonSerializer.Serialize(new { ids = workItemTypeResponse.WorkItems.Take(10).Select(x => x.Id), fields = fields.Select(x => x) }), Encoding.UTF8, "application/json")).Result;
            json = await response.Content.ReadAsStringAsync();
            dynamic items = JsonConvert.DeserializeObject(json); // only newtonsoft.json worked here

            // Add the details to the work items
            foreach (var item in items.value)
            {
                workItemTypeResponse.WorkItems.First(x => x.Id == (int)item.id).Fields = item.fields;
            }

            return workItemTypeResponse.WorkItems;
        }
    }
}

