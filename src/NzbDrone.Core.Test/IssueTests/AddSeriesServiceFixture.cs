using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class AddSeriesServiceFixture : CoreTest<AddSeriesService>
    {
        private string _sharedFolder;

        [SetUp]
        public void Setup()
        {
            _sharedFolder = @"C:\comics\DC Comics\Batman (2016)".AsOsAgnostic();

            Mocker.GetMock<IProvideSeriesInfo>()
                  .Setup(s => s.GetSeriesInfo(It.IsAny<string>(), It.IsAny<bool>()))
                  .Returns(() => new Series
                  {
                      Metadata = new SeriesMetadata
                      {
                          ForeignSeriesId = "cv:96128",
                          Name = "Batman Annual"
                      }
                  });

            Mocker.GetMock<IAddSeriesValidator>()
                  .Setup(s => s.Validate(It.IsAny<Series>()))
                  .Returns(new FluentValidation.Results.ValidationResult());

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()))
                  .Returns<Series, bool>((s, r) => s);
        }

        private Series GivenNewSeries(string path)
        {
            return new Series
            {
                Path = path,
                Metadata = new SeriesMetadata { ForeignSeriesId = "cv:96128" }
            };
        }

        [Test]
        public void explicit_path_to_existing_shared_folder_should_be_kept()
        {
            // Mylar-style layout: the annual's files live in the parent
            // series' folder — the caller's path is the truth
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.SeriesPathExists(_sharedFolder))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_sharedFolder))
                  .Returns(true);

            var added = Subject.AddSeries(GivenNewSeries(_sharedFolder), doRefresh: false);

            added.Path.Should().Be(_sharedFolder);
        }

        [Test]
        public void explicit_path_to_missing_folder_should_still_be_disambiguated_on_collision()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.SeriesPathExists(_sharedFolder))
                  .Returns(true);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.SeriesPathExists(_sharedFolder + " (1)"))
                  .Returns(false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_sharedFolder))
                  .Returns(false);

            var added = Subject.AddSeries(GivenNewSeries(_sharedFolder), doRefresh: false);

            added.Path.Should().NotBe(_sharedFolder);
        }

        [Test]
        public void explicit_path_without_collision_should_be_kept()
        {
            var path = @"C:\comics\DC Comics\Batman Annual (2016)".AsOsAgnostic();

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.SeriesPathExists(path))
                  .Returns(false);

            var added = Subject.AddSeries(GivenNewSeries(path), doRefresh: false);

            added.Path.Should().Be(path);
        }
    }
}
