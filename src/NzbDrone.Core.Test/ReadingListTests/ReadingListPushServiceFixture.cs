using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Kavita;
using NzbDrone.Core.Notifications.Komga;
using NzbDrone.Core.ReadingLists;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ReadingListTests
{
    [TestFixture]
    public class ReadingListPushServiceFixture : CoreTest<ReadingListPushService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IReadingListService>()
                  .Setup(s => s.Get(5))
                  .Returns(new ReadingList { Id = 5, Name = "Knightfall" });

            Mocker.GetMock<IReadingListService>()
                  .Setup(s => s.GetSlots(5))
                  .Returns(new System.Collections.Generic.List<ReadingListItem>
                  {
                      new ReadingListItem { IssueId = 11 },
                      new ReadingListItem { IssueId = null }
                  });

            Mocker.GetMock<IReadingListService>()
                  .Setup(s => s.ExportCbl(5, true))
                  .Returns("<ReadingList/>");

            Mocker.GetMock<IKavitaServiceProxy>()
                  .Setup(p => p.PushCbl(It.IsAny<KavitaSettings>(), It.IsAny<string>(), It.IsAny<byte[]>()))
                  .Returns(new ReaderPushResult { MatchedCount = 3 });

            Mocker.GetMock<IKomgaProxy>()
                  .Setup(p => p.PushCbl(It.IsAny<KomgaSettings>(), It.IsAny<string>(), It.IsAny<byte[]>()))
                  .Returns(new ReaderPushResult { MatchedCount = 2, Updated = true });
        }

        private void GivenConnections(params NotificationDefinition[] definitions)
        {
            Mocker.GetMock<INotificationFactory>()
                  .Setup(f => f.All())
                  .Returns(definitions.ToList());
        }

        private static NotificationDefinition Definition(string name, IProviderConfig settings)
        {
            return new NotificationDefinition { Name = name, Settings = settings };
        }

        [Test]
        public void should_only_push_to_optedin_reader_connections()
        {
            GivenConnections(
                Definition("Kavita on", new KavitaSettings { EnableReadingListPush = true }),
                Definition("Kavita off", new KavitaSettings()),
                Definition("Komga on", new KomgaSettings { EnableReadingListPush = true }),
                Definition("Komga off", new KomgaSettings()));

            var results = Subject.PushToReaders(5);

            results.Should().HaveCount(2);
            results.Should().Contain(r => r.ConnectionName == "Kavita on" && r.Reader == "Kavita" && r.Success && r.MatchedCount == 3);
            results.Should().Contain(r => r.ConnectionName == "Komga on" && r.Reader == "Komga" && r.Success && r.Updated && r.MatchedCount == 2);
        }

        [Test]
        public void should_return_empty_when_no_connection_opted_in()
        {
            GivenConnections(Definition("Kavita off", new KavitaSettings()));

            Subject.PushToReaders(5).Should().BeEmpty();
        }

        [Test]
        public void should_isolate_a_failing_connection()
        {
            GivenConnections(
                Definition("Kavita", new KavitaSettings { EnableReadingListPush = true }),
                Definition("Komga", new KomgaSettings { EnableReadingListPush = true }));

            Mocker.GetMock<IKavitaServiceProxy>()
                  .Setup(p => p.PushCbl(It.IsAny<KavitaSettings>(), It.IsAny<string>(), It.IsAny<byte[]>()))
                  .Throws(new KavitaException("boom"));

            var results = Subject.PushToReaders(5);

            results.Should().HaveCount(2);
            results.Should().Contain(r => r.Reader == "Kavita" && !r.Success && r.ErrorMessage == "boom");
            results.Should().Contain(r => r.Reader == "Komga" && r.Success);

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_export_resolved_slots_only()
        {
            GivenConnections(Definition("Kavita", new KavitaSettings { EnableReadingListPush = true }));

            Subject.PushToReaders(5);

            Mocker.GetMock<IReadingListService>()
                  .Verify(s => s.ExportCbl(5, true), Times.Once);
        }

        [Test]
        public void should_not_push_a_list_with_no_resolved_slots()
        {
            Mocker.GetMock<IReadingListService>()
                  .Setup(s => s.GetSlots(5))
                  .Returns(new System.Collections.Generic.List<ReadingListItem>
                  {
                      new ReadingListItem { IssueId = null },
                      new ReadingListItem { IssueId = null }
                  });

            GivenConnections(
                Definition("Kavita", new KavitaSettings { EnableReadingListPush = true }),
                Definition("Komga", new KomgaSettings { EnableReadingListPush = true }));

            var results = Subject.PushToReaders(5);

            results.Should().HaveCount(2);
            results.Should().OnlyContain(r => !r.Success && r.ErrorMessage.Contains("no resolved slots"));

            Mocker.GetMock<IKavitaServiceProxy>()
                  .Verify(p => p.PushCbl(It.IsAny<KavitaSettings>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
            Mocker.GetMock<IKomgaProxy>()
                  .Verify(p => p.PushCbl(It.IsAny<KomgaSettings>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public void should_sanitize_the_cbl_file_name()
        {
            Mocker.GetMock<IReadingListService>()
                  .Setup(s => s.Get(5))
                  .Returns(new ReadingList { Id = 5, Name = "Sinestro/Corps: War" });

            GivenConnections(Definition("Kavita", new KavitaSettings { EnableReadingListPush = true }));

            Subject.PushToReaders(5);

            Mocker.GetMock<IKavitaServiceProxy>()
                  .Verify(p => p.PushCbl(It.IsAny<KavitaSettings>(), It.Is<string>(f => !f.Contains("/") && f.EndsWith(".cbl")), It.IsAny<byte[]>()), Times.Once);
        }
    }
}
