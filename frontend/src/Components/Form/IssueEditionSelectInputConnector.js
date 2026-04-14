import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import titleCase from 'Utilities/String/titleCase';
import SelectInput from './SelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, { issueEditions }) => issueEditions,
    (issueEditions) => {
      const values = _.map(issueEditions.value, (issueEdition) => {

        let value = `${issueEdition.title}`;

        if (issueEdition.disambiguation) {
          value = `${value} (${titleCase(issueEdition.disambiguation)})`;
        }

        const extras = [];
        if (issueEdition.language) {
          extras.push(issueEdition.language);
        }
        if (issueEdition.publisher) {
          extras.push(issueEdition.publisher);
        }
        if (issueEdition.isbn13) {
          extras.push(issueEdition.isbn13);
        }
        if (issueEdition.format) {
          extras.push(issueEdition.format);
        }
        if (issueEdition.pageCount > 0) {
          extras.push(`${issueEdition.pageCount}p`);
        }

        if (extras) {
          value = `${value} [${extras.join(', ')}]`;
        }

        return {
          key: issueEdition.foreignEditionId,
          value
        };
      });

      const sortedValues = _.orderBy(values, ['value']);

      const value = _.find(issueEditions.value, { monitored: true }).foreignEditionId;

      return {
        values: sortedValues,
        value
      };
    }
  );
}

class IssueEditionSelectInputConnector extends Component {

  //
  // Listeners

  onChange = ({ name, value }) => {
    const {
      issueEditions
    } = this.props;

    const updatedEditions = _.map(issueEditions.value, (e) => ({ ...e, monitored: false }));
    _.find(updatedEditions, { foreignEditionId: value }).monitored = true;

    this.props.onChange({ name, value: updatedEditions });
  };

  render() {

    return (
      <SelectInput
        {...this.props}
        onChange={this.onChange}
      />
    );
  }
}

IssueEditionSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  onChange: PropTypes.func.isRequired,
  issueEditions: PropTypes.object
};

export default connect(createMapStateToProps)(IssueEditionSelectInputConnector);
