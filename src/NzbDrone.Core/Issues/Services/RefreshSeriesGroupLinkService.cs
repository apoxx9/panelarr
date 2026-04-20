using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshSeriesIssueLinkService
    {
        bool RefreshSeriesIssueLinkInfo(List<SeriesGroupLink> add, List<SeriesGroupLink> update, List<Tuple<SeriesGroupLink, SeriesGroupLink>> merge, List<SeriesGroupLink> delete, List<SeriesGroupLink> upToDate, List<SeriesGroupLink> remoteSeriesIssueLinks, bool forceUpdateFileTags);
    }

    public class RefreshSeriesGroupLinkService : IRefreshSeriesIssueLinkService
    {
        private readonly ISeriesIssueLinkService _seriesIssueLinkService;
        private readonly Logger _logger;

        public RefreshSeriesGroupLinkService(ISeriesIssueLinkService linkService,
                                            Logger logger)
        {
            _seriesIssueLinkService = linkService;
            _logger = logger;
        }

        public bool RefreshSeriesIssueLinkInfo(List<SeriesGroupLink> add, List<SeriesGroupLink> update, List<Tuple<SeriesGroupLink, SeriesGroupLink>> merge, List<SeriesGroupLink> delete, List<SeriesGroupLink> upToDate, List<SeriesGroupLink> remoteSeriesIssueLinks, bool forceUpdateFileTags)
        {
            var updateList = new List<SeriesGroupLink>();

            foreach (var link in update)
            {
                var remoteSeriesIssueLink = remoteSeriesIssueLinks.Single(e => e.Issue.Value.SeriesMetadataId == link.SeriesMetadataId);
                link.UseMetadataFrom(remoteSeriesIssueLink);

                // make sure title is not null
                updateList.Add(link);
            }

            _seriesIssueLinkService.DeleteMany(delete);
            _seriesIssueLinkService.UpdateMany(updateList);

            return add.Any() || delete.Any() || updateList.Any() || merge.Any();
        }
    }
}
