using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedMetadataFilesFixture : DbTest<CleanupOrphanedMetadataFiles, MetadataFile>
    {
        [Test]
        public void should_delete_metadata_files_that_dont_have_a_coresponding_author()
        {
            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.ComicFileId = null)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_delete_metadata_files_that_dont_have_a_coresponding_book()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            Db.Insert(series);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.ComicFileId = null)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_metadata_files_that_have_a_coresponding_author()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            Db.Insert(series);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.IssueId = null)
                                                    .With(m => m.ComicFileId = null)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            var countMods = AllStoredModels.Count;
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_not_delete_metadata_files_that_have_a_coresponding_book()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            var issue = Builder<Issue>.CreateNew()
                .BuildNew();

            Db.Insert(series);
            Db.Insert(issue);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.IssueId = issue.Id)
                                                    .With(m => m.ComicFileId = null)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_metadata_files_that_dont_have_a_coresponding_track_file()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            var issue = Builder<Issue>.CreateNew()
                .BuildNew();

            Db.Insert(series);
            Db.Insert(issue);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.IssueId = issue.Id)
                                                    .With(m => m.ComicFileId = 10)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_metadata_files_that_have_a_coresponding_track_file()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            var issue = Builder<Issue>.CreateNew()
                                        .BuildNew();

            var trackFile = Builder<ComicFile>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .BuildNew();

            Db.Insert(series);
            Db.Insert(issue);
            Db.Insert(trackFile);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                    .With(m => m.SeriesId = series.Id)
                                                    .With(m => m.IssueId = issue.Id)
                                                    .With(m => m.ComicFileId = trackFile.Id)
                                                    .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_delete_book_metadata_files_that_have_bookid_of_zero()
        {
            var series = Builder<Series>.CreateNew()
                .BuildNew();

            Db.Insert(series);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                .With(m => m.SeriesId = series.Id)
                .With(m => m.Type = MetadataType.IssueMetadata)
                .With(m => m.IssueId = 0)
                .With(m => m.ComicFileId = null)
                .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }

        [Test]
        public void should_delete_book_image_files_that_have_bookid_of_zero()
        {
            var series = Builder<Series>.CreateNew()
                .BuildNew();

            Db.Insert(series);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                .With(m => m.SeriesId = series.Id)
                .With(m => m.Type = MetadataType.IssueImage)
                .With(m => m.IssueId = 0)
                .With(m => m.ComicFileId = null)
                .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }

        [Test]
        public void should_delete_track_metadata_files_that_have_trackfileid_of_zero()
        {
            var series = Builder<Series>.CreateNew()
                                        .BuildNew();

            Db.Insert(series);

            var metadataFile = Builder<MetadataFile>.CreateNew()
                                                 .With(m => m.SeriesId = series.Id)
                                                 .With(m => m.Type = MetadataType.IssueMetadata)
                                                 .With(m => m.ComicFileId = 0)
                                                 .BuildNew();

            Db.Insert(metadataFile);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(0);
        }
    }
}
