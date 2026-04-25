import React from 'react';
import IssueFileEditorTableContentConnector from './IssueFileEditorTableContentConnector';
import styles from './IssueFileEditorTable.css';

function IssueFileEditorTable(props) {
  const {
    ...otherProps
  } = props;

  return (
    <div className={styles.container}>
      <IssueFileEditorTableContentConnector
        {...otherProps}
      />
    </div>
  );
}

export default IssueFileEditorTable;
