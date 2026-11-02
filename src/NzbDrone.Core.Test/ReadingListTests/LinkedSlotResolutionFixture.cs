using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.ReadingLists;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ReadingListTests
{
    [TestFixture]
    public class LinkedSlotResolutionFixture : CoreTest<ReadingListService>
    {
        private const int ListId = 4;

        private Series _series;
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            // The library holds ComicVine's spelling; the checklist holds its own
            _series = new Series { Id = 1429, Name = "Absolute Batman: Ark-M", CleanName = "absolutebatmanarkm" };
            _issue = new Issue { Id = 13455, SeriesId = 1429, IssueNumber = "1" };

            Mocker.GetMock<IReadingListRepository>()
                  .Setup(s => s.Get(ListId))
                  .Returns(new ReadingList { Id = ListId, Name = "DC Absolute Universe" });

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById("cv:169970"))
                  .Returns(_series);

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(1429))
                  .Returns(new List<Issue> { _issue });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssues(It.IsAny<List<int>>()))
                  .Returns(new List<Issue> { _issue });

            // Tier 2 is reached whenever the link does not resolve; the
            // checklist's own name matches nothing in the library
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindAllByName(It.IsAny<string>()))
                  .Returns(new List<Series>());
        }

        private void GivenSlot(string foreignSeriesId, string issueNumber = "1")
        {
            Mocker.GetMock<IReadingListItemRepository>()
                  .Setup(s => s.FindByReadingListId(ListId))
                  .Returns(new List<ReadingListItem>
                  {
                      new ReadingListItem
                      {
                          Id = 2590,
                          ReadingListId = ListId,
                          Position = 76,
                          SeriesName = "Absolute Batman: Ark M Special",
                          IssueNumber = issueNumber,
                          ForeignSeriesId = foreignSeriesId
                      }
                  });
        }

        [Test]
        public void should_resolve_a_slot_through_the_series_the_user_linked()
        {
            // Nothing read ForeignSeriesId before, so linking a slot and adding
            // the series left the slot stuck on notInLibrary forever
            GivenSlot("cv:169970");

            Subject.Resolve(ListId).Single().IssueId.Should().Be(13455);
        }

        [Test]
        public void should_not_resolve_when_the_linked_series_is_not_in_the_library()
        {
            GivenSlot("cv:999999");

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById("cv:999999"))
                  .Returns((Series)null);

            Subject.Resolve(ListId).Single().IssueId.Should().BeNull();
        }

        [Test]
        public void should_not_resolve_when_the_linked_series_lacks_that_issue_number()
        {
            GivenSlot("cv:169970", issueNumber: "7");

            Subject.Resolve(ListId).Single().IssueId.Should().BeNull();
        }
    }
}
