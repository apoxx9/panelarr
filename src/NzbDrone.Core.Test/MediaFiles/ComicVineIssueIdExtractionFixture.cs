using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Test.MediaFiles
{
    // Formats verified against a real Mylar/ComicTagger-tagged library:
    // Web url carries ".../4000-<id>/", Notes carries "[CVDB<id>]" (no
    // separator, ninjas.walk.alone ComicTagger fork) or "[Issue ID <id>]".
    [TestFixture]
    public class ComicVineIssueIdExtractionFixture
    {
        [TestCase("https://comicvine.gamespot.com/the-walking-dead-16-a-larger-world/4000-338482/", null, "cv:338482")]
        [TestCase("http://comicvine.gamespot.com/saga-1/4000-289693", null, "cv:289693")]
        [TestCase(null, "Tagged with the ninjas.walk.alone fork of ComicTagger 1.3.5 using info from Comic Vine on 2024-05-30 13:57:59.  [CVDB338482]", "cv:338482")]
        [TestCase(null, "Tagged with ComicTagger 1.6.0 using info from Comic Vine on 2023-01-01. [Issue ID 12345]", "cv:12345")]
        [TestCase(null, "[CVDB: 99]", "cv:99")]
        [TestCase(null, "[cvdb777]", "cv:777")]
        public void should_extract_comicvine_issue_id(string web, string notes, string expected)
        {
            MetadataTagService.ExtractComicVineIssueId(web, notes).Should().Be(expected);
        }

        [Test]
        public void web_should_win_over_notes()
        {
            MetadataTagService.ExtractComicVineIssueId(
                "https://comicvine.gamespot.com/x/4000-111/",
                "[CVDB222]").Should().Be("cv:111");
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("https://example.com/not-comicvine/4000-123/", "no ids here")]
        [TestCase("https://comicvine.gamespot.com/the-walking-dead/4050-30345/", null)] // volume url, not an issue
        public void should_return_null_when_no_issue_id(string web, string notes)
        {
            MetadataTagService.ExtractComicVineIssueId(web, notes).Should().BeNull();
        }
    }
}
