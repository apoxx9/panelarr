using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ArchiveInspectorFixture : CoreTest<ArchiveInspector>
    {
        private string GivenZipArchive(string extension, string comicInfoXml = null)
        {
            var path = GetTempFilePath() + extension;

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var page = archive.CreateEntry("001.jpg");

                using (var stream = page.Open())
                {
                    stream.Write(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }, 0, 4);
                }

                if (comicInfoXml != null)
                {
                    var info = archive.CreateEntry("ComicInfo.xml");

                    using (var writer = new StreamWriter(info.Open(), Encoding.UTF8))
                    {
                        writer.Write(comicInfoXml);
                    }
                }
            }

            return path;
        }

        [Test]
        public void should_inspect_a_zip_archive_named_cbr()
        {
            // Two homelab files sat on "Rar signature not found" for months -
            // they are ZIPs wearing a .cbr
            var path = GivenZipArchive(".cbr");

            var result = Subject.Inspect(path, ComicFormat.CBR);

            result.Error.Should().BeNull();
            result.ImageCount.Should().Be(1);
            result.PageCount.Should().Be(1);
        }

        [Test]
        public void should_capture_comicinfo_from_a_misnamed_archive()
        {
            var path = GivenZipArchive(".cbr", "<ComicInfo><Title>Recovered</Title></ComicInfo>");

            var result = Subject.Inspect(path, ComicFormat.CBR);

            result.Error.Should().BeNull();
            result.ComicInfoXml.Should().Contain("Recovered");
        }

        [Test]
        public void should_inspect_a_zip_archive_named_cbz_without_a_signature_read()
        {
            var path = GivenZipArchive(".cbz");

            var result = Subject.Inspect(path, ComicFormat.CBZ);

            result.Error.Should().BeNull();
            result.ImageCount.Should().Be(1);
        }

        [Test]
        public void should_still_report_a_corrupt_archive()
        {
            // No recognisable signature means no second guess - a genuinely
            // broken file has to stay loud
            var path = GetTempFilePath() + ".cbz";
            File.WriteAllBytes(path, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01, 0x02, 0x03 });

            var result = Subject.Inspect(path, ComicFormat.CBZ);

            result.Error.Should().NotBeNullOrWhiteSpace();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_report_a_missing_file()
        {
            var result = Subject.Inspect(GetTempFilePath() + ".cbz", ComicFormat.CBZ);

            result.Error.Should().NotBeNullOrWhiteSpace();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_second_guess_epub()
        {
            // EPUB is a ZIP container, so a signature read would redirect it
            // away from its own handling - it never fails, so it never sniffs
            var path = GivenZipArchive(".epub");

            var result = Subject.Inspect(path, ComicFormat.EPUB);

            result.Error.Should().BeNull();
            result.ImageCount.Should().Be(0);
            result.ImageQualityScore.Should().Be(0.5f);
        }
    }
}
