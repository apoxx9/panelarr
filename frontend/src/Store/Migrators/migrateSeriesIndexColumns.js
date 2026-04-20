import _ from 'lodash';

export default function migrateSeriesIndexColumns(persistedState) {
  const columns = _.get(persistedState, 'seriesIndex.columns');

  if (!columns) {
    return;
  }

  // Reset columns if they still have the old layout
  // (e.g. qualityProfileId visible, or missing publisher/year columns)
  const hasOldLayout = columns.some((c) => c.name === 'qualityProfileId' && c.isVisible) ||
    !columns.some((c) => c.name === 'publisher') ||
    !columns.some((c) => c.name === 'year');

  if (hasOldLayout) {
    _.unset(persistedState, 'seriesIndex.columns');
  }
}
