using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.ComicInfo
{
    [TestFixture]
    public class ComicInfoReaderServiceFixture : CoreTest<ComicInfoReaderService>
    {
        private string GivenZipWithComicInfo(string extension)
        {
            var path = GetTempFilePath() + extension;

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("ComicInfo.xml");

                using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                {
                    writer.Write("<ComicInfo><Series>Nightwing</Series><Number>1</Number></ComicInfo>");
                }
            }

            return path;
        }

        [Test]
        public void should_read_metadata_from_a_matching_archive()
        {
            var file = new ComicFile
            {
                Path = GivenZipWithComicInfo(".cbz"),
                ComicFormat = ComicFormat.CBZ
            };

            var results = Subject.ReadMetadata(file);

            results.Should().NotBeEmpty();
            results.First().Fields["Series"].Should().Be("Nightwing");
        }

        [Test]
        public void should_read_metadata_from_a_zip_archive_named_cbr()
        {
            // The extension lies; the signature does not
            var file = new ComicFile
            {
                Path = GivenZipWithComicInfo(".cbr"),
                ComicFormat = ComicFormat.CBR
            };

            var results = Subject.ReadMetadata(file);

            results.Should().NotBeEmpty();
            results.First().Fields["Series"].Should().Be("Nightwing");
        }

        [Test]
        public void should_report_a_corrupt_archive()
        {
            var path = GetTempFilePath() + ".cbz";
            File.WriteAllBytes(path, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01, 0x02, 0x03 });

            var file = new ComicFile { Path = path, ComicFormat = ComicFormat.CBZ };

            Subject.ReadMetadata(file).Should().BeEmpty();

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_return_empty_for_a_missing_file()
        {
            var file = new ComicFile
            {
                Path = GetTempFilePath() + ".cbz",
                ComicFormat = ComicFormat.CBZ
            };

            Subject.ReadMetadata(file).Should().BeEmpty();
        }
    }
}
