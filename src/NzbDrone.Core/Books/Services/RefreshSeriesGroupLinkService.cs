using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Books
{
    public interface IRefreshSeriesBookLinkService
    {
        bool RefreshSeriesBookLinkInfo(List<SeriesGroupLink> add, List<SeriesGroupLink> update, List<Tuple<SeriesGroupLink, SeriesGroupLink>> merge, List<SeriesGroupLink> delete, List<SeriesGroupLink> upToDate, List<SeriesGroupLink> remoteSeriesBookLinks, bool forceUpdateFileTags);
    }

    public class RefreshSeriesGroupLinkService : IRefreshSeriesBookLinkService
    {
        private readonly ISeriesBookLinkService _seriesBookLinkService;
        private readonly Logger _logger;

        public RefreshSeriesGroupLinkService(ISeriesBookLinkService trackService,
                                            Logger logger)
        {
            _seriesBookLinkService = trackService;
            _logger = logger;
        }

        public bool RefreshSeriesBookLinkInfo(List<SeriesGroupLink> add, List<SeriesGroupLink> update, List<Tuple<SeriesGroupLink, SeriesGroupLink>> merge, List<SeriesGroupLink> delete, List<SeriesGroupLink> upToDate, List<SeriesGroupLink> remoteSeriesBookLinks, bool forceUpdateFileTags)
        {
            var updateList = new List<SeriesGroupLink>();

            foreach (var link in update)
            {
                var remoteSeriesBookLink = remoteSeriesBookLinks.Single(e => e.Issue.Value.Id == link.IssueId);
                link.UseMetadataFrom(remoteSeriesBookLink);

                // make sure title is not null
                updateList.Add(link);
            }

            _seriesBookLinkService.DeleteMany(delete);
            _seriesBookLinkService.UpdateMany(updateList);

            return add.Any() || delete.Any() || updateList.Any() || merge.Any();
        }
    }
}
