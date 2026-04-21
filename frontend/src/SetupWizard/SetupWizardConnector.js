import { connect } from 'react-redux';
import { withRouter } from 'react-router-dom';
import { createSelector } from 'reselect';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import SetupWizard from './SetupWizard';

function createMapStateToProps() {
  return createSelector(
    createSystemStatusSelector(),
    (systemStatus) => {
      return {
        systemStatus
      };
    }
  );
}

export default withRouter(
  connect(createMapStateToProps)(SetupWizard)
);
