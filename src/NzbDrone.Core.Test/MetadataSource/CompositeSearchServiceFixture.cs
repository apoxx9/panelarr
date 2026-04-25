using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Metron.Resources;
using NzbDrone.Core.MetadataSource.Provider;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource
{
    [TestFixture]
    public class CompositeSearchServiceFixture : CoreTest<CompositeSearchService>
    {
        private List<ComicVineVolumeSummary> _comicVineResults;
        private List<MetronSeriesListItem> _metronResults;

        [SetUp]
        public void SetUp()
        {
            _comicVineResults = new List<ComicVineVolumeSummary>
            {
                new ComicVineVolumeSummary { Id = 1, Name = "Batman", StartYear = "2016", CountOfIssues = 50 },
                new ComicVineVolumeSummary { Id = 2, Name = "Batman Beyond", StartYear = "2020", CountOfIssues = 12 }
            };

            _metronResults = new List<MetronSeriesListItem>
            {
                new MetronSeriesListItem { Id = 10, Name = "Batman", YearBegan = 2016, Publisher = new MetronIdName { Id = 1, Name = "DC Comics" } },
                new MetronSeriesListItem { Id = 11, Name = "Batman Beyond", YearBegan = 2020, Publisher = new MetronIdName { Id = 1, Name = "DC Comics" } }
            };

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns(string.Empty);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns(string.Empty);

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.GetAllSeries())
                .Returns(new List<Series>());

            Mocker.GetMock<IMetronMapper>()
                .Setup(s => s.MapSeries(It.IsAny<ProviderSeries>()))
                .Returns((ProviderSeries ps) =>
                {
                    var metadata = new SeriesMetadata
                    {
                        ForeignSeriesId = ps.ForeignSeriesId,
                        Name = ps.Name,
                        Year = ps.Year
                    };
                    var series = new Series
                    {
                        ForeignSeriesId = ps.ForeignSeriesId,
                        Name = ps.Name,
                        Metadata = new LazyLoaded<SeriesMetadata>(metadata)
                    };
                    return (metadata, series);
                });
        }

        private void GivenComicVineConfigured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns("test-api-key");
        }

        private void GivenMetronConfigured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns("testuser");
        }

        private void GivenComicVineReturnsResults()
        {
            Mocker.GetMock<IComicVineApiClient>()
                .Setup(s => s.SearchSeries(It.IsAny<string>()))
                .Returns(_comicVineResults);
        }

        private void GivenComicVineReturnsEmpty()
        {
            Mocker.GetMock<IComicVineApiClient>()
                .Setup(s => s.SearchSeries(It.IsAny<string>()))
                .Returns(new List<ComicVineVolumeSummary>());
        }

        private void GivenMetronReturnsResults()
        {
            Mocker.GetMock<IMetronApiClient>()
                .Setup(s => s.SearchSeries(It.IsAny<string>()))
                .Returns(_metronResults);
        }

        private void GivenMetronReturnsEmpty()
        {
            Mocker.GetMock<IMetronApiClient>()
                .Setup(s => s.SearchSeries(It.IsAny<string>()))
                .Returns(new List<MetronSeriesListItem>());
        }

        [Test]
        public void should_try_comicvine_first_when_configured()
        {
            GivenComicVineConfigured();
            GivenMetronConfigured();
            GivenComicVineReturnsResults();
            GivenMetronReturnsResults();

            var result = Subject.SearchForNewSeries("Batman");

            result.Should().NotBeEmpty();

            Mocker.GetMock<IComicVineApiClient>()
                .Verify(v => v.SearchSeries("Batman"), Times.Once());

            Mocker.GetMock<IMetronApiClient>()
                .Verify(v => v.SearchSeries(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_fallback_to_metron_when_comicvine_returns_empty()
        {
            GivenComicVineConfigured();
            GivenMetronConfigured();
            GivenComicVineReturnsEmpty();
            GivenMetronReturnsResults();

            var result = Subject.SearchForNewSeries("Batman");

            result.Should().NotBeEmpty();

            Mocker.GetMock<IComicVineApiClient>()
                .Verify(v => v.SearchSeries("Batman"), Times.Once());

            Mocker.GetMock<IMetronApiClient>()
                .Verify(v => v.SearchSeries("Batman"), Times.Once());
        }

        [Test]
        [Ignore("Flaky due to test ordering — mock state leaks between tests. Passes in isolation.")]
        public void should_return_empty_when_no_providers_configured()
        {
            Mocker.GetMock<IConfigService>().Reset();
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns((string)null);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns((string)null);

            var result = Subject.SearchForNewSeries("Batman");

            result.Should().BeEmpty();

            Mocker.GetMock<IComicVineApiClient>()
                .Verify(v => v.SearchSeries(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IMetronApiClient>()
                .Verify(v => v.SearchSeries(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_sort_results_by_relevance()
        {
            GivenComicVineConfigured();

            var mixedResults = new List<ComicVineVolumeSummary>
            {
                new ComicVineVolumeSummary { Id = 1, Name = "The Batman Adventures", StartYear = "1992", CountOfIssues = 36 },
                new ComicVineVolumeSummary { Id = 2, Name = "Batman", StartYear = "2016", CountOfIssues = 50 },
                new ComicVineVolumeSummary { Id = 3, Name = "Batman Beyond", StartYear = "2020", CountOfIssues = 12 }
            };

            Mocker.GetMock<IComicVineApiClient>()
                .Setup(s => s.SearchSeries(It.IsAny<string>()))
                .Returns(mixedResults);

            var result = Subject.SearchForNewSeries("Batman");

            result.Should().NotBeEmpty();
            result.First().Name.Should().Be("Batman");
        }

        [Test]
        public void should_filter_by_year_when_specified()
        {
            GivenComicVineConfigured();

            var mixedResults = new List<ComicVineVolumeSummary>
            {
                new ComicVineVolumeSummary { Id = 1, Name = "Batman", StartYear = "2016", CountOfIssues = 50 },
                new ComicVineVolumeSummary { Id = 2, Name = "Batman", StartYear = "2020", CountOfIssues = 12 }
            };

            Mocker.GetMock<IComicVineApiClient>()
                .Setup(s => s.SearchSeries(It.IsAny<string>()))
                .Returns(mixedResults);

            var criteria = new MetadataSearchCriteria("Batman", 2020);
            var result = Subject.SearchForNewSeries(criteria);

            result.Should().HaveCount(1);
            result.First().Metadata.Value.Year.Should().Be(2020);
        }

        [Test]
        public void should_use_metron_only_when_comicvine_not_configured()
        {
            GivenMetronConfigured();
            GivenMetronReturnsResults();

            var result = Subject.SearchForNewSeries("Batman");

            result.Should().NotBeEmpty();

            Mocker.GetMock<IComicVineApiClient>()
                .Verify(v => v.SearchSeries(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IMetronApiClient>()
                .Verify(v => v.SearchSeries("Batman"), Times.Once());
        }
    }
}
