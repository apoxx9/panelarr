import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function IssueTitleLink({ titleSlug, title, issueNumber, disambiguation }) {
  const link = `/issue/${titleSlug}`;
  const displayTitle = title || '';

  return (
    <Link to={link}>
      {displayTitle}{disambiguation ? ` (${disambiguation})` : ''}
    </Link>
  );
}

IssueTitleLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string,
  issueNumber: PropTypes.number,
  disambiguation: PropTypes.string
};

export default IssueTitleLink;
