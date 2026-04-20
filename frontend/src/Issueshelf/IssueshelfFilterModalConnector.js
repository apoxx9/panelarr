import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setIssueshelfFilter } from 'Store/Actions/issueshelfActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.series.items,
    (state) => state.issueshelf.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'issueshelf'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setIssueshelfFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
