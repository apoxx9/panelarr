import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IssueSearchCellConnector from 'Issue/IssueSearchCellConnector';
import IssueTitleLink from 'Issue/IssueTitleLink';
import IndexerFlags from 'Issue/IndexerFlags';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';

import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import IssueStatus from './IssueStatus';
import styles from './IssueRow.css';

class IssueRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isEditIssueModalOpen: false
    };
  }

  //
  // Listeners

  onManualSearchPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onEditIssuePress = () => {
    this.setState({ isEditIssueModalOpen: true });
  };

  onEditIssueModalClose = () => {
    this.setState({ isEditIssueModalOpen: false });
  };

  onMonitorIssuePress = (monitored, options) => {
    this.props.onMonitorIssuePress(this.props.id, monitored, options);
  };

  //
  // Render

  render() {
    const {
      id,
      seriesId,
      monitored,
      releaseDate,
      title,
      seriesTitle,
      seriesName,
      position,
      issueNumber,
      pageCount,
      ratings,
      isSaving,
      seriesMonitored,
      titleSlug,
      issueFiles,
      indexerFlags,
      isEditorActive,
      isSelected,
      onSelectedChange,
      columns
    } = this.props;

    const issueFile = issueFiles[0];
    const isAvailable = Date.parse(releaseDate) < new Date();

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

            if (isEditorActive && name === 'select') {
              return (
                <TableSelectCell
                  key={name}
                  id={id}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (name === 'monitored') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.monitored}
                >
                  <MonitorToggleButton
                    monitored={monitored}
                    isDisabled={!seriesMonitored}
                    isSaving={isSaving}
                    onPress={this.onMonitorIssuePress}
                  />
                </TableRowCell>
              );
            }

            if (name === 'issueNumber') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.issueNumber}
                >
                  {issueNumber || '\u2014'}
                </TableRowCell>
              );
            }

            if (name === 'title') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  <IssueTitleLink
                    titleSlug={titleSlug}
                    title={title}
                  />
                </TableRowCell>
              );
            }

            if (name === 'series') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.title}
                >
                  {seriesTitle || ''}
                </TableRowCell>
              );
            }

            if (name === 'position') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.position}
                >
                  {position || ''}
                </TableRowCell>
              );
            }

            if (name === 'releaseDate') {
              if (!releaseDate) {
                return (
                  <TableRowCell
                    key={name}
                    className={styles.releaseDate}
                  >
                    TBA
                  </TableRowCell>
                );
              }

              return (
                <RelativeDateCellConnector
                  className={styles.releaseDate}
                  key={name}
                  date={releaseDate}
                />
              );
            }

            if (name === 'indexerFlags') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexerFlags}
                >
                  {indexerFlags ? (
                    <Popover
                      anchor={<Icon name={icons.FLAG} kind={kinds.PRIMARY} />}
                      title={translate('IndexerFlags')}
                      body={<IndexerFlags indexerFlags={indexerFlags} />}
                      position={tooltipPositions.LEFT}
                    />
                  ) : null}
                </TableRowCell>
              );
            }

            if (name === 'status') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.status}
                >
                  <IssueStatus
                    isAvailable={isAvailable}
                    monitored={monitored}
                    issueFile={issueFile}
                  />
                </TableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <IssueSearchCellConnector
                  key={name}
                  issueId={id}
                  seriesId={seriesId}
                  issueTitle={title}
                  seriesName={seriesName}
                />
              );
            }
            return null;
          })
        }
      </TableRow>
    );
  }
}

IssueRow.propTypes = {
  id: PropTypes.number.isRequired,
  seriesId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  releaseDate: PropTypes.string,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  position: PropTypes.string,
  issueNumber: PropTypes.number,
  pageCount: PropTypes.number,
  ratings: PropTypes.object.isRequired,
  indexerFlags: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  isSaving: PropTypes.bool,
  seriesMonitored: PropTypes.bool.isRequired,
  issueFiles: PropTypes.arrayOf(PropTypes.object).isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onMonitorIssuePress: PropTypes.func.isRequired
};

IssueRow.defaultProps = {
  indexerFlags: 0
};

export default IssueRow;
