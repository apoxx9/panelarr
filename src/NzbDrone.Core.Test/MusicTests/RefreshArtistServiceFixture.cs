using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class RefreshSeriesServiceFixture : CoreTest<RefreshSeriesService>
    {
        private Series _author;
        private Issue _book1;
        private Issue _book2;
        private List<Issue> _books;
        private List<Issue> _remoteBooks;

        [SetUp]
        public void Setup()
        {
            _book1 = Builder<Issue>.CreateNew()
                .With(s => s.ForeignIssueId = "1")
                .Build();

            _book2 = Builder<Issue>.CreateNew()
                .With(s => s.ForeignIssueId = "2")
                .Build();

            _books = new List<Issue> { _book1, _book2 };

            _remoteBooks = _books.JsonClone();
            _remoteBooks.ForEach(x => x.Id = 0);

            var metadata = Builder<SeriesMetadata>.CreateNew().Build();
            var series = Builder<SeriesGroup>.CreateListOfSize(1).BuildList();

            _author = Builder<Series>.CreateNew()
                .With(a => a.Metadata = metadata)
                .With(a => a.SeriesGroups = series)
                .Build();

            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                  .Setup(s => s.GetSeriess(new List<int> { _author.Id }))
                  .Returns(new List<Series> { _author });

            Mocker.GetMock<IBookService>(MockBehavior.Strict)
                .Setup(s => s.InsertMany(It.IsAny<List<Issue>>()));

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.FilterBooks(It.IsAny<Series>(), It.IsAny<int>()))
                .Returns(_remoteBooks);

            Mocker.GetMock<IProvideSeriesInfo>()
                .Setup(s => s.GetSeriesInfo(It.IsAny<string>(), true))
                .Callback(() => { throw new SeriesNotFoundException(_author.ForeignSeriesId); });

            Mocker.GetMock<IMediaFileService>()
                .Setup(x => x.GetFilesBySeries(It.IsAny<int>()))
                .Returns(new List<ComicFile>());

            Mocker.GetMock<IHistoryService>()
                .Setup(x => x.GetBySeries(It.IsAny<int>(), It.IsAny<EntityHistoryEventType?>()))
                .Returns(new List<EntityHistory>());

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(x => x.FindByForeignId(It.IsAny<List<string>>()))
                .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IRootFolderService>()
                .Setup(x => x.All())
                .Returns(new List<RootFolder>());

            Mocker.GetMock<IMonitorNewBookService>()
                .Setup(x => x.ShouldMonitorNewBook(It.IsAny<Issue>(), It.IsAny<List<Issue>>(), It.IsAny<NewItemMonitorTypes>()))
                .Returns(true);
        }

        private void GivenNewSeriesInfo(Series author)
        {
            Mocker.GetMock<IProvideSeriesInfo>()
                .Setup(s => s.GetSeriesInfo(_author.ForeignSeriesId, true))
                .Returns(author);
        }

        private void GivenSeriesFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(x => x.GetFilesBySeries(It.IsAny<int>()))
                  .Returns(Builder<ComicFile>.CreateListOfSize(1).BuildList());
        }

        private void GivenBooksForRefresh(List<Issue> issues)
        {
            Mocker.GetMock<IBookService>(MockBehavior.Strict)
                .Setup(s => s.GetBooksForRefresh(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns(issues);
        }

        private void AllowSeriesUpdate()
        {
            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .Setup(x => x.UpdateSeries(It.IsAny<Series>()))
                .Returns((Series a) => a);
        }

        [Test]
        public void should_not_publish_author_updated_event_if_metadata_not_updated()
        {
            var newSeriesInfo = _author.JsonClone();
            newSeriesInfo.Metadata = _author.Metadata.Value.JsonClone();
            newSeriesInfo.Books = _remoteBooks;

            GivenNewSeriesInfo(newSeriesInfo);
            GivenBooksForRefresh(_books);
            AllowSeriesUpdate();

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            VerifyEventNotPublished<SeriesUpdatedEvent>();
            VerifyEventPublished<SeriesRefreshCompleteEvent>();
        }

        [Test]
        public void should_publish_author_updated_event_if_metadata_updated()
        {
            var newSeriesInfo = _author.JsonClone();
            newSeriesInfo.Metadata = _author.Metadata.Value.JsonClone();
            newSeriesInfo.Metadata.Value.Images = new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover(MediaCover.MediaCoverTypes.Logo, "dummy")
            };
            newSeriesInfo.Books = _remoteBooks;

            GivenNewSeriesInfo(newSeriesInfo);
            GivenBooksForRefresh(new List<Issue>());
            AllowSeriesUpdate();

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            VerifyEventPublished<SeriesUpdatedEvent>();
            VerifyEventPublished<SeriesRefreshCompleteEvent>();
        }

        [Test]
        public void should_call_new_book_monitor_service_when_adding_book()
        {
            var newBook = Builder<Issue>.CreateNew()
                .With(x => x.Id = 0)
                .With(x => x.ForeignIssueId = "3")
                .Build();
            _remoteBooks.Add(newBook);

            var newSeriesInfo = _author.JsonClone();
            newSeriesInfo.Metadata = _author.Metadata.Value.JsonClone();
            newSeriesInfo.Books = _remoteBooks;

            GivenNewSeriesInfo(newSeriesInfo);
            GivenBooksForRefresh(_books);
            AllowSeriesUpdate();

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            Mocker.GetMock<IMonitorNewBookService>()
                .Verify(x => x.ShouldMonitorNewBook(newBook, _books, _author.MonitorNewItems), Times.Once());
        }

        [Test]
        public void should_log_error_and_delete_if_musicbrainz_id_not_found_and_author_has_no_files()
        {
            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.DeleteSeries(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()));

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.IsAny<Series>()), Times.Never());

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.DeleteSeries(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_log_error_but_not_delete_if_musicbrainz_id_not_found_and_author_has_files()
        {
            GivenSeriesFiles();
            GivenBooksForRefresh(new List<Issue>());

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.IsAny<Series>()), Times.Never());

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.DeleteSeries(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            ExceptionVerification.ExpectedErrors(2);
        }

        [Test]
        public void should_update_if_musicbrainz_id_changed_and_no_clash()
        {
            var newSeriesInfo = _author.JsonClone();
            newSeriesInfo.Metadata = _author.Metadata.Value.JsonClone();
            newSeriesInfo.Books = _remoteBooks;
            newSeriesInfo.ForeignSeriesId = _author.ForeignSeriesId + 1;
            newSeriesInfo.Metadata.Value.Id = 100;

            GivenNewSeriesInfo(newSeriesInfo);

            var seq = new MockSequence();

            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .Setup(x => x.FindById(newSeriesInfo.ForeignSeriesId))
                .Returns(default(Series));

            // Make sure that the author is updated before we refresh the issues
            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateSeries(It.IsAny<Series>()))
                .Returns((Series a) => a);

            Mocker.GetMock<IBookService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetBooksForRefresh(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns(new List<Issue>());

            // Update called twice for a move/merge
            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateSeries(It.IsAny<Series>()))
                .Returns((Series a) => a);

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.Is<Series>(s => s.SeriesMetadataId == 100 && s.ForeignSeriesId == newSeriesInfo.ForeignSeriesId)),
                        Times.Exactly(2));
        }

        [Test]
        public void should_merge_if_musicbrainz_id_changed_and_new_id_already_exists()
        {
            var existing = _author;

            var clash = _author.JsonClone();
            clash.Id = 100;
            clash.Metadata = existing.Metadata.Value.JsonClone();
            clash.Metadata.Value.Id = 101;
            clash.Metadata.Value.ForeignSeriesId = clash.Metadata.Value.ForeignSeriesId + 1;

            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .Setup(x => x.FindById(clash.Metadata.Value.ForeignSeriesId))
                .Returns(clash);

            var newSeriesInfo = clash.JsonClone();
            newSeriesInfo.Metadata = clash.Metadata.Value.JsonClone();
            newSeriesInfo.Books = _remoteBooks;

            GivenNewSeriesInfo(newSeriesInfo);

            var seq = new MockSequence();

            // Make sure that the author is updated before we refresh the issues
            Mocker.GetMock<IBookService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetBooksBySeries(existing.Id))
                .Returns(_books);

            Mocker.GetMock<IBookService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateMany(It.IsAny<List<Issue>>()));

            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.DeleteSeries(existing.Id, It.IsAny<bool>(), false));

            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateSeries(It.Is<Series>(a => a.Id == clash.Id)))
                .Returns((Series a) => a);

            Mocker.GetMock<IBookService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetBooksForRefresh(clash.SeriesMetadataId, It.IsAny<List<string>>()))
                .Returns(_books);

            // Update called twice for a move/merge
            Mocker.GetMock<ISeriesService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateSeries(It.IsAny<Series>()))
                .Returns((Series a) => a);

            Subject.Execute(new RefreshSeriesCommand(_author.Id));

            // the retained author gets updated
            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.Is<Series>(s => s.Id == clash.Id)), Times.Exactly(2));

            // the old one gets removed
            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.DeleteSeries(existing.Id, false, false));

            Mocker.GetMock<IBookService>()
                .Verify(v => v.UpdateMany(It.Is<List<Issue>>(x => x.Count == _books.Count)));

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
