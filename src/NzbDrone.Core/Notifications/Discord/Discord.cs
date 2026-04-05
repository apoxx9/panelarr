using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications.Discord.Payloads;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Discord
{
    public class Discord : NotificationBase<DiscordSettings>
    {
        private readonly IDiscordProxy _proxy;

        public Discord(IDiscordProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Discord";
        public override string Link => "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        public override void OnGrab(GrabMessage message)
        {
            var embeds = new List<Embed>
            {
                new ()
                {
                    Description = message.Message,
                    Title = message.Series.Name,
                    Text = message.Message,
                    Color = (int)DiscordColors.Warning
                }
            };
            var payload = CreatePayload($"Grabbed: {message.Message}", embeds);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Description = message.Message,
                    Title = message.Series.Name,
                    Text = message.Message,
                    Color = (int)DiscordColors.Success
                }
            };
            var payload = CreatePayload($"Imported: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnRename(Series series, List<RenamedComicFile> renamedFiles)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = series.Name,
                }
            };

            var payload = CreatePayload("Renamed", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnSeriesAdded(Series series)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = series.Name,
                    Fields = new List<DiscordField>()
                    {
                        new ()
                        {
                            Name = "Links",
                            Value = string.Join(" / ", series.Metadata.Value.Links.Select(link => $"[{link.Name}]({link.Url})"))
                        }
                    },
                }
            };
            var payload = CreatePayload("Series Added", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = deleteMessage.Series.Name,
                    Description = deleteMessage.DeletedFilesMessage
                }
            };

            var payload = CreatePayload("Series Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = $"{deleteMessage.Issue.Series.Value.Name} - ${deleteMessage.Issue.Title}",
                    Description = deleteMessage.DeletedFilesMessage
                }
            };

            var payload = CreatePayload("Issue Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = $"{deleteMessage.Issue.Series.Value.Name} - ${deleteMessage.Issue.Title} - file deleted",
                    Description = deleteMessage.ComicFile.Path
                }
            };

            var payload = CreatePayload("Issue File Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = healthCheck.Source.Name,
                    Text = healthCheck.Message,
                    Color = healthCheck.Type == HealthCheck.HealthCheckResult.Warning ? (int)DiscordColors.Warning : (int)DiscordColors.Danger
                }
            };

            var payload = CreatePayload("Health Issue", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnIssueRetag(IssueRetagMessage message)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Title = ISSUE_RETAGGED_TITLE,
                    Text = message.Message
                }
            };

            var payload = CreatePayload($"Track file tags updated: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnDownloadFailure(DownloadFailedMessage message)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Description = message.Message,
                    Title = message.SourceTitle,
                    Text = message.Message,
                    Color = (int)DiscordColors.Danger
                }
            };
            var payload = CreatePayload($"Download Failed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnImportFailure(IssueDownloadMessage message)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Description = message.Message,
                    Title = message.Issue?.Title ?? message.Message,
                    Text = message.Message,
                    Color = (int)DiscordColors.Warning
                }
            };
            var payload = CreatePayload($"Import Failed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var attachments = new List<Embed>
            {
                new ()
                {
                    Series = new DiscordSeries
                    {
                        Name = Settings.Series.IsNullOrWhiteSpace() ? Environment.MachineName : Settings.Series,
                        IconUrl = "https://raw.githubusercontent.com/Panelarr/Panelarr/develop/Logo/256.png"
                    },
                    Title = APPLICATION_UPDATE_TITLE,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Color = (int)DiscordColors.Standard,
                    Fields = new List<DiscordField>()
                    {
                        new ()
                        {
                            Name = "Previous Version",
                            Value = updateMessage.PreviousVersion.ToString()
                        },
                        new ()
                        {
                            Name = "New Version",
                            Value = updateMessage.NewVersion.ToString()
                        }
                    },
                }
            };

            var payload = CreatePayload(null, attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestMessage());

            return new ValidationResult(failures);
        }

        public ValidationFailure TestMessage()
        {
            try
            {
                var message = $"Test message from Panelarr posted at {DateTime.Now}";
                var payload = CreatePayload(message);

                _proxy.SendPayload(payload, Settings);
            }
            catch (DiscordException ex)
            {
                return new NzbDroneValidationFailure("Unable to post", ex.Message);
            }

            return null;
        }

        private DiscordPayload CreatePayload(string message, List<Embed> embeds = null)
        {
            var avatar = Settings.Avatar;

            var payload = new DiscordPayload
            {
                Username = Settings.Username,
                Content = message,
                Embeds = embeds
            };

            if (avatar.IsNotNullOrWhiteSpace())
            {
                payload.AvatarUrl = avatar;
            }

            if (Settings.Username.IsNotNullOrWhiteSpace())
            {
                payload.Username = Settings.Username;
            }

            return payload;
        }
    }
}
