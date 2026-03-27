using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IFilenameSampleService
    {
        SampleResult GetStandardTrackSample(NamingConfig nameSpec);
        SampleResult GetMultiDiscTrackSample(NamingConfig nameSpec);
        string GetSeriesFolderSample(NamingConfig nameSpec);
    }

    public class FileNameSampleService : IFilenameSampleService
    {
        private readonly IBuildFileNames _buildFileNames;

        private static Series _standardSeries;
        private static Issue _standardBook;
        private static ComicFile _singleTrackFile;
        private static ComicFile _multiTrackFile;
        private static List<CustomFormat> _customFormats;

        public FileNameSampleService(IBuildFileNames buildFileNames)
        {
            _buildFileNames = buildFileNames;

            _standardSeries = new Series
            {
                Metadata = new SeriesMetadata
                {
                    Name = "The Series Name",
                    Disambiguation = "US Series"
                }
            };

            var series = new SeriesGroup
            {
                Title = "SeriesGroup Title"
            };

            var seriesLink = new SeriesGroupLink
            {
                Position = "1",
                SeriesGroup = series
            };

            _standardBook = new Issue
            {
                Title = "The Issue Title",
                IssueNumber = 42f,
                ReleaseDate = System.DateTime.Today,
                Series = _standardSeries,
                SeriesMetadata = _standardSeries.Metadata.Value,
                SeriesLinks = new List<SeriesGroupLink> { seriesLink }
            };

            _customFormats = new List<CustomFormat>
            {
                new CustomFormat
                {
                    Name = "Surround Sound",
                    IncludeCustomFormatWhenRenaming = true
                },
                new CustomFormat
                {
                    Name = "x264",
                    IncludeCustomFormatWhenRenaming = true
                }
            };

            var mediaInfo = new MediaInfoModel()
            {
                AudioFormat = "Flac Audio",
                AudioChannels = 2,
                AudioBitrate = 875,
                AudioBits = 24,
                AudioSampleRate = 44100
            };

            _singleTrackFile = new ComicFile
            {
                Quality = new QualityModel(Quality.CBZ, new Revision(2)),
                Path = "/comics/The.Series.Name.042.CBZ",
                SceneName = "The.Series.Name.042",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Issue = _standardBook,
                Part = 1,
                PartCount = 1
            };

            _multiTrackFile = new ComicFile
            {
                Quality = new QualityModel(Quality.CBZ, new Revision(2)),
                Path = "/comics/The.Series.Name.042.CBZ",
                SceneName = "The.Series.Name.042",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Issue = _standardBook,
                Part = 1,
                PartCount = 2
            };
        }

        public SampleResult GetStandardTrackSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildTrackSample(_standardSeries, _standardBook, _singleTrackFile, nameSpec),
                Series = _standardSeries,
                Issue = _standardBook,
                ComicFile = _singleTrackFile
            };

            return result;
        }

        public SampleResult GetMultiDiscTrackSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildTrackSample(_standardSeries, _standardBook, _multiTrackFile, nameSpec),
                Series = _standardSeries,
                Issue = _standardBook,
                ComicFile = _singleTrackFile
            };

            return result;
        }

        public string GetSeriesFolderSample(NamingConfig nameSpec)
        {
            return _buildFileNames.GetSeriesFolder(_standardSeries, nameSpec);
        }

        private string BuildTrackSample(Series author, Issue issue, ComicFile comicFile, NamingConfig nameSpec)
        {
            try
            {
                return _buildFileNames.BuildBookFileName(author, issue, comicFile, nameSpec, _customFormats);
            }
            catch (NamingFormatException)
            {
                return string.Empty;
            }
        }
    }
}
