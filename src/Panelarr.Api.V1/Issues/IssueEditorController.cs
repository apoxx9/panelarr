using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using Panelarr.Http;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController("issue/editor")]
    public class IssueEditorController : Controller
    {
        private readonly IIssueService _issueService;
        private readonly IManageCommandQueue _commandQueueManager;

        public IssueEditorController(IIssueService issueService, IManageCommandQueue commandQueueManager)
        {
            _issueService = issueService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] IssueEditorResource resource)
        {
            var issuesToUpdate = _issueService.GetIssues(resource.IssueIds);

            foreach (var issue in issuesToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    issue.Monitored = resource.Monitored.Value;
                }
            }

            _issueService.UpdateMany(issuesToUpdate);
            return Accepted(issuesToUpdate.ToResource());
        }

        [HttpDelete]
        public void DeleteIssue([FromBody] IssueEditorResource resource)
        {
            foreach (var issueId in resource.IssueIds)
            {
                _issueService.DeleteIssue(issueId, resource.DeleteFiles ?? false, resource.AddImportListExclusion ?? false);
            }
        }
    }
}
