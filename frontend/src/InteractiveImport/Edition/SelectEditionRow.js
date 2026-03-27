import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormInputGroup from 'Components/Form/FormInputGroup';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { inputTypes } from 'Helpers/Props';
import titleCase from 'Utilities/String/titleCase';

class SelectEditionRow extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.onEditionSelect(parseInt(name), value);
  };

  //
  // Render

  render() {
    const {
      id,
      matchedEditionId,
      title,
      disambiguation,
      editions,
      columns
    } = this.props;

    const extendedTitle = disambiguation ? `${title} (${disambiguation})` : title;

    const values = _.map(editions, (issueEdition) => {

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
      if (issueEdition.asin) {
        extras.push(issueEdition.asin);
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

    return (
      <TableRow>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'issue') {
              return (
                <TableRowCell key={name}>
                  {extendedTitle}
                </TableRowCell>
              );
            }

            if (name === 'edition') {
              return (
                <TableRowCell key={name}>
                  <FormInputGroup
                    type={inputTypes.SELECT}
                    name={id.toString()}
                    values={sortedValues}
                    value={matchedEditionId}
                    onChange={this.onInputChange}
                  />
                </TableRowCell>
              );
            }

            return null;
          })
        }
      </TableRow>

    );
  }
}

SelectEditionRow.propTypes = {
  id: PropTypes.number.isRequired,
  matchedEditionId: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  editions: PropTypes.arrayOf(PropTypes.object).isRequired,
  onEditionSelect: PropTypes.func.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default SelectEditionRow;
