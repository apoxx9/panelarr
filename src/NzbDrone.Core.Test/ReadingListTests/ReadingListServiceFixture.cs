using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.ReadingLists;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ReadingListTests
{
    [TestFixture]
    public class ReadingListServiceFixture : CoreTest<ReadingListService>
    {
        private List<ReadingListItem> _storedSlots;

        [SetUp]
        public void Setup()
        {
            _storedSlots = new List<ReadingListItem>();

            Mocker.GetMock<IReadingListRepository>()
                  .Setup(r => r.Insert(It.IsAny<ReadingList>()))
                  .Returns<ReadingList>(a =>
                  {
                      a.Id = 1;
                      return a;
                  });

            Mocker.GetMock<IReadingListItemRepository>()
                  .Setup(r => r.InsertMany(It.IsAny<IList<ReadingListItem>>()))
                  .Callback<IList<ReadingListItem>>(slots => _storedSlots.AddRange(slots));

            Mocker.GetMock<IReadingListItemRepository>()
                  .Setup(r => r.FindByReadingListId(1))
                  .Returns(() => _storedSlots.OrderBy(s => s.Position).ToList());

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssues(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Issue>());

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.FindById(It.IsAny<string>()))
                  .Returns((Issue)null);
        }

        private void GivenProviderArc(params ComicVineArcIssue[] issues)
        {
            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(c => c.GetStoryArc(42))
                  .Returns(new ComicVineStoryArcSummary
                  {
                      Id = 42,
                      Name = "Knightfall",
                      Publisher = new ComicVineIdName { Id = 10, Name = "DC Comics" }
                  });

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(c => c.GetStoryArcIssues(42))
                  .Returns(issues.ToList());
        }

        private static ComicVineArcIssue ProviderIssue(int id, string volume, string number, string coverDate, string name = null)
        {
            return new ComicVineArcIssue
            {
                Id = id,
                Name = name,
                IssueNumber = number,
                CoverDate = coverDate,
                Volume = new ComicVineIdName { Id = id * 10, Name = volume }
            };
        }

        [Test]
        public void should_order_provider_slots_by_cover_date()
        {
            GivenProviderArc(
                ProviderIssue(3, "Batman", "486", "1992-11-01"),
                ProviderIssue(1, "Batman: Sword of Azrael", "1", "1992-10-01"),
                ProviderIssue(2, "Detective Comics", "654", "1992-10-15"));

            Subject.AddFromProvider(42, ReadingListType.Arc, out _);

            _storedSlots.Select(s => s.ForeignIssueId).Should().ContainInOrder("cv:1", "cv:2", "cv:3");
            _storedSlots.Select(s => s.Position).Should().ContainInOrder(1, 2, 3);
        }

        [Test]
        public void should_skip_collected_editions_and_report_count()
        {
            GivenProviderArc(
                ProviderIssue(1, "Batman", "484", "1992-10-01"),
                ProviderIssue(2, "Batman: Knightfall TPB", "1", "1993-01-01"),
                ProviderIssue(3, "Batman Knightfall Omnibus", "1", "2017-01-01"));

            Subject.AddFromProvider(42, ReadingListType.Arc, out var skipped);

            skipped.Should().Be(2);
            _storedSlots.Should().HaveCount(1);
            _storedSlots.Single().ForeignIssueId.Should().Be("cv:1");
        }

        [Test]
        public void should_reject_duplicate_provider_arc()
        {
            Mocker.GetMock<IReadingListRepository>()
                  .Setup(r => r.FindByForeignReadingListId("cv:42"))
                  .Returns(new ReadingList { Id = 7, ForeignReadingListId = "cv:42" });

            Assert.Throws<ArgumentException>(() => Subject.AddFromProvider(42, ReadingListType.Arc, out _));
        }

        [Test]
        public void should_resolve_slots_by_foreign_id()
        {
            GivenProviderArc(ProviderIssue(1, "Batman", "484", "1992-10-01"));

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.FindById("cv:1"))
                  .Returns(new Issue { Id = 555 });

            Subject.AddFromProvider(42, ReadingListType.Arc, out _);

            _storedSlots.Single().IssueId.Should().Be(555);
        }

        [Test]
        public void should_import_cbl_and_report_unresolved()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Test List</Name><Books>
<Book Series=""Batman"" Number=""484"" Volume=""1940"" Year=""1992""><Database Name=""cv"" Series=""796"" Issue=""36110"" /></Book>
<Book Series=""Unknown Series"" Number=""1"" />
</Books></ReadingList>";

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.FindById("cv:36110"))
                  .Returns(new Issue { Id = 900 });

            var arc = Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            arc.Name.Should().Be("Test List");
            _storedSlots.Should().HaveCount(2);
            _storedSlots[0].IssueId.Should().Be(900);
            _storedSlots[1].IssueId.Should().BeNull();
            unresolved.Should().HaveCount(1);
            unresolved.Single().Should().Contain("Unknown Series");
        }

        [Test]
        public void should_resolve_idless_cbl_entries_by_series_name_and_number()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Old List</Name><Books>
<Book Series=""Saga"" Number=""7"" />
</Books></ReadingList>";

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByName("Saga"))
                  .Returns(new Series { Id = 3 });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(3))
                  .Returns(new List<Issue> { new Issue { Id = 77, IssueNumber = "7" } });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            _storedSlots.Single().IssueId.Should().Be(77);
            unresolved.Should().BeEmpty();
        }

        [Test]
        public void should_unresolve_dangling_issue_links_on_resolve()
        {
            _storedSlots.Add(new ReadingListItem { Id = 1, ReadingListId = 1, Position = 1, IssueId = 999, ForeignIssueId = null, SeriesName = "Gone" });

            // issue 999 no longer exists in the library
            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssues(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Issue>());

            var slots = Subject.Resolve(1);

            slots.Single().IssueId.Should().BeNull();

            Mocker.GetMock<IReadingListItemRepository>()
                  .Verify(r => r.UpdateMany(It.Is<IList<ReadingListItem>>(l => l.Single().IssueId == null)), Times.Once);
        }

        [Test]
        public void should_export_resolved_slots_with_library_naming()
        {
            var metadata = new SeriesMetadata { Id = 5, Name = "Batman", ForeignSeriesId = "cv:796", Year = 1940 };
            var series = new Series { Id = 3, Metadata = metadata };
            var issue = new Issue { Id = 900, IssueNumber = "484", SeriesMetadataId = 5, Series = series };

            _storedSlots.Add(new ReadingListItem
            {
                Id = 1,
                ReadingListId = 1,
                Position = 1,
                IssueId = 900,
                ForeignIssueId = "cv:36110",
                SeriesName = "batman (old name)",
                IssueNumber = "484"
            });

            Mocker.GetMock<IReadingListRepository>()
                  .Setup(r => r.Get(1))
                  .Returns(new ReadingList { Id = 1, Name = "Knightfall" });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssues(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Issue> { issue });

            var xml = Subject.ExportCbl(1);

            xml.Should().Contain(@"Series=""Batman""");
            xml.Should().Contain(@"Volume=""1940""");
            xml.Should().Contain(@"Issue=""36110""");
            xml.Should().Contain(@"Series=""796""");
        }
    }
}
