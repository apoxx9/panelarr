using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.IssueInfo;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource
{
    [TestFixture]
    public class CompositeMetadataProviderFixture : CoreTest<CompositeMetadataProvider>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.ComicVineApiKey)
                  .Returns(string.Empty);
        }

        // Returning null for an unconfigured provider would be indistinguishable
        // from "not found at the provider", which deletes file-less series on refresh.
        [Test]
        public void should_throw_not_return_null_for_cv_id_without_api_key()
        {
            Assert.Throws<IssueInfoException>(() => Subject.GetSeriesInfo("cv:12345"));
        }

        [Test]
        public void should_throw_for_cv_issue_id_without_api_key()
        {
            Assert.Throws<IssueInfoException>(() => Subject.GetIssueInfo("cv:12345"));
        }

        [Test]
        public void should_throw_for_cv_publisher_id_without_api_key()
        {
            Assert.Throws<IssueInfoException>(() => Subject.GetPublisher("cv:12345"));
        }
    }
}
