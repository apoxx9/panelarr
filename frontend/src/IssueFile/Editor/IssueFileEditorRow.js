import PropTypes from 'prop-types';
import React from 'react';
import IssueQuality from 'Issue/IssueQuality';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import formatBytes from 'Utilities/Number/formatBytes';
import IssueFileActionsCell from './IssueFileActionsCell';
import styles from './IssueFileEditorRow.css';

function IssueFileEditorRow(props) {
  const {
    id,
    path,
    size,
    dateAdded,
    quality,
    qualityCutoffNotMet,
    isSelected,
    onSelectedChange,
    deleteIssueFile
  } = props;

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChange}
      />
      <TableRowCell
        className={styles.path}
      >
        {path}
      </TableRowCell>

      <TableRowCell
        className={styles.size}
      >
        {formatBytes(size)}
      </TableRowCell>

      <RelativeDateCellConnector
        className={styles.dateAdded}
        date={dateAdded}
      />

      <TableRowCell
        className={styles.quality}
      >
        <IssueQuality
          quality={quality}
          isCutoffNotMet={qualityCutoffNotMet}
        />
      </TableRowCell>

      <IssueFileActionsCell
        id={id}
        path={path}
        deleteIssueFile={deleteIssueFile}
      />
    </TableRow>
  );
}

IssueFileEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  path: PropTypes.string.isRequired,
  size: PropTypes.number.isRequired,
  quality: PropTypes.object.isRequired,
  qualityCutoffNotMet: PropTypes.bool.isRequired,
  dateAdded: PropTypes.string.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  deleteIssueFile: PropTypes.func.isRequired
};

export default IssueFileEditorRow;
