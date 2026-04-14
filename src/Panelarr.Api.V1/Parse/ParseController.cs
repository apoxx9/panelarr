using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using Panelarr.Api.V1.Issues;
using Panelarr.Api.V1.Series;
using Panelarr.Http;

namespace Panelarr.Api.V1.Parse
{
    [V1ApiController]
    public class ParseController : Controller
    {
        private readonly IParsingService _parsingService;

        public ParseController(IParsingService parsingService)
        {
            _parsingService = parsingService;
        }

        [HttpGet]
        public ParseResource Parse(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsedIssueInfo = Parser.ParseIssueTitle(title);

            if (parsedIssueInfo == null)
            {
                return new ParseResource
                {
                    Title = title
                };
            }

            var remoteIssue = _parsingService.Map(parsedIssueInfo);

            if (remoteIssue != null)
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedIssueInfo = remoteIssue.ParsedIssueInfo,
                    Series = remoteIssue.Series.ToResource(),
                    Issues = remoteIssue.Issues.ToResource()
                };
            }
            else
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedIssueInfo = parsedIssueInfo
                };
            }
        }
    }
}
