import PropTypes from 'prop-types';
import React from 'react';
import SeriesPoster from 'Series/SeriesPoster';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import styles from './SeriesSearchResult.css';

function SeriesSearchResult(props) {
  const {
    match,
    name,
    images,
    tags
  } = props;

  let tag = null;

  if (match.key === 'tags.label') {
    tag = tags[match.arrayIndex];
  }

  return (
    <div className={styles.result}>
      <SeriesPoster
        className={styles.poster}
        images={images}
        size={250}
        lazy={false}
        overflow={true}
      />

      <div className={styles.titles}>
        <div className={styles.title}>
          {name}
        </div>

        {
          tag ?
            <div className={styles.tagContainer}>
              <Label
                key={tag.id}
                kind={kinds.INFO}
              >
                {tag.label}
              </Label>
            </div> :
            null
        }
      </div>
    </div>
  );
}

SeriesSearchResult.propTypes = {
  name: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  tags: PropTypes.arrayOf(PropTypes.object).isRequired,
  match: PropTypes.object.isRequired
};

export default SeriesSearchResult;
