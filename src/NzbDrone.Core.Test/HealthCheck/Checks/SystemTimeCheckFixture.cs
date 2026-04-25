using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class SystemTimeCheckFixture : CoreTest<SystemTimeCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IPanelarrCloudRequestBuilder>(new PanelarrCloudRequestBuilder());
        }

        [Test]
        public void should_always_return_ok()
        {
            Subject.Check().ShouldBeOk();
        }
    }
}
