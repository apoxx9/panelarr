import { connect } from 'react-redux';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import IssueInteractiveSearchModal from './IssueInteractiveSearchModal';

function createMapDispatchToProps(dispatch, props) {
  return {
    onModalClose() {
      dispatch(cancelFetchReleases());
      dispatch(clearReleases());
      props.onModalClose();
    }
  };
}

export default connect(null, createMapDispatchToProps)(IssueInteractiveSearchModal);
