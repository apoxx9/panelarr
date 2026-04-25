import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Fragment } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import translate from 'Utilities/String/translate';
import styles from './FileDetails.css';

function renderRejections(rejections) {
  return (
    <span>
      <DescriptionListItemTitle>
        Rejections
      </DescriptionListItemTitle>
      {
        _.map(rejections, (item, key) => {
          return (
            <DescriptionListItemDescription key={key}>
              {item.reason}
            </DescriptionListItemDescription>
          );
        })
      }
    </span>
  );
}

function FileDetails(props) {

  const {
    filename,
    fileTags,
    rejections
  } = props;

  return (
    <Fragment>
      <div className={styles.fileTags}>
        <DescriptionList>
          {
            filename &&
              <DescriptionListItem
                title={translate('Filename')}
                data={filename}
                descriptionClassName={styles.filename}
              />
          }
          {
            fileTags.quality !== undefined &&
              <DescriptionListItem
                title={translate('Quality')}
                data={fileTags.quality}
              />
          }
          {
            fileTags.size !== undefined &&
              <DescriptionListItem
                title={translate('Size')}
                data={fileTags.size}
              />
          }
          {
            fileTags.dateAdded !== undefined &&
              <DescriptionListItem
                title={translate('DateAdded')}
                data={fileTags.dateAdded}
              />
          }
          {
            !!rejections && rejections.length > 0 &&
              renderRejections(rejections)
          }
        </DescriptionList>
      </div>
    </Fragment>
  );
}

FileDetails.propTypes = {
  filename: PropTypes.string,
  fileTags: PropTypes.object.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object)
};

export default FileDetails;
