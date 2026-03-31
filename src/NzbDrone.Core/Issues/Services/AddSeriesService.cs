using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Issues
{
    public interface IAddSeriesService
    {
        Series AddSeries(Series newSeries, bool doRefresh = true);
        List<Series> AddSeries(List<Series> newSeriesList, bool doRefresh = true);
    }

    public class AddSeriesService : IAddSeriesService
    {
        private readonly ISeriesService _seriesService;
        private readonly ISeriesMetadataService _seriesMetadataService;
        private readonly IProvideSeriesInfo _seriesInfo;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IAddSeriesValidator _addSeriesValidator;
        private readonly Logger _logger;

        public AddSeriesService(ISeriesService seriesService,
                                ISeriesMetadataService seriesMetadataService,
                                IProvideSeriesInfo seriesInfo,
                                IBuildFileNames fileNameBuilder,
                                IAddSeriesValidator addSeriesValidator,
                                Logger logger)
        {
            _seriesService = seriesService;
            _seriesMetadataService = seriesMetadataService;
            _seriesInfo = seriesInfo;
            _fileNameBuilder = fileNameBuilder;
            _addSeriesValidator = addSeriesValidator;
            _logger = logger;
        }

        public Series AddSeries(Series newSeries, bool doRefresh = true)
        {
            Ensure.That(newSeries, () => newSeries).IsNotNull();

            newSeries = AddSkyhookData(newSeries);
            newSeries = SetPropertiesAndValidate(newSeries);

            _logger.Info("Adding Series {0} Path: [{1}]", newSeries, newSeries.Path);

            // add metadata
            _seriesMetadataService.Upsert(newSeries.Metadata.Value);
            newSeries.SeriesMetadataId = newSeries.Metadata.Value.Id;

            // add the series itself
            return _seriesService.AddSeries(newSeries, doRefresh);
        }

        public List<Series> AddSeries(List<Series> newSeriesList, bool doRefresh = true)
        {
            var added = DateTime.UtcNow;
            var seriesToAdd = new List<Series>();

            foreach (var s in newSeriesList)
            {
                try
                {
                    var series = AddSkyhookData(s);
                    series = SetPropertiesAndValidate(series);
                    series.Added = added;
                    seriesToAdd.Add(series);
                }
                catch (Exception ex)
                {
                    // Catch Import Errors for now until we get things fixed up
                    _logger.Error(ex, "Failed to import id: {0} - {1}", s.Metadata.Value.ForeignSeriesId, s.Metadata.Value.Name);
                }
            }

            // add metadata
            _seriesMetadataService.UpsertMany(seriesToAdd.Select(x => x.Metadata.Value).ToList());
            seriesToAdd.ForEach(x => x.SeriesMetadataId = x.Metadata.Value.Id);

            return _seriesService.AddSeries(seriesToAdd, doRefresh);
        }

        private Series AddSkyhookData(Series newSeries)
        {
            Series series;

            try
            {
                series = _seriesInfo.GetSeriesInfo(newSeries.Metadata.Value.ForeignSeriesId, false);
            }
            catch (SeriesNotFoundException)
            {
                _logger.Error("PanelarrId {0} was not found, it may have been removed from the metadata provider.", newSeries.Metadata.Value.ForeignSeriesId);

                throw new ValidationException(new List<ValidationFailure>
                {
                    new ("ForeignSeriesId", "An series with this ID was not found", newSeries.Metadata.Value.ForeignSeriesId)
                });
            }

            series.ApplyChanges(newSeries);

            return series;
        }

        private Series SetPropertiesAndValidate(Series newSeries)
        {
            var path = newSeries.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                var folderName = _fileNameBuilder.GetSeriesFolder(newSeries);
                path = Path.Combine(newSeries.RootFolderPath, folderName);
            }

            // Disambiguate series path if it exists already
            if (_seriesService.SeriesPathExists(path))
            {
                if (newSeries.Metadata.Value.Disambiguation.IsNotNullOrWhiteSpace())
                {
                    path += $" ({newSeries.Metadata.Value.Disambiguation})";
                }

                if (_seriesService.SeriesPathExists(path))
                {
                    var basepath = path;
                    var i = 0;
                    do
                    {
                        i++;
                        path = basepath + $" ({i})";
                    }
                    while (_seriesService.SeriesPathExists(path));
                }
            }

            newSeries.Path = path;
            newSeries.CleanName = newSeries.Metadata.Value.Name.CleanSeriesName();
            newSeries.Added = DateTime.UtcNow;

            if (newSeries.AddOptions != null && newSeries.AddOptions.Monitor == MonitorTypes.None)
            {
                newSeries.Monitored = false;
            }

            var validationResult = _addSeriesValidator.Validate(newSeries);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            return newSeries;
        }
    }
}
