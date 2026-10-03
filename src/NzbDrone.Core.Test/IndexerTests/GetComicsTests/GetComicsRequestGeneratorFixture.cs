using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.GetComics;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.GetComicsTests
{
    [TestFixture]
    public class GetComicsRequestGeneratorFixture : CoreTest
    {
        private GetComicsRequestGenerator Subject()
        {
            return new GetComicsRequestGenerator
            {
                Settings = new GetComicsSettings { BaseUrl = "https://getcomics.org/" }
            };
        }

        [Test]
        public void recent_requests_walk_three_pages()
        {
            // a big new-comic day pushes more than one front page of posts
            // between two RSS polls - releases scrolled past the window unseen
            var urls = Subject().GetRecentRequests()
                .GetAllTiers()
                .SelectMany(x => x)
                .Select(x => x.Url.FullUri)
                .ToList();

            urls.Should().Equal(
                "https://getcomics.org/",
                "https://getcomics.org/page/2/",
                "https://getcomics.org/page/3/");
        }
    }
}
