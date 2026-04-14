using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]
    [Ignore("Don't use issue folder in panelarr")]
    public class BuildFilePathFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [Test]
        public void should_clean_book_folder_when_it_contains_illegal_characters_in_book_or_series_title()
        {
            var filename = @"bookfile";
            var expectedPath = @"C:\Test\Fake- The Series\Fake- The Issue\bookfile.mobi";

            var fakeSeries = Builder<Series>.CreateNew()
                .With(s => s.Name = "Fake: The Series")
                .With(s => s.Path = @"C:\Test\Fake- The Series".AsOsAgnostic())
                .Build();

            var fakeIssue = Builder<Issue>.CreateNew()
                .With(s => s.Title = "Fake: Issue")
                .Build();

            Subject.BuildComicFilePath(fakeSeries, fakeIssue, filename, ".mobi").Should().Be(expectedPath.AsOsAgnostic());
        }
    }
}
