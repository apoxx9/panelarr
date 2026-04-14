using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Panelarr
{
    public class PanelarrImport : ImportListBase<PanelarrSettings>
    {
        private readonly IPanelarrV1Proxy _panelarrV1Proxy;
        public override string Name => "Panelarr";

        public override ImportListType ListType => ImportListType.Program;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromMinutes(15);

        public PanelarrImport(IPanelarrV1Proxy panelarrV1Proxy,
                            IImportListStatusService importListStatusService,
                            IConfigService configService,
                            IParsingService parsingService,
                            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _panelarrV1Proxy = panelarrV1Proxy;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var seriesAndIssues = new List<ImportListItemInfo>();

            try
            {
                var remoteIssues = _panelarrV1Proxy.GetIssues(Settings);
                var remoteSeriesList = _panelarrV1Proxy.GetSeries(Settings);

                var seriesDict = remoteSeriesList.ToDictionary(x => x.Id);

                foreach (var remoteIssue in remoteIssues)
                {
                    var remoteSeries = seriesDict[remoteIssue.SeriesId];

                    if (Settings.ProfileIds.Any() && !Settings.ProfileIds.Contains(remoteSeries.QualityProfileId))
                    {
                        continue;
                    }

                    if (Settings.TagIds.Any() && !Settings.TagIds.Any(x => remoteSeries.Tags.Any(y => y == x)))
                    {
                        continue;
                    }

                    if (Settings.RootFolderPaths.Any() && !Settings.RootFolderPaths.Any(rootFolderPath => remoteSeries.RootFolderPath.ContainsIgnoreCase(rootFolderPath)))
                    {
                        continue;
                    }

                    if (!remoteIssue.Monitored || !remoteSeries.Monitored)
                    {
                        continue;
                    }

                    seriesAndIssues.Add(new ImportListItemInfo
                    {
                        ForeignIssueId = remoteIssue.ForeignIssueId,
                        Issue = remoteIssue.Title,
                        ForeignEditionId = remoteIssue.ForeignEditionId,
                        Series = remoteSeries.SeriesName,
                        ForeignSeriesId = remoteSeries.ForeignSeriesId
                    });
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _logger.Warn("List Import Sync Task Failed for List [{0}]", Definition.Name);
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(seriesAndIssues);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            // Return early if there is not an API key
            if (Settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new
                {
                    devices = new List<object>()
                };
            }

            Settings.Validate().Filter("ApiKey").ThrowOnError();

            if (action == "getProfiles")
            {
                var devices = _panelarrV1Proxy.GetProfiles(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Name, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Id,
                            Name = d.Name
                        })
                };
            }

            if (action == "getTags")
            {
                var devices = _panelarrV1Proxy.GetTags(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Label, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Id,
                            Name = d.Label
                        })
                };
            }

            if (action == "getRootFolders")
            {
                var remoteRootFolders = _panelarrV1Proxy.GetRootFolders(Settings);

                return new
                {
                    options = remoteRootFolders.OrderBy(d => d.Path, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            value = d.Path,
                            name = d.Path
                        })
                };
            }

            return new { };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_panelarrV1Proxy.Test(Settings));
        }
    }
}
