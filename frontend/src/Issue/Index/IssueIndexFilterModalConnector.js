import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setIssueFilter } from 'Store/Actions/issueIndexActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.issues.items,
    (state) => state.issueIndex.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'issueIndex'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setIssueFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
