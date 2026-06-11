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
    public class IssueGroupingServiceFixture : CoreTest<TrackGroupingService>
    {
        private List<LocalIssue> GivenTracks(string root, string series, string issue, int count)
        {
            var fileInfos = Builder<ParsedFileTagInfo>
                .CreateListOfSize(count)
                .All()
                .With(f => f.Series = new List<string> { series })
                .With(f => f.SeriesTitle = series)
                .With(f => f.IssueTitle = issue)
                .Build();

            var tracks = fileInfos.Select(x => Builder<LocalIssue>
                                          .CreateNew()
                                          .With(y => y.FileTagInfo = x)
                                          .With(y => y.Path = Path.Combine(root, x.Title))
                                          .Build()).ToList();

            return tracks;
        }

        private List<LocalIssue> GivenVaTracks(string root, string issue, int count)
        {
            var settings = new BuilderSettings();
            settings.SetPropertyNamerFor<ParsedFileTagInfo>(new RandomValueNamerShortStrings(settings));

            var builder = new Builder(settings);

            var fileInfos = builder
                .CreateListOfSize<ParsedFileTagInfo>(count)
                .All()
                .With(f => f.IssueTitle = "issue")
                .Build();

            var tracks = fileInfos.Select(x => Builder<LocalIssue>
                                          .CreateNew()
                                          .With(y => y.FileTagInfo = x)
                                          .With(y => y.Path = Path.Combine(@"C:\comics\incoming".AsOsAgnostic(), x.Title))
                                          .Build()).ToList();

            return tracks;
        }

        // Comics never group multiple archives into one item: two variants of
        // the same issue in one scan must each get their own decision.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void every_file_should_be_its_own_release(int count)
        {
            var tracks = GivenTracks(@"C:\comics\incoming".AsOsAgnostic(), "series", "issue", count);

            var output = Subject.GroupTracks(tracks);

            output.Should().HaveCount(count);
            output.Should().OnlyContain(e => e.LocalIssues.Count == 1);
        }

        [Test]
        public void same_issue_variants_in_one_folder_should_not_fuse()
        {
            var dir = @"C:\comics\incoming\Saga (2012)".AsOsAgnostic();
            var tracks = GivenTracks(dir, "Saga", "Chapter Three", 1);
            tracks.AddRange(GivenTracks(dir, "Saga", "Chapter Three", 1));

            var output = Subject.GroupTracks(tracks);

            output.Should().HaveCount(2);
            output.SelectMany(e => e.LocalIssues).Should().BeEquivalentTo(tracks);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void single_series_is_not_various(int count)
        {
            var tracks = GivenTracks(@"C:\comics\incoming".AsOsAgnostic(), "series", "issue", count);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
        }

        // GivenVaTracks uses random names so repeat multiple times to try to prompt any intermittent failures
        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void all_different_series_is_various_series()
        {
            var tracks = GivenVaTracks(@"C:\comics\incoming".AsOsAgnostic(), "issue", 10);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(true);
        }

        [Test]
        public void two_series_is_not_various_series()
        {
            var dir = @"C:\comics\incoming".AsOsAgnostic();
            var tracks = GivenTracks(dir, "author1", "issue", 10);
            tracks.AddRange(GivenTracks(dir, "author2", "issue", 10));

            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
        }

        [Ignore("TODO: fix")]
        [Test]
        [Repeat(100)]
        public void mostly_different_series_is_various_series()
        {
            var dir = @"C:\comics\incoming".AsOsAgnostic();
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
            var tracks = GivenTracks(@"C:\comics\incoming".AsOsAgnostic(), series, "issue", 10);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(true);
        }

        [TestCase("Va?!")]
        [TestCase("Va Va Voom")]
        [TestCase("V.A. Jr.")]
        [TestCase("Ca Va")]
        public void va_in_series_name_is_not_various(string series)
        {
            var tracks = GivenTracks(@"C:\comics\incoming".AsOsAgnostic(), series, "issue", 10);
            TrackGroupingService.IsVariousSeries(tracks).Should().Be(false);
        }
    }
}
