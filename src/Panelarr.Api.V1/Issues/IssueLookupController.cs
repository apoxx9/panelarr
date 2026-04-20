using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using Panelarr.Http;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController("issue/lookup")]
    public class IssueLookupController : Controller
    {
        private readonly ISearchForNewIssue _searchProxy;
        private readonly IMapCoversToLocal _coverMapper;

        public IssueLookupController(ISearchForNewIssue searchProxy, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        public object Search(string term)
        {
            var searchResults = _searchProxy.SearchForNewIssue(term, null);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<IssueResource> MapToResource(IEnumerable<NzbDrone.Core.Issues.Issue> issues)
        {
            foreach (var currentIssue in issues)
            {
                var resource = currentIssue.ToResource();

                _coverMapper.ConvertToLocalUrls(resource.Id, MediaCoverEntity.Issue, resource.Images);

                var cover = resource.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

                if (cover != null)
                {
                    resource.RemoteCover = cover.RemoteUrl;
                }

                yield return resource;
            }
        }
    }
}
