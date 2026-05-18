import PropTypes from 'prop-types';
import React from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoSeries.css';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.rootFolders.items,
    (rootFolders) => ({
      hasRootFolders: rootFolders && rootFolders.length > 0
    })
  );
}

function NoSeries(props) {
  const {
    totalItems,
    itemType,
    hasRootFolders
  } = props;

  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          {`All ${itemType} are hidden due to the applied filter.`}
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>
        {hasRootFolders
          ? 'No series found. Add a new series to get started.'
          : `No ${itemType} found, to get started you'll want to add a new series or issue or add an existing library location (Root Folder) and update.`
        }
      </div>

      {
        !hasRootFolders &&
          <div className={styles.buttonContainer}>
            <Button
              to="/settings/mediamanagement"
              kind={kinds.PRIMARY}
            >
              {translate('AddRootFolder')}
            </Button>
          </div>
      }

      <div className={styles.buttonContainer}>
        <Button
          to="/add/search"
          kind={kinds.PRIMARY}
        >
          {translate('AddNewSeries')}
        </Button>
      </div>
    </div>
  );
}

NoSeries.propTypes = {
  totalItems: PropTypes.number.isRequired,
  itemType: PropTypes.string.isRequired,
  hasRootFolders: PropTypes.bool.isRequired
};

NoSeries.defaultProps = {
  itemType: 'series'
};

export default connect(createMapStateToProps)(NoSeries);
