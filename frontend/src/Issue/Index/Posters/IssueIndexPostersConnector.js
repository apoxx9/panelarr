import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import IssueIndexPosters from './IssueIndexPosters';

function createMapStateToProps() {
  return createSelector(
    (state) => state.issueIndex.posterOptions,
    createUISettingsSelector(),
    createDimensionsSelector(),
    (posterOptions, uiSettings, dimensions) => {
      return {
        posterOptions,
        showRelativeDates: uiSettings.showRelativeDates,
        shortDateFormat: uiSettings.shortDateFormat,
        timeFormat: uiSettings.timeFormat,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

export default connect(createMapStateToProps)(IssueIndexPosters);
