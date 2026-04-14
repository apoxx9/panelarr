import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createIssueFileSelector from 'Store/Selectors/createIssueFileSelector';
import MediaInfo from './MediaInfo';

function createMapStateToProps() {
  return createSelector(
    createIssueFileSelector(),
    (issueFile) => {
      if (issueFile) {
        return {
          ...issueFile.mediaInfo
        };
      }

      return {};
    }
  );
}

export default connect(createMapStateToProps)(MediaInfo);
