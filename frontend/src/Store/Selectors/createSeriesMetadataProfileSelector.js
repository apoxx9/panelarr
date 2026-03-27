import { createSelector } from 'reselect';
import createSeriesSelector from './createSeriesSelector';

function createSeriesMetadataProfileSelector() {
  return createSelector(
    (state) => state.settings.metadataProfiles.items,
    createSeriesSelector(),
    (metadataProfiles, series = {}) => {
      return metadataProfiles.find((profile) => {
        return profile.id === series.metadataProfileId;
      });
    }
  );
}

export default createSeriesMetadataProfileSelector;
