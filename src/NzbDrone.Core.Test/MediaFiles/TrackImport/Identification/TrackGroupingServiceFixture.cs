using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FizzWare.NBuilder;
using FizzWare.NBuilder.PropertyNaming;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    // we need to use random strings to test the va (so we don't just get author1, author2 etc which are too similar)
    // but the standard random value namer would give paths that are too long on windows
    public class RandomValueNamerShortStrings : RandomValuePropertyNamer
    {
        private static readonly List<char> AllowedChars;
        private readonly IRandomGenerator _generator;

        public RandomValueNamerShortStrings(BuilderSettings settings)
            : base(settings)
        {
            _generator = new RandomGenerator();
        }

        static RandomValueNamerShortStrings()
        {
            AllowedChars = new List<char>();
            for (var c = 'a'; c < 'z'; c++)
            {
                AllowedChars.Add(c);
            }

            for (var c = 'A'; c < 'Z'; c++)
            {
                AllowedChars.Add(c);
            }

            for (var c = '0'; c < '9'; c++)
            {
                AllowedChars.Add(c);
            }
        }

        protected override string GetString(MemberInfo memberInfo)
        {
            var length = _generator.Next(1, 100);

            var chars = new char[length];

            for (var i = 0; i < length; i++)
            {
                var index = _generator.Next(0, AllowedChars.Count - 1);
                chars[i] = AllowedChars[index];
            }

            var bytes = Encoding.UTF8.GetBytes(chars);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }

    [TestFixture]
    public class TrackGroupingServiceFixture : CoreTest<TrackGroupingService>
    {
        private List<LocalIssue> GivenTracks(string root, string series, string issue, int count)
        {
            var fileInfos = Builder<ParsedTrackInfo>
                .CreateListOfSize(count)
                .All()
                .With(f => f.Series = new List<string> { series })
                .With(f => f.SeriesTitle = series)
                .With(f => f.IssueTitle = issue)
                .With(f => f.IssueMBId = null)
                .With(f => f.ReleaseMBId = null)
                .Build();

            var tracks = fileInfos.Select(x => Builder<LocalIssue>
                                          .CreateNew()
                                          .With(y => y.FileTrackInfo = x)
                                          .With(y => y.Path = Path.Combine(root, x.Title))
                                          .Build()).ToList();

            return tracks;
        }

        private List<LocalIssue> GivenTracksWithNoTags(string root, int count)
        {
            var outp = new List<LocalIssue>();

            for (var i = 0; i < count; i++)
            {
                var track = Builder<LocalIssue>
                    .CreateNew()
                    .With(y => y.FileTrackInfo = new ParsedTrackInfo())
                    .With(y => y.Path = Path.Combine(root, $"{i}.mp3"))
                    .Build();
                outp.Add(track);
            }

            return outp;
        }

        [Repeat(100)]
        private List<LocalIssue> GivenVaTracks(string root, string issue, int count)
        {
            var settings = new BuilderSettings();
            settings.SetPropertyNamerFor<ParsedTrackInfo>(new RandomValueNamerShortStrings(settings));

            var builder = new Builder(settings);

            var fileInfos = builder
                .CreateListOfSize<ParsedTrackInfo>(count)
                .All()
                .With(f => f.IssueTitle = "issue")
                .With(f => f.IssueMBId = null)
                .With(f => f.ReleaseMBId = null)
                .Build();

            var tracks = fileInfos.Select(x => Builder<LocalIssue>
                                          .CreateNew()
                                          .With(y => y.FileTrackInfo = x)
                                          .With(y => y.Path = Path.Combine(@"C:\music\incoming".AsOsAgnostic(), x.Title))
                                          .Build()).ToList();

            return tracks;
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void single_series_is_not_various(int count)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), "series", "issue", count);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
        }

        // GivenVaTracks uses random names so repeat multiple times to try to prompt any intermittent failures
        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void all_different_series_is_various_series()
        {
            var tracks = GivenVaTracks(@"C:\music\incoming".AsOsAgnostic(), "issue", 10);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(true);
        }

        [Test]
        public void two_series_is_not_various_series()
        {
            var dir = @"C:\music\incoming".AsOsAgnostic();
            var tracks = GivenTracks(dir, "author1", "issue", 10);
            tracks.AddRange(GivenTracks(dir, "author2", "issue", 10));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
        }

        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void mostly_different_series_is_various_series()
        {
            var dir = @"C:\music\incoming".AsOsAgnostic();
            var tracks = GivenVaTracks(dir, "issue", 10);
            tracks.AddRange(GivenTracks(dir, "single_series", "issue", 2));
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(true);
        }

        [TestCase("")]
        [TestCase("Various Series")]
        [TestCase("Various")]
        [TestCase("VA")]
        [TestCase("Unknown")]
        public void va_series_title_is_various(string series)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), series, "issue", 10);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(true);
        }

        [TestCase("Va?!")]
        [TestCase("Va Va Voom")]
        [TestCase("V.A. Jr.")]
        [TestCase("Ca Va")]
        public void va_in_series_name_is_not_various(string series)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), series, "issue", 10);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void should_group_single_author_book(int count)
        {
            var tracks = GivenTracks(@"C:\music\incoming".AsOsAgnostic(), "series", "issue", count);
            var output = Subject.GroupTracks(tracks);

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(count);
        }

        [TestCase("cd")]
        [TestCase("disc")]
        [TestCase("disk")]
        public void should_group_multi_disc_release(string mediaName)
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\series - issue\\{mediaName} 1".AsOsAgnostic(), "series", "issue", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\series - issue\\{mediaName} 2".AsOsAgnostic(), "series", "issue", 5));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(15);
        }

        [Test]
        public void should_not_group_two_different_books_by_same_author()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\series - book1".AsOsAgnostic(), "series", "book1", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\series - book2".AsOsAgnostic(), "series", "book2", 5));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Test]
        public void should_group_books_with_typos()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\series - issue".AsOsAgnostic(), "series", "Rastaman Vibration (Remastered)", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\series - issue".AsOsAgnostic(), "series", "Rastaman Vibration (Remastered", 5));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(15);
        }

        [Test]
        public void should_not_group_two_different_tracks_in_same_directory()
        {
            var tracks = GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "series", "book1", 1);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "series", "book2", 1));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(1);
            output[1].LocalIssues.Count.Should().Be(1);
        }

        [Test]
        public void should_separate_two_books_in_same_directory()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\series discog".AsOsAgnostic(), "series", "book1", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\series disog".AsOsAgnostic(), "series", "book2", 5));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Test]
        public void should_separate_many_books_in_same_directory()
        {
            var tracks = new List<LocalIssue>();
            for (var i = 0; i < 100; i++)
            {
                tracks.AddRange(GivenTracks($"C:\\music".AsOsAgnostic(), "series" + i, "issue" + i, 10));
            }

            // don't test various allSeries here because it's designed to only work if there's a common issue
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(100);
            output.Select(x => x.LocalIssues.Count).Distinct().Should().BeEquivalentTo(new List<int> { 10 });
        }

        [Test]
        public void should_separate_two_books_by_different_series_in_same_directory()
        {
            var tracks = GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "author1", "book1", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming".AsOsAgnostic(), "author2", "book2", 5));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void should_group_va_release()
        {
            var tracks = GivenVaTracks(@"C:\music\incoming".AsOsAgnostic(), "issue", 10);

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(true);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(10);
        }

        [Test]
        public void should_not_group_two_books_by_different_series_with_same_title()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "author1", "issue", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "author2", "issue", 5));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);

            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(5);
        }

        [Test]
        public void should_not_fail_if_all_tags_null()
        {
            var tracks = GivenTracksWithNoTags($"C:\\music\\incoming\\issue".AsOsAgnostic(), 10);

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(10);
        }

        [Test]
        public void should_not_fail_if_some_tags_null()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "author1", "issue", 10);
            tracks.AddRange(GivenTracksWithNoTags($"C:\\music\\incoming\\issue".AsOsAgnostic(), 2));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(true);

            var output = Subject.GroupTracks(tracks);
            output.Count.Should().Be(1);
            output[0].LocalIssues.Count.Should().Be(12);
        }

        [Test]
        public void should_cope_with_one_book_in_subfolder_of_another()
        {
            var tracks = GivenTracks($"C:\\music\\incoming\\issue".AsOsAgnostic(), "author1", "issue", 10);
            tracks.AddRange(GivenTracks($"C:\\music\\incoming\\issue\\anotherbook".AsOsAgnostic(), "author2", "book2", 10));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
            TrackGroupingService.LooksLikeSingleRelease(tracks).Should().Be(false);

            var output = Subject.GroupTracks(tracks);

            foreach (var group in output)
            {
                TestLogger.Debug($"*** group {group} ***");
                TestLogger.Debug(string.Join("\n", group.LocalIssues.Select(x => x.Path)));
            }

            output.Count.Should().Be(2);
            output[0].LocalIssues.Count.Should().Be(10);
            output[1].LocalIssues.Count.Should().Be(10);
        }
    }
}
