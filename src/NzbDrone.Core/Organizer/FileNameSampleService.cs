using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IFilenameSampleService
    {
        SampleResult GetStandardIssueSample(NamingConfig nameSpec);
        SampleResult GetMultiDiscIssueSample(NamingConfig nameSpec);
        string GetSeriesFolderSample(NamingConfig nameSpec);
    }

    public class FileNameSampleService : IFilenameSampleService
    {
        private readonly IBuildFileNames _buildFileNames;

        private static Series _standardSeries;
        private static Issue _standardIssue;
        private static ComicFile _singleComicFile;
        private static ComicFile _multiComicFile;
        private static List<CustomFormat> _customFormats;

        public FileNameSampleService(IBuildFileNames buildFileNames)
        {
            _buildFileNames = buildFileNames;

            _standardSeries = new Series
            {
                Metadata = new SeriesMetadata
                {
                    Name = "The Series Name",
                    Disambiguation = "US Series",
                    Year = 2024
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

            _standardIssue = new Issue
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

            _singleComicFile = new ComicFile
            {
                Quality = new QualityModel(Quality.CBZ, new Revision(2)),
                Path = "/comics/The.Series.Name.042.CBZ",
                SceneName = "The.Series.Name.042",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Issue = _standardIssue,
                Part = 1,
                PartCount = 1
            };

            _multiComicFile = new ComicFile
            {
                Quality = new QualityModel(Quality.CBZ, new Revision(2)),
                Path = "/comics/The.Series.Name.042.CBZ",
                SceneName = "The.Series.Name.042",
                ReleaseGroup = "RlsGrp",
                MediaInfo = mediaInfo,
                Issue = _standardIssue,
                Part = 1,
                PartCount = 2
            };
        }

        public SampleResult GetStandardIssueSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildIssueSample(_standardSeries, _standardIssue, _singleComicFile, nameSpec),
                Series = _standardSeries,
                Issue = _standardIssue,
                ComicFile = _singleComicFile
            };

            return result;
        }

        public SampleResult GetMultiDiscIssueSample(NamingConfig nameSpec)
        {
            var result = new SampleResult
            {
                FileName = BuildIssueSample(_standardSeries, _standardIssue, _multiComicFile, nameSpec),
                Series = _standardSeries,
                Issue = _standardIssue,
                ComicFile = _singleComicFile
            };

            return result;
        }

        public string GetSeriesFolderSample(NamingConfig nameSpec)
        {
            return _buildFileNames.GetSeriesFolder(_standardSeries, nameSpec);
        }

        private string BuildIssueSample(Series series, Issue issue, ComicFile comicFile, NamingConfig nameSpec)
        {
            try
            {
                return _buildFileNames.BuildComicFileName(series, issue, comicFile, nameSpec, _customFormats);
            }
            catch (NamingFormatException)
            {
                return string.Empty;
            }
        }
    }
}
