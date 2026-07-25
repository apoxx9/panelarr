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

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName(It.IsAny<string>()))
                  .Returns(new List<Series>());
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
            _storedSlots[0].ForeignSeriesId.Should().Be("cv:796");
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
                  .Setup(s => s.FindAllByName("Saga"))
                  .Returns(new List<Series> { new Series { Id = 3 } });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(3))
                  .Returns(new List<Issue> { new Issue { Id = 77, IssueNumber = "7" } });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            _storedSlots.Single().IssueId.Should().Be(77);
            unresolved.Should().BeEmpty();
        }

        [Test]
        public void should_fall_back_to_name_match_when_foreign_id_misses()
        {
            // The Shattered Grid case: a community CBL pointing at a wrong CV
            // id (the TPB record) while the same-named book is in the library.
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Simplified</Name><Books>
<Book Series=""Mighty Morphin Power Rangers: Shattered Grid"" Number=""1"" Volume=""2019"" Year=""2019""><Database Name=""cv"" Series=""120566"" Issue=""715246"" /></Book>
</Books></ReadingList>";

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName("Mighty Morphin Power Rangers: Shattered Grid"))
                  .Returns(new List<Series> { new Series { Id = 11, Metadata = new SeriesMetadata { Year = 2018 } } });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(11))
                  .Returns(new List<Issue> { new Issue { Id = 300, IssueNumber = "1" } });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            _storedSlots.Single().IssueId.Should().Be(300);
            unresolved.Should().BeEmpty();
        }

        [Test]
        public void should_retry_name_match_with_leading_the_toggled()
        {
            // Providers are inconsistent about a leading "The" across volumes
            // of one line (observed: 7 of 22 ASM Epic Collections are "The
            // Amazing..." on CV while the community lists say "Amazing...").
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Epics</Name><Books>
<Book Series=""Amazing Spider-Man Epic Collection: Great Power"" Number=""1"" />
<Book Series=""The Walking Dead"" Number=""1"" />
</Books></ReadingList>";

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName("The Amazing Spider-Man Epic Collection: Great Power"))
                  .Returns(new List<Series> { new Series { Id = 21 } });

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName("Walking Dead"))
                  .Returns(new List<Series> { new Series { Id = 22 } });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(21))
                  .Returns(new List<Issue> { new Issue { Id = 501, IssueNumber = "1" } });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(22))
                  .Returns(new List<Issue> { new Issue { Id = 502, IssueNumber = "1" } });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            _storedSlots[0].IssueId.Should().Be(501);
            _storedSlots[1].IssueId.Should().Be(502);
            unresolved.Should().BeEmpty();
        }

        [Test]
        public void should_export_only_resolved_slots_when_requested()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Checklist</Name><Books>
<Book Series=""Saga"" Number=""7"" />
<Book Series=""Unknown Series"" Number=""1"" />
</Books></ReadingList>";

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName("Saga"))
                  .Returns(new List<Series> { new Series { Id = 3 } });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(3))
                  .Returns(new List<Issue> { new Issue { Id = 77, IssueNumber = "7" } });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out _);

            Mocker.GetMock<IReadingListRepository>()
                  .Setup(r => r.Get(1))
                  .Returns(new ReadingList { Id = 1, Name = "Checklist" });

            var full = Subject.ExportCbl(1);
            var resolved = Subject.ExportCbl(1, resolvedOnly: true);

            System.Text.RegularExpressions.Regex.Matches(full, "<Book ").Count.Should().Be(2);
            System.Text.RegularExpressions.Regex.Matches(resolved, "<Book ").Count.Should().Be(1);
            resolved.Should().Contain("Saga").And.NotContain("Unknown Series");
        }

        [Test]
        public void should_backfill_provider_ids_for_unambiguous_matches_only()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Checklist</Name><Books>
