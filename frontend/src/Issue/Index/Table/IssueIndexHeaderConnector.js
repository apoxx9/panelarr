import { connect } from 'react-redux';
import { setIssueTableOption } from 'Store/Actions/issueIndexActions';
import IssueIndexHeader from './IssueIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setIssueTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(IssueIndexHeader);
