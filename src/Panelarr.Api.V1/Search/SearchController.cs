using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using Panelarr.Api.V1.Books;
using Panelarr.Api.V1.Series;
using Panelarr.Http;

namespace Panelarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNewEntity _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;

        public SearchController(ISearchForNewEntity searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewEntity(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<SearchResource> MapToResource(IEnumerable<object> results)
        {
            var id = 1;
            foreach (var result in results)
            {
                var resource = new SearchResource();
                resource.Id = id++;

                if (result is NzbDrone.Core.Books.Series author)
                {
                    resource.Series = author.ToResource();
                    resource.ForeignId = author.ForeignSeriesId;

                    _coverMapper.ConvertToLocalUrls(resource.Series.Id, MediaCoverEntity.Series, resource.Series.Images);

                    var poster = resource.Series.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                    if (poster != null)
                    {
                        resource.Series.RemotePoster = poster.RemoteUrl;
                    }

                    resource.Series.Folder = _fileNameBuilder.GetSeriesFolder(author);
                }
                else if (result is NzbDrone.Core.Books.Issue issue)
                {
                    resource.Issue = issue.ToResource();
                    resource.Issue.Series = issue.Series.Value.ToResource();
                    resource.ForeignId = issue.ForeignIssueId;

                    _coverMapper.ConvertToLocalUrls(resource.Issue.Id, MediaCoverEntity.Issue, resource.Issue.Images);

                    var cover = resource.Issue.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

                    if (cover != null)
                    {
                        resource.Issue.RemoteCover = cover.RemoteUrl;
                    }

                    resource.Issue.Series.Folder = _fileNameBuilder.GetSeriesFolder(issue.Series);
                }
                else
                {
                    throw new NotImplementedException("Bad response from search all proxy");
                }

                yield return resource;
            }
        }
    }
}