<Book Series=""Aliens Epic Collection: The Original Years"" Number=""1"" />
<Book Series=""Power Rangers"" Number=""1"" />
<Book Series=""Totally Obscure Series"" Number=""1"" />
</Books></ReadingList>";

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out _);

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(c => c.SearchVolumes("Aliens Epic Collection: The Original Years", null))
                  .Returns(new List<ComicVineVolumeSummary>
                  {
                      new ComicVineVolumeSummary { Id = 111, Name = "Aliens Epic Collection: The Original Years", StartYear = "2023" }
                  });

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(c => c.SearchVolumes("Power Rangers", null))
                  .Returns(new List<ComicVineVolumeSummary>
                  {
                      new ComicVineVolumeSummary { Id = 222, Name = "Power Rangers", StartYear = "2016" },
                      new ComicVineVolumeSummary { Id = 333, Name = "Power Rangers", StartYear = "2020" }
                  });

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(c => c.SearchVolumes("Totally Obscure Series", null))
                  .Returns(new List<ComicVineVolumeSummary>());

            var report = Subject.ResolveMissingProviderIds(1);

            report.SlotsConsidered.Should().Be(3);
            report.Linked.Should().Be(1);
            _storedSlots[0].ForeignSeriesId.Should().Be("cv:111");

            // two same-named CV volumes: never guess — surface candidates
            report.Ambiguous.Should().HaveCount(1);
            report.Ambiguous.Single().SeriesName.Should().Be("Power Rangers");
            report.Ambiguous.Single().Candidates.Should().HaveCount(2);
            _storedSlots[1].ForeignSeriesId.Should().BeNull();

            report.NotFound.Should().ContainSingle(n => n == "Totally Obscure Series");
        }

        [Test]
        public void should_tolerate_leading_the_in_provider_match()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Epics</Name><Books>
<Book Series=""Amazing Spider-Man Epic Collection: Great Power"" Number=""1"" />
</Books></ReadingList>";

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out _);

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(c => c.SearchVolumes("Amazing Spider-Man Epic Collection: Great Power", null))
                  .Returns(new List<ComicVineVolumeSummary>
                  {
                      new ComicVineVolumeSummary { Id = 444, Name = "The Amazing Spider-Man Epic Collection: Great Power", StartYear = "2014" }
                  });

            var report = Subject.ResolveMissingProviderIds(1);

            report.Linked.Should().Be(1);
            _storedSlots.Single().ForeignSeriesId.Should().Be("cv:444");
        }

        [Test]
        public void should_skip_slots_that_already_have_provider_ids_or_are_resolved()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Mixed</Name><Books>
<Book Series=""Batman"" Number=""484""><Database Name=""cv"" Series=""796"" Issue=""36110"" /></Book>
</Books></ReadingList>";

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out _);

            var report = Subject.ResolveMissingProviderIds(1);

            report.SlotsConsidered.Should().Be(0);
            Mocker.GetMock<IComicVineApiClient>()
                  .Verify(c => c.SearchVolumes(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Test]
        public void should_link_slot_series_manually()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>Pick</Name><Books>
<Book Series=""Power Rangers"" Number=""1"" />
</Books></ReadingList>";

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out _);
            _storedSlots.Single().Id = 77;

            var slot = Subject.LinkSlotSeries(1, 77, "cv:333");

            slot.ForeignSeriesId.Should().Be("cv:333");
            _storedSlots.Single().ForeignSeriesId.Should().Be("cv:333");
        }

        [Test]
        public void should_disambiguate_same_named_series_by_year()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>PR</Name><Books>
