using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLog;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Kavita;
using NzbDrone.Core.Notifications.Komga;

namespace NzbDrone.Core.ReadingLists
{
    public interface IReadingListPushService
    {
        List<ReadingListPushConnectionResult> PushToReaders(int readingListId);
    }

    public class ReadingListPushConnectionResult
    {
        public string ConnectionName { get; set; }
        public string Reader { get; set; }
        public bool Success { get; set; }
        public bool Updated { get; set; }
        public int MatchedCount { get; set; }
        public List<string> Unmatched { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }

    public class ReadingListPushService : IReadingListPushService
    {
        private readonly IReadingListService _readingListService;
        private readonly INotificationFactory _notificationFactory;
        private readonly IKavitaServiceProxy _kavitaProxy;
        private readonly IKomgaProxy _komgaProxy;
        private readonly Logger _logger;

        public ReadingListPushService(IReadingListService readingListService,
                                      INotificationFactory notificationFactory,
                                      IKavitaServiceProxy kavitaProxy,
                                      IKomgaProxy komgaProxy,
                                      Logger logger)
        {
            _readingListService = readingListService;
            _notificationFactory = notificationFactory;
            _kavitaProxy = kavitaProxy;
            _komgaProxy = komgaProxy;
            _logger = logger;
        }

        public List<ReadingListPushConnectionResult> PushToReaders(int readingListId)
        {
            var list = _readingListService.Get(readingListId);
            var cblData = Encoding.UTF8.GetBytes(_readingListService.ExportCbl(readingListId));
            var fileName = SanitizeFileName(list.Name) + ".cbl";

            var results = new List<ReadingListPushConnectionResult>();

            // Push is an explicit opt-in per connection (Send Reading Lists),
            // independent of the notification event toggles.
            foreach (var definition in _notificationFactory.All())
            {
                switch (definition.Settings)
                {
                    case KavitaSettings kavita when kavita.EnableReadingListPush:
                        results.Add(Push(definition.Name, "Kavita", () => _kavitaProxy.PushCbl(kavita, fileName, cblData)));
                        break;
                    case KomgaSettings komga when komga.EnableReadingListPush:
                        results.Add(Push(definition.Name, "Komga", () => _komgaProxy.PushCbl(komga, list.Name, cblData)));
                        break;
                }
            }

            return results;
        }

        private ReadingListPushConnectionResult Push(string connectionName, string reader, Func<ReaderPushResult> push)
        {
            try
            {
                var result = push();

                _logger.Info("Pushed reading list to {0} ({1}): {2} matched, {3} unmatched", connectionName, reader, result.MatchedCount, result.Unmatched.Count);

                return new ReadingListPushConnectionResult
                {
                    ConnectionName = connectionName,
                    Reader = reader,
                    Success = true,
                    Updated = result.Updated,
                    MatchedCount = result.MatchedCount,
                    Unmatched = result.Unmatched
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to push reading list to {0} ({1})", connectionName, reader);

                return new ReadingListPushConnectionResult
                {
                    ConnectionName = connectionName,
                    Reader = reader,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);

            foreach (var c in name)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            var sanitized = builder.ToString().Trim();

            return sanitized.Length > 0 ? sanitized : "reading-list";
        }
    }
}
