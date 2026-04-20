import React from 'react';
import SeriesHistoryContentConnector from 'Series/History/SeriesHistoryContentConnector';
import SeriesHistoryTableContent from 'Series/History/SeriesHistoryTableContent';
import styles from './SeriesHistoryTable.css';

function SeriesHistoryTable(props) {
  const {
    ...otherProps
  } = props;

  return (
    <div className={styles.container}>
      <SeriesHistoryContentConnector
        component={SeriesHistoryTableContent}
        {...otherProps}
      />
    </div>
  );
}

SeriesHistoryTable.propTypes = {
};

export default SeriesHistoryTable;
