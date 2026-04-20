using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using Panelarr.Api.V1.Issues;
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
        private readonly Logger _logger;

        public SearchController(ISearchForNewEntity searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper, Logger logger)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
            _logger = logger;
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

                if (result is NzbDrone.Core.Issues.Series series)
                {
                    resource.Series = series.ToResource();
                    resource.ForeignId = series.ForeignSeriesId;

                    if (resource.Series.Images != null)
                    {
                        _coverMapper.ConvertToLocalUrls(resource.Series.Id, MediaCoverEntity.Series, resource.Series.Images);

                        var poster = resource.Series.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                        if (poster != null)
                        {
                            resource.Series.RemotePoster = poster.RemoteUrl;
                        }
                    }

                    // For search results not yet in DB, ensure statistics exists
                    if (resource.Series.Statistics == null)
                    {
                        resource.Series.Statistics = new SeriesStatisticsResource();
                    }

                    try
                    {
                        resource.Series.Folder = _fileNameBuilder.GetSeriesFolder(series);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug(ex, "Failed to build folder name for search result {0}", series.Name);
                        resource.Series.Folder = series.Name;
                    }
                }
                else if (result is NzbDrone.Core.Issues.Issue issue)
                {
                    resource.Issue = issue.ToResource();
                    resource.ForeignId = issue.ForeignIssueId;

                    if (issue.Series?.Value != null)
                    {
                        resource.Issue.Series = issue.Series.Value.ToResource();

                        try
                        {
                            resource.Issue.Series.Folder = _fileNameBuilder.GetSeriesFolder(issue.Series);
                        }
                        catch (Exception)
                        {
                            resource.Issue.Series.Folder = issue.Series.Value?.Name;
                        }
                    }

                    if (resource.Issue.Images != null)
                    {
                        _coverMapper.ConvertToLocalUrls(resource.Issue.Id, MediaCoverEntity.Issue, resource.Issue.Images);

                        var cover = resource.Issue.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);

                        if (cover != null)
                        {
                            resource.Issue.RemoteCover = cover.RemoteUrl;
                        }
                    }
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
