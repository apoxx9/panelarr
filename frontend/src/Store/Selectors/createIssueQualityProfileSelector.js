import { createSelector } from 'reselect';
import createIssueSeriesSelector from './createIssueSeriesSelector';

function createIssueQualityProfileSelector() {
  return createSelector(
    (state) => state.settings.qualityProfiles.items,
    createIssueSeriesSelector(),
    (qualityProfiles, series) => {
      if (!series) {
        return {};
      }

      return qualityProfiles.find((profile) => profile.id === series.qualityProfileId);
    }
  );
}

export default createIssueQualityProfileSelector;
