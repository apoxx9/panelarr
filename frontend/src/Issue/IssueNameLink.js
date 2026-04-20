import PropTypes from 'prop-types';
import React from 'react';
import Link from 'Components/Link/Link';

function IssueNameLink({ titleSlug, title }) {
  const link = `/issue/${titleSlug}`;

  return (
    <Link to={link}>
      {title}
    </Link>
  );
}

IssueNameLink.propTypes = {
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired
};

export default IssueNameLink;
