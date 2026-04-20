import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Link from 'Components/Link/Link';
import styles from './SelectSeriesRow.css';

class SelectSeriesRow extends Component {

  //
  // Listeners

  onPress = () => {
    this.props.onSeriesSelect(this.props.id);
  };

  //
  // Render

  render() {
    return (
      <Link
        className={styles.series}
        component="div"
        onPress={this.onPress}
      >
        {this.props.seriesName}
      </Link>
    );
  }
}

SelectSeriesRow.propTypes = {
  id: PropTypes.number.isRequired,
  seriesName: PropTypes.string.isRequired,
  onSeriesSelect: PropTypes.func.isRequired
};

export default SelectSeriesRow;
