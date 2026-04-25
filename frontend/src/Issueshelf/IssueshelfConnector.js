import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveIssueshelf, setIssueshelfFilter, setIssueshelfSort } from 'Store/Actions/issueshelfActions';
import createSeriesClientSideCollectionItemsSelector from 'Store/Selectors/createSeriesClientSideCollectionItemsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import Issueshelf from './Issueshelf';

function createIssueFetchStateSelector() {
  return createSelector(
    (state) => state.issues.items.length,
    (state) => state.issues.isFetching,
    (state) => state.issues.isPopulated,
    (length, isFetching, isPopulated) => {
      const issueCount = (!isFetching && isPopulated) ? length : 0;
      return {
        issueCount,
        isFetching,
        isPopulated
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createIssueFetchStateSelector(),
    createSeriesClientSideCollectionItemsSelector('issueshelf'),
    createDimensionsSelector(),
    (issues, series, dimensionsState) => {
      const isPopulated = issues.isPopulated && series.isPopulated;
      const isFetching = series.isFetching || issues.isFetching;
      return {
        ...series,
        isPopulated,
        isFetching,
        issueCount: issues.issueCount,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  setIssueshelfSort,
  setIssueshelfFilter,
  saveIssueshelf
};

class IssueshelfConnector extends Component {

  //
  // Listeners

  onSortPress = (sortKey) => {
    this.props.setIssueshelfSort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey) => {
    this.props.setIssueshelfFilter({ selectedFilterKey });
  };

  onUpdateSelectedPress = (payload) => {
    this.props.saveIssueshelf(payload);
  };

  //
  // Render

  render() {
    return (
      <Issueshelf
        {...this.props}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onUpdateSelectedPress={this.onUpdateSelectedPress}
      />
    );
  }
}

IssueshelfConnector.propTypes = {
  setIssueshelfSort: PropTypes.func.isRequired,
  setIssueshelfFilter: PropTypes.func.isRequired,
  saveIssueshelf: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueshelfConnector);
