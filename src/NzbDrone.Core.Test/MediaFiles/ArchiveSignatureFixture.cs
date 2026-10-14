using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ArchiveSignatureFixture : CoreTest
    {
        [Test]
        public void should_identify_a_zip_header()
        {
            ArchiveSignature.Identify(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0 }, 8)
                            .Should().Be(ComicFormat.CBZ);
        }

        [Test]
        public void should_identify_an_empty_zip_header()
        {
            ArchiveSignature.Identify(new byte[] { 0x50, 0x4B, 0x05, 0x06, 0, 0, 0, 0 }, 8)
                            .Should().Be(ComicFormat.CBZ);
        }

        [Test]
        public void should_identify_a_rar_header()
        {
            // "Rar!" - shared by RAR4 and RAR5
            ArchiveSignature.Identify(Encoding.ASCII.GetBytes("Rar!\x1a\x07\x00\x00"), 8)
                            .Should().Be(ComicFormat.CBR);
        }

        [Test]
        public void should_identify_a_seven_zip_header()
        {
            ArchiveSignature.Identify(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C, 0, 0 }, 8)
                            .Should().Be(ComicFormat.CB7);
        }

        [Test]
        public void should_identify_a_pdf_header()
        {
            ArchiveSignature.Identify(Encoding.ASCII.GetBytes("%PDF-1.7"), 8)
                            .Should().Be(ComicFormat.PDF);
        }

        [Test]
        public void should_return_null_for_an_unrecognised_header()
        {
            ArchiveSignature.Identify(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0, 0, 0, 0 }, 8)
                            .Should().BeNull();
        }

        [Test]
        public void should_return_null_when_the_header_is_too_short_to_judge()
        {
            // A truncated file must stay a failure rather than be guessed at
            ArchiveSignature.Identify(new byte[] { 0x50, 0x4B, 0, 0, 0, 0, 0, 0 }, 2)
                            .Should().BeNull();
        }

        [Test]
        public void should_return_null_for_a_null_header()
        {
            ArchiveSignature.Identify(null, 8).Should().BeNull();
        }

        [Test]
        public void should_identify_a_zip_file_whose_extension_says_rar()
        {
            var path = GetTempFilePath() + ".cbr";

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                archive.CreateEntry("001.jpg");
            }

            ArchiveSignature.Identify(path).Should().Be(ComicFormat.CBZ);
        }

        [Test]
        public void should_return_null_for_a_missing_file()
        {
            ArchiveSignature.Identify(GetTempFilePath() + ".cbz").Should().BeNull();
        }

        [Test]
        public void should_return_null_for_an_empty_file()
        {
            var path = GetTempFilePath() + ".cbz";
            File.WriteAllBytes(path, new byte[0]);

            ArchiveSignature.Identify(path).Should().BeNull();
        }
    }
}
