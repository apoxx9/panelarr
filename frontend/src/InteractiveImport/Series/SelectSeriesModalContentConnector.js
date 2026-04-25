import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveInteractiveImportItem, updateInteractiveImportItem } from 'Store/Actions/interactiveImportActions';
import createAllSeriesSelector from 'Store/Selectors/createAllSeriesSelector';
import SelectSeriesModalContent from './SelectSeriesModalContent';

function createMapStateToProps() {
  return createSelector(
    createAllSeriesSelector(),
    (items) => {
      return {
        items: [...items].sort((a, b) => {
          if (a.sortName < b.sortName) {
            return -1;
          }

          if (a.sortName > b.sortName) {
            return 1;
          }

          return 0;
        })
      };
    }
  );
}

const mapDispatchToProps = {
  updateInteractiveImportItem,
  saveInteractiveImportItem
};

class SelectSeriesModalContentConnector extends Component {

  //
  // Listeners

  onSeriesSelect = (seriesId) => {
    const series = _.find(this.props.items, { id: seriesId });

    const ids = this.props.ids;

    ids.forEach((id) => {
      this.props.updateInteractiveImportItem({
        id,
        series,
        issue: undefined,
        rejections: []
      });
    });

    this.props.saveInteractiveImportItem({ ids });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <SelectSeriesModalContent
        {...this.props}
        onSeriesSelect={this.onSeriesSelect}
      />
    );
  }
}

SelectSeriesModalContentConnector.propTypes = {
  ids: PropTypes.arrayOf(PropTypes.number).isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  saveInteractiveImportItem: PropTypes.func.isRequired,
  updateInteractiveImportItem: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SelectSeriesModalContentConnector);
