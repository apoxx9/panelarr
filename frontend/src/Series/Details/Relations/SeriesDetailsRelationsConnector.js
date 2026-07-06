import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import SeriesDetailsRelations from './SeriesDetailsRelations';

function createMapStateToProps() {
  return createSelector(
    (state) => state.series.items,
    (allSeries) => {
      return {
        allSeries
      };
    }
  );
}

export default connect(createMapStateToProps)(SeriesDetailsRelations);
