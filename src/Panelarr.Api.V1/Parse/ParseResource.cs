using System.Collections.Generic;
using NzbDrone.Core.Parser.Model;
using Panelarr.Api.V1.Issues;
using Panelarr.Api.V1.Series;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Parse
{
    public class ParseResource : RestResource
    {
        public string Title { get; set; }
        public ParsedIssueInfo ParsedIssueInfo { get; set; }
        public SeriesResource Series { get; set; }
        public List<IssueResource> Issues { get; set; }
    }
}
