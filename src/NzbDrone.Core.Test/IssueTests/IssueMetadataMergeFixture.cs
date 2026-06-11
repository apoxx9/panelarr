using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Test.IssueTests
{
    // Refresh maps issues from the providers' list endpoints, which omit
    // covers/page counts/descriptions; a refresh must not blank values that
    // were populated from the detail endpoints.
    [TestFixture]
    public class IssueMetadataMergeFixture
    {
        private Issue GivenLocalIssue()
        {
            return new Issue
            {
                ForeignIssueId = "1234",
                Title = "Chapter Three",
                Overview = "The family is on the run.",
                CoverArtUrl = "https://example.com/saga-3.jpg",
                PageCount = 32,
                IssueNumber = "3"
            };
        }

        private Issue GivenListMappedRemote()
        {
            return new Issue
            {
                ForeignIssueId = "1234",
                Title = string.Empty,
                Overview = string.Empty,
                CoverArtUrl = string.Empty,
                PageCount = 0,
                IssueNumber = "3"
            };
        }

        [Test]
        public void refresh_should_not_blank_detail_sourced_fields()
        {
            var local = GivenLocalIssue();

            local.UseMetadataFrom(GivenListMappedRemote());

            local.Overview.Should().Be("The family is on the run.");
            local.CoverArtUrl.Should().Be("https://example.com/saga-3.jpg");
            local.PageCount.Should().Be(32);
        }

        [Test]
        public void refresh_should_apply_new_values_when_present()
        {
            var local = GivenLocalIssue();
            var remote = GivenListMappedRemote();
            remote.Overview = "Updated description.";
            remote.CoverArtUrl = "https://example.com/saga-3-v2.jpg";
            remote.PageCount = 36;

            local.UseMetadataFrom(remote);

            local.Overview.Should().Be("Updated description.");
            local.CoverArtUrl.Should().Be("https://example.com/saga-3-v2.jpg");
            local.PageCount.Should().Be(36);
        }
    }
}
