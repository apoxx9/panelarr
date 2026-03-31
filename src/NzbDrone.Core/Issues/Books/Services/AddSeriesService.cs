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
        private readonly ISeriesService _authorService;
        private readonly ISeriesMetadataService _authorMetadataService;
        private readonly IProvideSeriesInfo _authorInfo;
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IAddSeriesValidator _addSeriesValidator;
        private readonly Logger _logger;

        public AddSeriesService(ISeriesService authorService,
                                ISeriesMetadataService authorMetadataService,
                                IProvideSeriesInfo authorInfo,
                                IBuildFileNames fileNameBuilder,
                                IAddSeriesValidator addSeriesValidator,
                                Logger logger)
        {
            _authorService = authorService;
            _authorMetadataService = authorMetadataService;
            _authorInfo = authorInfo;
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
            _authorMetadataService.Upsert(newSeries.Metadata.Value);
            newSeries.SeriesMetadataId = newSeries.Metadata.Value.Id;

            // add the author itself
            return _authorService.AddSeries(newSeries, doRefresh);
        }

        public List<Series> AddSeries(List<Series> newSeriesList, bool doRefresh = true)
        {
            var added = DateTime.UtcNow;
            var authorsToAdd = new List<Series>();

            foreach (var s in newSeriesList)
            {
                try
                {
                    var author = AddSkyhookData(s);
                    author = SetPropertiesAndValidate(author);
                    author.Added = added;
                    authorsToAdd.Add(author);
                }
                catch (Exception ex)
                {
                    // Catch Import Errors for now until we get things fixed up
                    _logger.Error(ex, "Failed to import id: {0} - {1}", s.Metadata.Value.ForeignSeriesId, s.Metadata.Value.Name);
                }
            }

            // add metadata
            _authorMetadataService.UpsertMany(authorsToAdd.Select(x => x.Metadata.Value).ToList());
            authorsToAdd.ForEach(x => x.SeriesMetadataId = x.Metadata.Value.Id);

            return _authorService.AddSeries(authorsToAdd, doRefresh);
        }

        private Series AddSkyhookData(Series newSeries)
        {
            Series author;

            try
            {
                author = _authorInfo.GetSeriesInfo(newSeries.Metadata.Value.ForeignSeriesId, false);
            }
            catch (SeriesNotFoundException)
            {
                _logger.Error("PanelarrId {0} was not found, it may have been removed from the metadata provider.", newSeries.Metadata.Value.ForeignSeriesId);

                throw new ValidationException(new List<ValidationFailure>
                {
                    new ("ForeignSeriesId", "An author with this ID was not found", newSeries.Metadata.Value.ForeignSeriesId)
                });
            }

            author.ApplyChanges(newSeries);

            return author;
        }

        private Series SetPropertiesAndValidate(Series newSeries)
        {
            var path = newSeries.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                var folderName = _fileNameBuilder.GetSeriesFolder(newSeries);
                path = Path.Combine(newSeries.RootFolderPath, folderName);
            }

            // Disambiguate author path if it exists already
            if (_authorService.SeriesPathExists(path))
            {
                if (newSeries.Metadata.Value.Disambiguation.IsNotNullOrWhiteSpace())
                {
                    path += $" ({newSeries.Metadata.Value.Disambiguation})";
                }

                if (_authorService.SeriesPathExists(path))
                {
                    var basepath = path;
                    var i = 0;
                    do
                    {
                        i++;
                        path = basepath + $" ({i})";
                    }
                    while (_authorService.SeriesPathExists(path));
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
