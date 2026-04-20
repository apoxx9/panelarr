using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.IssueInfo;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.Goodreads
{
    [TestFixture]
    [Explicit("Integration test requiring network access to metadata server")]
    public class IssueInfoProxyFixture : CoreTest<IssueInfoProxy>
    {
        private MetadataProfile _metadataProfile;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            _metadataProfile = new MetadataProfile();

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.Get(It.IsAny<int>()))
                .Returns(_metadataProfile);

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.Exists(It.IsAny<int>()))
                .Returns(true);
        }

        [TestCase("1654", "Terry Pratchett")]
        [TestCase("575", "Robert Harris")]
        public void should_be_able_to_get_author_detail(string mbId, string name)
        {
            var details = Subject.GetSeriesInfo(mbId);

            ValidateSeries(details);

            details.Name.Should().Be(name);
        }

        [TestCase("1128601", "Guards! Guards!")]
        [TestCase("3293141", "Ἰλιάς")]
        public void should_be_able_to_get_book_detail(string mbId, string name)
        {
            var details = Subject.GetIssueInfo(mbId);

            ValidateIssues(new List<Issue> { details.Item2 });

            details.Item2.Title.Should().Be(name);
        }

        [TestCase("14190696", "The Issue of Dust", "1")]
        [TestCase("48427681", "October Daye Chronological Order", "7.1")]
        public void should_parse_series_from_title(string id, string series, string position)
        {
            var result = Subject.GetIssueInfo(id);

            var link = result.Item2.SeriesLinks.Value.OrderBy(x => x.SeriesPosition).First();
            link.SeriesGroup.Value.Title.Should().Be(series);
            link.Position.Should().Be(position);
        }

        [Test]
        public void getting_details_of_invalid_author()
        {
            Assert.Throws<SeriesNotFoundException>(() => Subject.GetSeriesInfo("1"));
        }

        [Test]
        public void getting_details_of_invalid_book()
        {
            Assert.Throws<IssueNotFoundException>(() => Subject.GetIssueInfo("1"));
        }

        private void ValidateSeries(Series series)
        {
            series.Should().NotBeNull();
            series.Name.Should().NotBeNullOrWhiteSpace();
            series.CleanName.Should().Be(Parser.Parser.CleanSeriesName(series.Name));
            series.Metadata.Value.TitleSlug.Should().NotBeNullOrWhiteSpace();
            series.Metadata.Value.Overview.Should().NotBeNullOrWhiteSpace();
            series.Metadata.Value.Images.Should().NotBeEmpty();
            series.ForeignSeriesId.Should().NotBeNullOrWhiteSpace();
        }

        private void ValidateIssues(List<Issue> issues, bool idOnly = false)
        {
            issues.Should().NotBeEmpty();

            foreach (var issue in issues)
            {
                issue.ForeignIssueId.Should().NotBeNullOrWhiteSpace();
                if (!idOnly)
                {
                    ValidateIssue(issue);
                }
            }

            //if atleast one issue has title it means parse it working.
            if (!idOnly)
            {
                issues.Should().Contain(c => !string.IsNullOrWhiteSpace(c.Title));
            }
        }

        private void ValidateIssue(Issue issue)
        {
            issue.Should().NotBeNull();

            issue.Title.Should().NotBeNullOrWhiteSpace();

            issue.Should().NotBeNull();

            if (issue.ReleaseDate.HasValue)
            {
                issue.ReleaseDate.Value.Kind.Should().Be(DateTimeKind.Utc);
            }
        }
    }
}
