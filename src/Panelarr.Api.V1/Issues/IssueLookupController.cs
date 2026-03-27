using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using Panelarr.Http;

namespace Panelarr.Api.V1.Books
{
    [V1ApiController("issue/lookup")]
    public class IssueLookupController : Controller
    {
        private readonly ISearchForNewBook _searchProxy;
        private readonly IMapCoversToLocal _coverMapper;

        public IssueLookupController(ISearchForNewBook searchProxy, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        public object Search(string term)
        {
            var searchResults = _searchProxy.SearchForNewBook(term, null);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<IssueResource> MapToResource(IEnumerable<NzbDrone.Core.Books.Issue> issues)
        {
            foreach (var currentBook in issues)
            {
                var resource = currentBook.ToResource();

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
