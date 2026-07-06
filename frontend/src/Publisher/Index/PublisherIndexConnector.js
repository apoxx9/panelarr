import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setSeriesPublisherFilter } from 'Store/Actions/seriesIndexActions';
import PublisherIndex from './PublisherIndex';

// Publishers are derived from the series list (fetched app-wide by
// PageConnector) instead of /api/v1/publisher: counting client-side is free
// and it naturally hides publishers with no series in the library.
function createMapStateToProps() {
  return createSelector(
    (state) => state.series,
    (series) => {
      const byName = new Map();

      series.items.forEach((s) => {
        const name = s.publisherName;

        if (!name) {
          return;
        }

        if (!byName.has(name)) {
          byName.set(name, { name, seriesCount: 0, monitoredCount: 0 });
        }

        const publisher = byName.get(name);

        publisher.seriesCount++;

        if (s.monitored) {
          publisher.monitoredCount++;
        }
      });

      const publishers = [...byName.values()].sort((a, b) => a.name.localeCompare(b.name));

      return {
        isFetching: series.isFetching,
        isPopulated: series.isPopulated,
        publishers
      };
    }
  );
}

function createMapDispatchToProps(dispatch) {
  return {
    onPublisherPress(publisherName) {
      dispatch(setSeriesPublisherFilter({ publisherName }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(PublisherIndex);
