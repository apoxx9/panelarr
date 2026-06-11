using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.IssueInfo;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Provider;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.IssueInfoTests
{
    [TestFixture]
    public class IssueInfoProxySearchFixture : CoreTest<IssueInfoProxy>
    {
        private ProviderSeries _providerSeries;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IMetronMapper>(Mocker.Resolve<MetronMapper>());

            _providerSeries = new ProviderSeries
            {
                ForeignSeriesId = "cv:46568",
                Name = "Saga",
                Year = 2012,
                Issues = new List<ProviderIssue>
                {
                    new ProviderIssue { ForeignIssueId = "cv:1", IssueNumber = "1", Title = "Chapter One" },
                    new ProviderIssue { ForeignIssueId = "cv:3", IssueNumber = "3", Title = "Chapter Three" }
                }
            };

            Mocker.GetMock<IMetadataProvider>()
                  .Setup(s => s.GetSeriesInfo("cv:46568"))
                  .Returns(_providerSeries);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetAllSeries())
                  .Returns(new List<Series>());
        }

        private void GivenSearchResults(params ProviderSeries[] results)
        {
            Mocker.GetMock<IMetadataProvider>()
                  .Setup(s => s.SearchSeries(It.IsAny<string>()))
                  .Returns(results.ToList());
        }

        [Test]
        public void search_should_return_issues_of_best_series_match()
        {
            GivenSearchResults(_providerSeries);

            var issues = Subject.SearchForNewIssue("Chapter Three", "Saga");

            issues.Should().HaveCount(2);
            issues.First().Title.Should().Be("Chapter Three");
        }

        [Test]
        public void search_should_return_empty_for_blank_query()
        {
            Subject.SearchForNewIssue(null, null).Should().BeEmpty();
        }

        [Test]
        public void search_should_return_empty_when_no_series_matches()
        {
            GivenSearchResults();

            Subject.SearchForNewIssue("Chapter Three", "Saga").Should().BeEmpty();
        }

        [Test]
        public void refresh_should_propagate_provider_publisher_rename()
        {
            _providerSeries.ForeignPublisherId = "cv:6";
            _providerSeries.PublisherName = "Image";

            Mocker.GetMock<IPublisherService>()
                  .Setup(s => s.FindByForeignId("cv:6"))
                  .Returns(new Publisher { Id = 1, ForeignPublisherId = "cv:6", Name = "Unknown", CleanName = "unknown" });

            Subject.GetSeriesInfo("cv:46568", useCache: false);

            Mocker.GetMock<IPublisherService>()
                  .Verify(s => s.UpdatePublisher(It.Is<Publisher>(p => p.Name == "Image" && p.CleanName == "image")), Times.Once());
        }

        [Test]
        public void refresh_should_not_touch_publisher_when_name_matches()
        {
            _providerSeries.ForeignPublisherId = "cv:6";
            _providerSeries.PublisherName = "Image";

            Mocker.GetMock<IPublisherService>()
                  .Setup(s => s.FindByForeignId("cv:6"))
                  .Returns(new Publisher { Id = 1, ForeignPublisherId = "cv:6", Name = "Image", CleanName = "image" });

            Subject.GetSeriesInfo("cv:46568", useCache: false);

            Mocker.GetMock<IPublisherService>()
                  .Verify(s => s.UpdatePublisher(It.IsAny<Publisher>()), Times.Never());
        }
    }
}
