using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using Panelarr.Http;

namespace Panelarr.Api.V1.Series
{
    [V1ApiController("series/lookup")]
    public class SeriesLookupController : Controller
    {
        private readonly ISearchForNewSeries _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly Logger _logger;

        public SeriesLookupController(ISearchForNewSeries searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper, Logger logger)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
            _coverMapper = coverMapper;
            _logger = logger;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewSeries(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<SeriesResource> MapToResource(IEnumerable<NzbDrone.Core.Issues.Series> seriesList)
        {
            foreach (var currentSeries in seriesList)
            {
                var resource = currentSeries.ToResource();

                if (resource.Images != null)
                {
                    _coverMapper.ConvertToLocalUrls(resource.Id, MediaCoverEntity.Series, resource.Images);

                    var poster = resource.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

                    if (poster != null)
                    {
                        resource.RemotePoster = poster.RemoteUrl;
                    }
                }

                try
                {
                    resource.Folder = _fileNameBuilder.GetSeriesFolder(currentSeries);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Failed to build folder name for search result {0}", currentSeries.Name);
                    resource.Folder = currentSeries.Name;
                }

                yield return resource;
            }
        }
    }
}
