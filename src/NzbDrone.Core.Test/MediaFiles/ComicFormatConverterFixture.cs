using System.IO;
using System.IO.Compression;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ComicFormatConverterFixture : CoreTest<ComicFormatConverter>
    {
        private string _dir;

        [SetUp]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "panelarr-convert-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private string GivenZipFile(string name, int pages = 3)
        {
            var path = Path.Combine(_dir, name);

            using (var stream = new FileStream(path, FileMode.Create))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                for (var i = 1; i <= pages; i++)
                {
                    var entry = zip.CreateEntry($"page{i:000}.jpg");
                    using var es = entry.Open();
                    es.WriteByte(0xFF);
                }
            }

            return path;
        }

        [Test]
        public void zip_content_with_cbr_extension_should_be_renamed()
        {
            // The Suske en Wiske case: real zip wearing .cbr, unreadable by
            // every RAR-based code path
            var path = GivenZipFile("Suske en Wiske #032a (1958).cbr");

            var result = Subject.ConvertToRealCbz(path);

            result.Error.Should().BeNull();
            result.Changed.Should().BeTrue();
            result.FinalPath.Should().EndWith(".cbz");
            File.Exists(result.FinalPath).Should().BeTrue();
            File.Exists(path).Should().BeFalse();

            using var verify = ZipFile.OpenRead(result.FinalPath);
            verify.Entries.Count.Should().Be(3);
        }

        [Test]
        public void real_cbz_should_be_left_alone()
        {
            var path = GivenZipFile("Iron Man 001.cbz");

            var result = Subject.ConvertToRealCbz(path);

            result.Error.Should().BeNull();
            result.Changed.Should().BeFalse();
            result.FinalPath.Should().Be(path);
        }

        [Test]
        public void rename_should_refuse_when_target_exists()
        {
            var path = GivenZipFile("Iron Man 001.cbr");
            GivenZipFile("Iron Man 001.cbz");

            var result = Subject.ConvertToRealCbz(path);

            result.Error.Should().NotBeNull();
            result.Changed.Should().BeFalse();
            File.Exists(path).Should().BeTrue();
        }

        [Test]
        public void unknown_format_should_error_without_touching_file()
        {
            var path = Path.Combine(_dir, "garbage.cbr");
            File.WriteAllText(path, "this is not an archive");

            var result = Subject.ConvertToRealCbz(path);

            result.Error.Should().NotBeNull();
            result.Changed.Should().BeFalse();
            File.Exists(path).Should().BeTrue();
        }

        [Test]
        public void missing_file_should_error()
        {
            var result = Subject.ConvertToRealCbz(Path.Combine(_dir, "nope.cbr"));

            result.Error.Should().NotBeNull();
        }
    }
}
