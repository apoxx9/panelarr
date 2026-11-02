using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.ReadingLists;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ReadingListTests
{
    [TestFixture]
    public class ResolveProviderFallbackFixture : CoreTest<ReadingListService>
    {
        private const int ListId = 9;

        private void GivenSlot(string seriesName)
        {
            Mocker.GetMock<IReadingListItemRepository>()
                  .Setup(s => s.FindByReadingListId(ListId))
                  .Returns(new List<ReadingListItem>
                  {
                      new ReadingListItem { Id = 1, ReadingListId = ListId, SeriesName = seriesName, Position = 1 }
                  });
        }

        [Test]
        public void should_fall_back_to_search_when_the_name_filter_finds_nothing()
        {
            // The filter API answers nothing when a checklist spells a series
            // differently from ComicVine, which left the user with "not found"
            // and no candidates to choose from
            GivenSlot("Absolute Batman: Ark M Special");

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchVolumes("Absolute Batman: Ark M Special", null))
                  .Returns(new List<ComicVineVolumeSummary>());

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchSeries("Absolute Batman: Ark M Special"))
                  .Returns(new List<ComicVineVolumeSummary>
                  {
                      new ComicVineVolumeSummary { Id = 169970, Name = "Absolute Batman: Ark-M", StartYear = "2026" }
                  });

            var report = Subject.ResolveMissingProviderIds(ListId);

            report.NotFound.Should().BeEmpty();
            report.Ambiguous.Should().HaveCount(1);
            report.Ambiguous[0].Candidates.Should().Contain(c => c.ForeignSeriesId == "cv:169970");
        }

        [Test]
        public void should_not_auto_link_a_fuzzy_only_result()
        {
            // The name does not match, so it must still be the user's choice
            GivenSlot("Absolute Batman: Ark M Special");

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchVolumes(It.IsAny<string>(), It.IsAny<int?>()))
                  .Returns(new List<ComicVineVolumeSummary>());

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchSeries(It.IsAny<string>()))
                  .Returns(new List<ComicVineVolumeSummary>
                  {
                      new ComicVineVolumeSummary { Id = 169970, Name = "Absolute Batman: Ark-M", StartYear = "2026" }
                  });

            Subject.ResolveMissingProviderIds(ListId).Linked.Should().Be(0);
        }

        [Test]
        public void should_still_auto_link_an_exact_filter_match_without_searching()
        {
            GivenSlot("Absolute Batman: Ark-M");

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchVolumes("Absolute Batman: Ark-M", null))
                  .Returns(new List<ComicVineVolumeSummary>
                  {
                      new ComicVineVolumeSummary { Id = 169970, Name = "Absolute Batman: Ark-M", StartYear = "2026" }
                  });

            var report = Subject.ResolveMissingProviderIds(ListId);

            report.Linked.Should().Be(1);

            Mocker.GetMock<IComicVineApiClient>()
                  .Verify(s => s.SearchSeries(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_report_not_found_when_neither_call_returns_anything()
        {
            GivenSlot("Totally Obscure Series");

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchVolumes(It.IsAny<string>(), It.IsAny<int?>()))
                  .Returns(new List<ComicVineVolumeSummary>());

            Mocker.GetMock<IComicVineApiClient>()
                  .Setup(s => s.SearchSeries(It.IsAny<string>()))
                  .Returns(new List<ComicVineVolumeSummary>());

            Subject.ResolveMissingProviderIds(ListId).NotFound.Should().Contain("Totally Obscure Series");
        }
    }
}
