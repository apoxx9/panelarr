import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function IssueTitleLink({ titleSlug, title, disambiguation }) {
  const link = `/issue/${titleSlug}`;

  return (
    <Link to={link}>
      {title}{disambiguation ? ` (${disambiguation})` : ''}
    </Link>
  );
}

IssueTitleLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string
};

export default IssueTitleLink;
