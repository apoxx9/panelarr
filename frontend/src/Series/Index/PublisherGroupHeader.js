import PropTypes from 'prop-types';
import React from 'react';
import translate from 'Utilities/String/translate';
import styles from './PublisherGroupHeader.css';

// Section header rendered between publisher groups when the series index
// is grouped by publisher. Shared by the table, poster and overview views.
function PublisherGroupHeader({ title, count }) {
  return (
    <div className={styles.groupHeader}>
      <span className={styles.title}>
        {title || translate('NoPublisher')}
      </span>

      <span className={styles.count}>
        ({count})
      </span>
    </div>
  );
}

PublisherGroupHeader.propTypes = {
  title: PropTypes.string,
  count: PropTypes.number.isRequired
};

export default PublisherGroupHeader;
