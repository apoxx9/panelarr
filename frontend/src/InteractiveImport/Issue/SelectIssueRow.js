import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './SelectIssueRow.css';

function getIssueCountKind(monitored, issueFileCount, issueCount) {
  if (issueFileCount === issueCount && issueCount > 0) {
    return kinds.SUCCESS;
  }

  if (!monitored) {
    return kinds.WARNING;
  }

  return kinds.DANGER;
}

class SelectIssueRow extends Component {

  //
  // Listeners

  onPress = () => {
    this.props.onIssueSelect(this.props.id);
  };

  //
  // Render

  render() {
    const {
      title,
      releaseDate,
      statistics,
      monitored,
      columns
    } = this.props;

    const {
      issueCount,
      issueFileCount,
      totalIssueCount
    } = statistics;

    return (
      <TableRow
        onClick={this.onPress}
        className={styles.issueRow}
      >
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'title') {
              return (
                <TableRowCell key={name}>
                  {title}
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  date={releaseDate}
                />
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                >
                  <Label
                    title={translate('TotalIssueCountIssuesTotalIssueFileCountIssuesWithFilesInterp', [totalIssueCount, issueFileCount])}
                    kind={getIssueCountKind(monitored, issueFileCount, issueCount)}
                    size={sizes.MEDIUM}
                  >
                    {
                      <span>{issueFileCount} / {issueCount}</span>
                    }
                  </Label>
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

SelectIssueRow.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  releaseDate: PropTypes.string.isRequired,
  onIssueSelect: PropTypes.func.isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired
};

SelectIssueRow.defaultProps = {
  statistics: {
    issueCount: 0,
    issueFileCount: 0
  }
};

export default SelectIssueRow;