<Book Series=""Power Rangers"" Number=""1"" Volume=""2020"" Year=""2020"" />
</Books></ReadingList>";

            var pr2016 = new Series { Id = 1, Metadata = new SeriesMetadata { Year = 2016 } };
            var pr2020 = new Series { Id = 2, Metadata = new SeriesMetadata { Year = 2020 } };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName("Power Rangers"))
                  .Returns(new List<Series> { pr2016, pr2020 });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(2))
                  .Returns(new List<Issue> { new Issue { Id = 400, IssueNumber = "1" } });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            _storedSlots.Single().IssueId.Should().Be(400);
            unresolved.Should().BeEmpty();
        }

        [Test]
        public void should_stay_unresolved_when_same_named_series_are_ambiguous()
        {
            var cbl = @"<?xml version=""1.0""?><ReadingList><Name>PR</Name><Books>
<Book Series=""Power Rangers"" Number=""1"" />
</Books></ReadingList>";

            var pr2016 = new Series { Id = 1, Metadata = new SeriesMetadata { Year = 2016 } };
            var pr2020 = new Series { Id = 2, Metadata = new SeriesMetadata { Year = 2020 } };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName("Power Rangers"))
                  .Returns(new List<Series> { pr2016, pr2020 });

            Subject.ImportCbl(cbl, ReadingListType.ReadingOrder, out var unresolved);

            _storedSlots.Single().IssueId.Should().BeNull();
            unresolved.Should().HaveCount(1);
        }

        [Test]
        public void should_remap_slot_and_rewrite_identity()
        {
            var metadata = new SeriesMetadata { Id = 5, Name = "Mighty Morphin Power Rangers: Shattered Grid", ForeignSeriesId = "cv:113084", Year = 2018 };
            var series = new Series { Id = 11, Metadata = metadata };
            var issue = new Issue
            {
                Id = 300,
                IssueNumber = "1",
                ForeignIssueId = "cv:682536",
                ReleaseDate = new DateTime(2018, 8, 29),
                Series = series
            };

            var slot = new ReadingListItem
            {
                Id = 9,
                ReadingListId = 1,
                Position = 46,
                ForeignIssueId = "cv:715246",
                ForeignSeriesId = "cv:120566",
                SeriesName = "Mighty Morphin Power Rangers: Shattered Grid",
                IssueNumber = "1",
                Volume = "2019",
                Year = "2019"
            };

            Mocker.GetMock<IReadingListItemRepository>()
                  .Setup(r => r.Get(9))
                  .Returns(slot);

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssue(300))
                  .Returns(issue);

            var updated = Subject.RemapSlot(1, 9, 300);

            updated.IssueId.Should().Be(300);
            updated.ForeignIssueId.Should().Be("cv:682536");
            updated.ForeignSeriesId.Should().Be("cv:113084");
            updated.Volume.Should().Be("2018");
            updated.Year.Should().Be("2018");

            Mocker.GetMock<IReadingListItemRepository>()
                  .Verify(r => r.Update(It.Is<ReadingListItem>(s => s.ForeignIssueId == "cv:682536")), Times.Once);
        }

        [Test]
        public void should_reject_remap_for_a_slot_of_another_list()
        {
            Mocker.GetMock<IReadingListItemRepository>()
                  .Setup(r => r.Get(9))
                  .Returns(new ReadingListItem { Id = 9, ReadingListId = 2 });

            Assert.Throws<ArgumentException>(() => Subject.RemapSlot(1, 9, 300));
        }

        [Test]
        public void should_export_resolved_slots_with_the_library_issue_id()
        {
            // A slot resolved past a bad community id exports REPAIRED.
            var metadata = new SeriesMetadata { Id = 5, Name = "Mighty Morphin Power Rangers: Shattered Grid", ForeignSeriesId = "cv:113084", Year = 2018 };
            var series = new Series { Id = 11, Metadata = metadata };
            var issue = new Issue { Id = 300, IssueNumber = "1", ForeignIssueId = "cv:682536", SeriesMetadataId = 5, Series = series };

            _storedSlots.Add(new ReadingListItem
            {
                Id = 1,
                ReadingListId = 1,
                Position = 1,
                IssueId = 300,
                ForeignIssueId = "cv:715246",
                SeriesName = "Mighty Morphin Power Rangers: Shattered Grid",
                IssueNumber = "1",
                Volume = "2019",
                Year = "2019"
            });

            Mocker.GetMock<IReadingListRepository>()
                  .Setup(r => r.Get(1))
                  .Returns(new ReadingList { Id = 1, Name = "Simplified" });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssues(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Issue> { issue });

            var xml = Subject.ExportCbl(1);

            xml.Should().Contain(@"Issue=""682536""");
            xml.Should().Contain(@"Series=""113084""");
            xml.Should().NotContain("715246");
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
        public void should_export_unresolved_slots_with_stored_ids()
        {
            _storedSlots.Add(new ReadingListItem
            {
                Id = 1,
                ReadingListId = 1,
                Position = 1,
                ForeignIssueId = "cv:107898",
                ForeignSeriesId = "cv:9223",
                SeriesName = "Batman: Sword of Azrael",
                IssueNumber = "1",
                Volume = "1992",
                Year = "1992"
            });

            Mocker.GetMock<IReadingListRepository>()
                  .Setup(r => r.Get(1))
                  .Returns(new ReadingList { Id = 1, Name = "Knightfall" });

            var xml = Subject.ExportCbl(1);

            xml.Should().Contain(@"Issue=""107898""");
            xml.Should().Contain(@"Series=""9223""");
            xml.Should().Contain(@"Volume=""1992""");
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
