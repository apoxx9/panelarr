using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationBaseFixture : TestBase
    {
        private class TestSetting : IProviderConfig
        {
            public NzbDroneValidationResult Validate()
            {
                return new NzbDroneValidationResult();
            }
        }

        private class TestNotificationWithOnReleaseImport : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnReleaseImport(IssueDownloadMessage message)
            {
                TestLogger.Info("OnDownload was called");
            }
        }

        private class TestNotificationWithAllEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnGrab(GrabMessage grabMessage)
            {
                TestLogger.Info("OnGrab was called");
            }

            public override void OnReleaseImport(IssueDownloadMessage message)
            {
                TestLogger.Info("OnDownload was called");
            }

            public override void OnRename(Series series, List<RenamedComicFile> renamedFiles)
            {
                TestLogger.Info("OnRename was called");
            }

            public override void OnSeriesAdded(Series series)
            {
                TestLogger.Info("OnSeriesAdded was called");
            }

            public override void OnSeriesDelete(SeriesDeleteMessage message)
            {
                TestLogger.Info("OnSeriesDelete was called");
            }

            public override void OnIssueDelete(IssueDeleteMessage message)
            {
                TestLogger.Info("OnIssueDelete was called");
            }

            public override void OnComicFileDelete(ComicFileDeleteMessage message)
            {
                TestLogger.Info("OnComicFileDelete was called");
            }

            public override void OnHealthIssue(NzbDrone.Core.HealthCheck.HealthCheck series)
            {
                TestLogger.Info("OnHealthIssue was called");
            }

            public override void OnDownloadFailure(DownloadFailedMessage message)
            {
                TestLogger.Info("OnDownloadFailure was called");
            }

            public override void OnImportFailure(IssueDownloadMessage message)
            {
                TestLogger.Info("OnImportFailure was called");
            }

            public override void OnIssueRetag(IssueRetagMessage message)
            {
                TestLogger.Info("OnIssueRetag was called");
            }

            public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
            {
                TestLogger.Info("OnApplicationUpdate was called");
            }
        }

        private class TestNotificationWithNoEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void should_support_OnUpgrade_should_link_to_OnReleaseImport()
        {
            var notification = new TestNotificationWithOnReleaseImport();

            notification.SupportsOnReleaseImport.Should().BeTrue();
            notification.SupportsOnUpgrade.Should().BeTrue();

            notification.SupportsOnGrab.Should().BeFalse();
            notification.SupportsOnRename.Should().BeFalse();
        }

        [Test]
        public void should_support_all_if_implemented()
        {
            var notification = new TestNotificationWithAllEvents();

            notification.SupportsOnGrab.Should().BeTrue();
            notification.SupportsOnReleaseImport.Should().BeTrue();
            notification.SupportsOnUpgrade.Should().BeTrue();
            notification.SupportsOnRename.Should().BeTrue();
            notification.SupportsOnHealthIssue.Should().BeTrue();
            notification.SupportsOnSeriesAdded.Should().BeTrue();
            notification.SupportsOnSeriesDelete.Should().BeTrue();
            notification.SupportsOnIssueDelete.Should().BeTrue();
            notification.SupportsOnComicFileDelete.Should().BeTrue();
            notification.SupportsOnComicFileDeleteForUpgrade.Should().BeTrue();
            notification.SupportsOnDownloadFailure.Should().BeTrue();
            notification.SupportsOnImportFailure.Should().BeTrue();
            notification.SupportsOnIssueRetag.Should().BeTrue();
            notification.SupportsOnApplicationUpdate.Should().BeTrue();
        }

        [Test]
        public void should_support_none_if_none_are_implemented()
        {
            var notification = new TestNotificationWithNoEvents();

            notification.SupportsOnGrab.Should().BeFalse();
            notification.SupportsOnReleaseImport.Should().BeFalse();
            notification.SupportsOnUpgrade.Should().BeFalse();
            notification.SupportsOnRename.Should().BeFalse();
            notification.SupportsOnSeriesAdded.Should().BeFalse();
            notification.SupportsOnSeriesDelete.Should().BeFalse();
            notification.SupportsOnIssueDelete.Should().BeFalse();
            notification.SupportsOnComicFileDelete.Should().BeFalse();
            notification.SupportsOnComicFileDeleteForUpgrade.Should().BeFalse();
            notification.SupportsOnHealthIssue.Should().BeFalse();
            notification.SupportsOnDownloadFailure.Should().BeFalse();
            notification.SupportsOnImportFailure.Should().BeFalse();
            notification.SupportsOnIssueRetag.Should().BeFalse();
            notification.SupportsOnApplicationUpdate.Should().BeFalse();
        }
    }
}
