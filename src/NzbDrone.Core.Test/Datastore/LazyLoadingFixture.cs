using System.Linq;
using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class LazyLoadingFixture : DbTest
    {
        [SetUp]
        public void Setup()
        {
            SqlBuilderExtensions.LogSql = true;

            var profile = new QualityProfile
            {
                Name = "Test",
                Cutoff = Quality.CBR.Id,
                Items = Qualities.QualityFixture.GetDefaultQualities()
            };

            profile = Db.Insert(profile);

            var metadata = Builder<SeriesMetadata>.CreateNew()
                .With(v => v.Id = 0)
                .Build();
            Db.Insert(metadata);

            var series = Builder<Series>.CreateListOfSize(1)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.QualityProfileId = profile.Id)
                .With(v => v.SeriesMetadataId = metadata.Id)
                .BuildListOfNew();

            Db.InsertMany(series);

            var issues = Builder<Issue>.CreateListOfSize(3)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.SeriesMetadataId = metadata.Id)
                .BuildListOfNew();

            Db.InsertMany(issues);

            var trackFiles = Builder<ComicFile>.CreateListOfSize(1)
                .All()
                .With(v => v.Id = 0)
                .With(v => v.IssueId = issues[0].Id)
                .With(v => v.Quality = new QualityModel())
                .BuildListOfNew();

            Db.InsertMany(trackFiles);
        }

        [Test]
        public void should_lazy_load_author_for_trackfile()
        {
            var db = Mocker.Resolve<IDatabase>();
            var tracks = db.Query<ComicFile>(new SqlBuilder(db.DatabaseType)).ToList();

            Assert.IsNotEmpty(tracks);
            foreach (var track in tracks)
            {
                Assert.IsFalse(track.Series.IsLoaded);
                Assert.IsNotNull(track.Series.Value);
                Assert.IsTrue(track.Series.IsLoaded);
                Assert.IsTrue(track.Series.Value.Metadata.IsLoaded);
            }
        }

        [Test]
        public void should_lazy_load_trackfile_if_not_joined()
        {
            var db = Mocker.Resolve<IDatabase>();
            var tracks = db.Query<Issue>(new SqlBuilder(db.DatabaseType)).ToList();

            foreach (var track in tracks)
            {
                Assert.IsFalse(track.ComicFiles.IsLoaded);
                Assert.IsNotNull(track.ComicFiles.Value);
                Assert.IsTrue(track.ComicFiles.IsLoaded);
            }
        }

        [Test]
        public void should_explicit_load_everything_if_joined()
        {
            var db = Mocker.Resolve<IDatabase>();
            var files = MediaFileRepository.Query(db,
                                                  new SqlBuilder(db.DatabaseType)
                                                  .Join<ComicFile, Issue>((t, b) => t.IssueId == b.Id)
                                                  .Join<Issue, Series>((issue, series) => issue.SeriesMetadataId == series.SeriesMetadataId)
                                                  .Join<Series, SeriesMetadata>((a, m) => a.SeriesMetadataId == m.Id));

            Assert.IsNotEmpty(files);
            foreach (var file in files)
            {
                Assert.IsTrue(file.Issue.IsLoaded);
                Assert.IsTrue(file.Series.IsLoaded);
                Assert.IsTrue(file.Series.Value.Metadata.IsLoaded);
            }
        }
    }
}
