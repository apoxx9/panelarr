using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    public class NewznabRequestGeneratorFixture : CoreTest<NewznabRequestGenerator>
    {
        private IssueSearchCriteria _singleIssueSearchCriteria;
        private NewznabCapabilities _capabilities;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new NewznabSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                Categories = new[] { 1, 2 },
                ApiKey = "abcd",
            };

            _singleIssueSearchCriteria = new IssueSearchCriteria
            {
                Series = new Issues.Series { Name = "East of West" },
                IssueTitle = "The Promise"
            };

            _capabilities = new NewznabCapabilities();

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Returns(_capabilities);
        }

        [Test]
        public void should_use_all_categories_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("&cat=1,2&");
        }

        [Test]
        [Ignore("Disabled since no usenet indexers seem to support it")]
        public void should_search_by_author_and_book_if_supported()
        {
            _capabilities.SupportedComicSearchParameters = new[] { "q", "series", "title" };

            var results = Subject.GetSearchRequests(_singleIssueSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("series=East%20of%20West");
            page.Url.Query.Should().Contain("title=The%20Promise");
        }

        [Test]
        [Ignore("TODO: add raw search support")]
        public void should_encode_raw_title()
        {
            _capabilities.SupportedComicSearchParameters = new[] { "q", "series", "title" };

            // _capabilities.IssueTextSearchEngine = "raw";
            _singleIssueSearchCriteria.IssueTitle = "Batman & Robin";

            var results = Subject.GetSearchRequests(_singleIssueSearchCriteria);
            results.Tiers.Should().Be(1);

            var pageTier = results.GetTier(0).First().First();

            pageTier.Url.Query.Should().Contain("q=Batman%20%26%20Robin");
            pageTier.Url.Query.Should().NotContain(" & ");
            pageTier.Url.Query.Should().Contain("%26");
        }

        [Test]
        public void should_search_by_series_name_alone_for_single_issue_series()
        {
            // A one-shot (Epic Collection, TPB, OGN) is a book: the CV issue
            // title is boilerplate ("Volume 1") and poisons AND-matching
            // trackers, so the query is the series name and nothing else.
            _singleIssueSearchCriteria.Series = new Issues.Series { Name = "Iron Fist Epic Collection: The Fury of Iron Fist" };
            _singleIssueSearchCriteria.IssueTitle = "Volume 1";
            _singleIssueSearchCriteria.IssueNumber = 1;
            _singleIssueSearchCriteria.SingleIssueSeries = true;

            var results = Subject.GetSearchRequests(_singleIssueSearchCriteria);
            var pages = new System.Collections.Generic.List<NzbDrone.Core.Indexers.IndexerRequest>();

            foreach (var tier in results.GetAllTiers())
            {
                pages.AddRange(tier);
            }

            pages.Should().NotBeEmpty();

            foreach (var page in pages)
            {
                page.Url.Query.Should().NotContain("Volume");
                page.Url.Query.Should().NotContain("q=+");
                page.Url.Query.Should().NotContain("+&");
            }

            pages.First().Url.Query.Should().Contain("q=Iron%20Fist%20Epic%20Collection%20The%20Fury%20of%20Iron%20Fist");
        }

        [Test]
        public void should_fall_back_to_issue_number_for_boilerplate_titles_on_multi_issue_series()
        {
            _singleIssueSearchCriteria.Series = new Issues.Series { Name = "Naruto" };
            _singleIssueSearchCriteria.IssueTitle = "Volume 12";
            _singleIssueSearchCriteria.IssueNumber = 12;
            _singleIssueSearchCriteria.SingleIssueSeries = false;

            _singleIssueSearchCriteria.IssueQuery.Should().Be("#12");
        }

        [Test]
        public void should_keep_real_issue_titles()
        {
            _singleIssueSearchCriteria.IssueTitle = "The Promise";
            _singleIssueSearchCriteria.SingleIssueSeries = false;

            _singleIssueSearchCriteria.IssueQuery.Should().Contain("Promise");
        }

        [Test]
        public void should_use_clean_title_and_encode()
        {
            _capabilities.SupportedComicSearchParameters = new[] { "q", "series", "title" };

            // _capabilities.IssueTextSearchEngine = "sphinx";
            _singleIssueSearchCriteria.IssueTitle = "Batman & Robin";

            var results = Subject.GetSearchRequests(_singleIssueSearchCriteria);
            results.Tiers.Should().Be(2);

            var pageTier = results.GetTier(0).First().First();

            pageTier.Url.Query.Should().Contain("q=Batman%20Robin");
            pageTier.Url.Query.Should().NotContain("and");
            pageTier.Url.Query.Should().NotContain(" & ");
            pageTier.Url.Query.Should().NotContain("%26");
        }
    }
}
