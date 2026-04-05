using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentValidation.Results;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.IssueInfo;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class IdentificationServiceFixture : DbTest
    {
        private SeriesService _seriesService;
        private AddSeriesService _addSeriesService;
        private RefreshSeriesService _refreshSeriesService;

        private IdentificationService _Subject;

        [SetUp]
        public void SetUp()
        {
            UseRealHttp();

            // Resolve all the parts we need
            Mocker.SetConstant<ISeriesRepository>(Mocker.Resolve<SeriesRepository>());
            Mocker.SetConstant<ISeriesMetadataRepository>(Mocker.Resolve<SeriesMetadataRepository>());
            Mocker.SetConstant<IIssueRepository>(Mocker.Resolve<IssueRepository>());
            Mocker.SetConstant<IImportListExclusionRepository>(Mocker.Resolve<ImportListExclusionRepository>());
            Mocker.SetConstant<IMediaFileRepository>(Mocker.Resolve<MediaFileRepository>());

            Mocker.GetMock<IMetadataProfileService>().Setup(x => x.Exists(It.IsAny<int>())).Returns(true);

            _seriesService = Mocker.Resolve<SeriesService>();
            Mocker.SetConstant<ISeriesService>(_seriesService);
            Mocker.SetConstant<ISeriesMetadataService>(Mocker.Resolve<SeriesMetadataService>());
            Mocker.SetConstant<IIssueService>(Mocker.Resolve<IssueService>());
            Mocker.SetConstant<IImportListExclusionService>(Mocker.Resolve<ImportListExclusionService>());
            Mocker.SetConstant<IMediaFileService>(Mocker.Resolve<MediaFileService>());

            Mocker.SetConstant<IConfigService>(Mocker.Resolve<IConfigService>());
            Mocker.SetConstant<IProvideSeriesInfo>(Mocker.Resolve<IssueInfoProxy>());
            Mocker.SetConstant<IProvideIssueInfo>(Mocker.Resolve<IssueInfoProxy>());

            _addSeriesService = Mocker.Resolve<AddSeriesService>();

            Mocker.SetConstant<IRefreshIssueService>(Mocker.Resolve<RefreshIssueService>());
            _refreshSeriesService = Mocker.Resolve<RefreshSeriesService>();

            Mocker.GetMock<IAddSeriesValidator>().Setup(x => x.Validate(It.IsAny<Series>())).Returns(new ValidationResult());

            Mocker.SetConstant<ITrackGroupingService>(Mocker.Resolve<TrackGroupingService>());
            Mocker.SetConstant<ICandidateService>(Mocker.Resolve<CandidateService>());

            // set up the augmenters
            var aggregators = new List<IAggregate<LocalEdition>>
            {
                Mocker.Resolve<AggregateFilenameInfo>()
            };
            Mocker.SetConstant<IEnumerable<IAggregate<LocalEdition>>>(aggregators);
            Mocker.SetConstant<IAugmentingService>(Mocker.Resolve<AugmentingService>());

            _Subject = Mocker.Resolve<IdentificationService>();
        }

        private void GivenMetadataProfile(MetadataProfile profile)
        {
            Mocker.GetMock<IMetadataProfileService>().Setup(x => x.Get(profile.Id)).Returns(profile);
        }

        private List<Series> GivenSeries(List<SeriesTestCase> allSeries)
        {
            var outp = new List<Series>();
            for (var i = 0; i < allSeries.Count; i++)
            {
                var meta = allSeries[i].MetadataProfile;
                meta.Id = i + 1;
                GivenMetadataProfile(meta);
                outp.Add(GivenSeries(allSeries[i].Series, meta.Id));
            }

            return outp;
        }

        private Series GivenSeries(string foreignSeriesId, int metadataProfileId)
        {
            var series = _addSeriesService.AddSeries(new Series
            {
                Metadata = new SeriesMetadata
                {
                    ForeignSeriesId = foreignSeriesId
                },
                Path = @"c:\test".AsOsAgnostic()
            });

            var command = new RefreshSeriesCommand
            {
                SeriesId = series.Id,
                Trigger = CommandTrigger.Unspecified
            };

            _refreshSeriesService.Execute(command);

            return _seriesService.FindById(foreignSeriesId);
        }

        public static class IdTestCaseFactory
        {
            // for some reason using Directory.GetFiles causes nUnit to error
            private static string[] files =
            {
                "FilesWithMBIds.json",
                "PreferMissingToBadMatch.json",
                "InconsistentTyposInIssue.json",
                "SucceedWhenManyIssuesHaveSameTitle.json",
                "PenalizeUnknownMedia.json",
                "CorruptFile.json",
                "FilesWithoutTags.json"
            };

            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var file in files)
                    {
                        yield return new TestCaseData(file).SetName($"should_match_tracks_{file.Replace(".json", "")}");
                    }
                }
            }
        }

        // these are slow to run so only do so manually
        [Explicit]
        [TestCaseSource(typeof(IdTestCaseFactory), "TestCases")]
        public void should_match_tracks(string file)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Identification", file);
            var testcase = JsonConvert.DeserializeObject<IdTestCase>(File.ReadAllText(path));

            var allSeries = GivenSeries(testcase.LibrarySeries);
            var specifiedSeries = allSeries.SingleOrDefault(x => x.Metadata.Value.ForeignSeriesId == testcase.Series);
            var idOverrides = new IdentificationOverrides { Series = specifiedSeries };

            var tracks = testcase.Tracks.Select(x => new LocalIssue
            {
                Path = x.Path.AsOsAgnostic(),
                FileTrackInfo = x.FileTrackInfo
            }).ToList();

            var config = new ImportDecisionMakerConfig
            {
                NewDownload = testcase.NewDownload,
                SingleRelease = testcase.SingleRelease,
                IncludeExisting = false
            };

            var result = _Subject.Identify(tracks, idOverrides, config);

            result.Should().HaveCount(testcase.ExpectedMusicBrainzReleaseIds.Count);
        }
    }
}
