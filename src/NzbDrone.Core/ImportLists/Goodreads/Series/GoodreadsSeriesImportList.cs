using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsSeriesImportList : ImportListBase<GoodreadsSeriesImportListSettings>
    {
        private readonly IGoodreadsProxy _goodreadsProxy;

        public override string Name => "Goodreads SeriesGroup";
        public override ImportListType ListType => ImportListType.Goodreads;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(12);

        public GoodreadsSeriesImportList(IGoodreadsProxy goodreadsProxy,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _goodreadsProxy = goodreadsProxy;
        }

        public override IList<ImportListItemInfo> Fetch()
        {
            var result = new List<ImportListItemInfo>();

            try
            {
                var seriesGroup = _goodreadsProxy.GetSeriesInfo(Settings.SeriesId);

                foreach (var work in seriesGroup.Works)
                {
                    result.Add(new ImportListItemInfo
                    {
                        IssueGoodreadsId = work.Id.ToString(),
                        Issue = work.OriginalTitle,
                        EditionGoodreadsId = work.BestBook.Id.ToString(),
                        Series = work.BestBook.SeriesName,
                        SeriesGoodreadsId = work.BestBook.SeriesId.ToString()
                    });
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch
            {
                _importListStatusService.RecordFailure(Definition.Id);
            }

            return CleanupListItems(result);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _goodreadsProxy.GetSeriesInfo(Settings.SeriesId);
                return null;
            }
            catch (HttpException e)
            {
                _logger.Warn(e, "Goodreads API Error");
                if (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new ValidationFailure(nameof(Settings.SeriesId), $"SeriesGroup {Settings.SeriesId} not found");
                }

                return new ValidationFailure(nameof(Settings.SeriesId), $"Could not get series data");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to Goodreads");

                return new ValidationFailure(string.Empty, "Unable to connect to import list, check the log for more details");
            }
        }
    }
}
