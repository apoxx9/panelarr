import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function SeriesNameLink({ titleSlug, seriesName, ...otherProps }) {
  const link = `/series/${titleSlug}`;

  return (
    <Link to={link} {...otherProps}>
      {seriesName}
    </Link>
  );
}

SeriesNameLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired
};

export default SeriesNameLink;
