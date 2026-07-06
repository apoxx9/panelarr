using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.LibraryImport;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.LibraryImport
{
    [TestFixture]
    public class StagingImportReportServiceFixture : CoreTest<StagingImportReportService>
    {
        private static StagingImportReport GivenReport(int commandId)
        {
            return new StagingImportReport { CommandId = commandId };
        }

        [Test]
        public void should_find_a_stored_report_by_command_id()
        {
            var report = GivenReport(7);

            Subject.Store(report);

            Subject.Find(7).Should().BeSameAs(report);
        }

        [Test]
        public void should_return_null_for_an_unknown_command_id()
        {
            Subject.Store(GivenReport(7));

            Subject.Find(8).Should().BeNull();
        }

        [Test]
        public void should_replace_a_report_stored_for_the_same_command_id()
        {
            var replacement = GivenReport(7);

            Subject.Store(GivenReport(7));
            Subject.Store(replacement);

            Subject.Find(7).Should().BeSameAs(replacement);
        }

        [Test]
        public void should_keep_only_the_most_recent_reports()
        {
            foreach (var commandId in Enumerable.Range(1, 6))
            {
                Subject.Store(GivenReport(commandId));
            }

            Subject.Find(1).Should().BeNull();
            Subject.Find(2).Should().NotBeNull();
            Subject.Find(6).Should().NotBeNull();
        }
    }
}
