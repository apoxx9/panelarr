using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class MetadataProviderCheckFixture : CoreTest<MetadataProviderCheck>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns(string.Empty);

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns(string.Empty);

            Mocker.GetMock<ILocalizationService>()
                .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                .Returns("No metadata providers configured");
        }

        [Test]
        public void should_return_warning_when_no_providers_configured()
        {
            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_ok_when_comicvine_configured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns("some-api-key");

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_metron_configured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns("testuser");

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_ok_when_both_configured()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns("some-api-key");

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns("testuser");

            Subject.Check().ShouldBeOk();
        }
    }
}
