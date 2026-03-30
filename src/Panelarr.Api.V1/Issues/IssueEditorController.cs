using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.Messaging.Commands;
using Panelarr.Http;

namespace Panelarr.Api.V1.Books
{
    [V1ApiController("issue/editor")]
    public class IssueEditorController : Controller
    {
        private readonly IIssueService _bookService;
        private readonly IManageCommandQueue _commandQueueManager;

        public IssueEditorController(IIssueService bookService, IManageCommandQueue commandQueueManager)
        {
            _bookService = bookService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] IssueEditorResource resource)
        {
            var booksToUpdate = _bookService.GetIssues(resource.IssueIds);

            foreach (var issue in booksToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    issue.Monitored = resource.Monitored.Value;
                }
            }

            _bookService.UpdateMany(booksToUpdate);
            return Accepted(booksToUpdate.ToResource());
        }

        [HttpDelete]
        public void DeleteBook([FromBody] IssueEditorResource resource)
        {
            foreach (var bookId in resource.IssueIds)
            {
                _bookService.DeleteIssue(bookId, resource.DeleteFiles ?? false, resource.AddImportListExclusion ?? false);
            }
        }
    }
}
