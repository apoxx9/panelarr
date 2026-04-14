import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setIssuePosterOption } from 'Store/Actions/issueIndexActions';
import IssueIndexPosterOptionsModalContent from './IssueIndexPosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.issueIndex,
    (issueIndex) => {
      return issueIndex.posterOptions;
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangePosterOption(payload) {
      dispatch(setIssuePosterOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IssueIndexPosterOptionsModalContent);
