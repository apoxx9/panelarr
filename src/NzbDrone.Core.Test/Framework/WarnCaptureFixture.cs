using System.Linq;
using FluentAssertions;
using NLog;
using NUnit.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Framework
{
    /// <summary>
    /// Guards the harness that every other fixture's warn and error
    /// assertions depend on. Capture was silently dead in some fixtures -
    /// a configuration holding no logging rules satisfied the old
    /// "is the target registered?" check - so ExpectedWarns and
    /// AssertNoUnexpectedLogs passed without ever seeing a log.
    /// </summary>
    [TestFixture]
    public class WarnCaptureFixture : CoreTest
    {
        [Test]
        public void a_rule_should_route_warnings_to_the_verification_target()
        {
            var config = LogManager.Configuration;

            config.Should().NotBeNull();
            config.LoggingRules
                  .Any(r => r.Targets.Any(t => t is ExceptionVerification) && r.IsLoggingEnabledForLevel(LogLevel.Warn))
                  .Should().BeTrue("warn assertions across the suite are vacuous without this rule");
        }

        [Test]
        public void the_test_logger_should_be_named_after_itself()
        {
            // GetLogger("TestLogger") used to bind to the object overload and
            // name the logger after the argument's type, so everything logged
            // as "String"
            TestLogger.Name.Should().Be("TestLogger");
            TestLogger.IsWarnEnabled.Should().BeTrue();
        }

        [Test]
        public void a_warning_should_actually_be_captured()
        {
            TestLogger.Warn("deliberate warning from the capture guard");

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void an_error_should_actually_be_captured()
        {
            TestLogger.Error("deliberate error from the capture guard");

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
