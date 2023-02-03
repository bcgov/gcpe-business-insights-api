using Gcpe.Hub.BusinessInsights.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using Gcpe.Hub.BusinessInsights.API.Models;

namespace Gcpe.Hub.BusinessInsights.API.Controllers
{
    [ApiController]
    [Route("api/devops")]
    public class AzureDevOpsController : ControllerBase
    {
        private readonly IAzureDevOpsService _devOpsService;
        private readonly ILogger _logger;
        public AzureDevOpsController(
            ILogger logger,
            IAzureDevOpsService devOpsService
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devOpsService = devOpsService ?? throw new ArgumentNullException(nameof(devOpsService));
        }

        [HttpGet("projects")]
        public async Task<ActionResult> GetProjects()
        {
            await _devOpsService.GetProjects();
            return Ok();
        }

        [HttpGet("work-items")]
        public async Task<ActionResult> GetWorkItems()
        {
            var workItems = await _devOpsService.GetWorkItems();
            var items = workItems.Select(i => new WorkItemViewModel { Id = i.Id, Title = i.Fields["System.Title"] });

            return Ok(items);
        }
    }
}
