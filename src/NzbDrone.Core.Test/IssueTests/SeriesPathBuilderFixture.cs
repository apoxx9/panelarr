using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class SeriesPathBuilderFixture : CoreTest<SeriesPathBuilder>
    {
        private const string RootFolder = @"/comics";
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series
            {
                RootFolderPath = RootFolder,
                Metadata = new SeriesMetadata { Name = "Saga" }
            };

            GivenSeriesFolder(Path.Combine("Boom! Studios", "Saga (2012)"));
            GivenExistingFolders();
        }

        private void GivenSeriesFolder(string relativeFolder)
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetSeriesFolder(_series, null))
                  .Returns(relativeFolder);
        }

        private void GivenExistingFolders(params string[] folderNames)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(RootFolder))
                  .Returns(true);

            var fullPaths = new string[folderNames.Length];
            for (var i = 0; i < folderNames.Length; i++)
            {
                fullPaths[i] = Path.Combine(RootFolder, folderNames[i]);
            }

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetDirectories(RootFolder))
                  .Returns(fullPaths);
        }

        [Test]
        public void should_adopt_existing_publisher_folder_differing_in_punctuation()
        {
            GivenExistingFolders("Boom Studios", "DC Comics");

            Subject.BuildPath(_series, false)
                   .Should().Be(Path.Combine(RootFolder, "Boom Studios", "Saga (2012)"));
        }

        [Test]
        public void should_adopt_existing_publisher_folder_differing_in_case()
        {
            GivenExistingFolders("boom! studios");

            Subject.BuildPath(_series, false)
                   .Should().Be(Path.Combine(RootFolder, "boom! studios", "Saga (2012)"));
        }

        [Test]
        public void should_prefer_exact_publisher_folder_match_over_variants()
        {
            GivenExistingFolders("Boom Studios", "Boom! Studios");

            Subject.BuildPath(_series, false)
                   .Should().Be(Path.Combine(RootFolder, "Boom! Studios", "Saga (2012)"));
        }

        [Test]
        public void should_keep_built_publisher_folder_when_no_match_exists()
        {
            GivenExistingFolders("DC Comics", "Image Comics");

            Subject.BuildPath(_series, false)
                   .Should().Be(Path.Combine(RootFolder, "Boom! Studios", "Saga (2012)"));
        }

        [Test]
        public void should_keep_built_path_when_root_folder_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(RootFolder))
                  .Returns(false);

            Subject.BuildPath(_series, false)
                   .Should().Be(Path.Combine(RootFolder, "Boom! Studios", "Saga (2012)"));
        }

        [Test]
        public void should_not_rewrite_series_folder_component()
        {
            GivenSeriesFolder("Saga (2012)");
            GivenExistingFolders("saga (2012)");

            Subject.BuildPath(_series, false)
                   .Should().Be(Path.Combine(RootFolder, "Saga (2012)"));
        }
    }
}
