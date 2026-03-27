import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesNameLink from 'Series/SeriesNameLink';
import IssueFormats from 'Issue/IssueFormats';
import IssueQuality from 'Issue/IssueQuality';
import IssueTitleLink from 'Issue/IssueTitleLink';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, tooltipPositions } from 'Helpers/Props';
import formatCustomFormatScore from 'Utilities/Number/formatCustomFormatScore';
import HistoryDetailsModal from './Details/HistoryDetailsModal';
import HistoryEventTypeCell from './HistoryEventTypeCell';
import styles from './HistoryRow.css';

class HistoryRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    if (
      prevProps.isMarkingAsFailed &&
      !this.props.isMarkingAsFailed &&
      !this.props.markAsFailedError
    ) {
      this.setState({ isDetailsModalOpen: false });
    }
  }

  //
  // Listeners

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      series,
      issue,
      quality,
      customFormats,
      customFormatScore,
      qualityCutoffNotMet,
      eventType,
      sourceTitle,
      date,
      data,
      isMarkingAsFailed,
      columns,
      shortDateFormat,
      timeFormat,
      onMarkAsFailedPress
    } = this.props;

    if (!series || !issue) {
      return null;
    }

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

            if (name === 'eventType') {
              return (
                <HistoryEventTypeCell
                  key={name}
                  eventType={eventType}
                  data={data}
                />
              );
            }

            if (name === 'seriesMetadata.sortName') {
              return (
                <TableRowCell key={name}>
                  <SeriesNameLink
                    titleSlug={series.titleSlug}
                    seriesName={series.seriesName}
                  />
                </TableRowCell>
              );
            }

            if (name === 'issues.title') {
              return (
                <TableRowCell key={name}>
                  <IssueTitleLink
                    titleSlug={issue.titleSlug}
                    title={issue.title}
                    disambiguation={issue.disambiguation}
                  />
                </TableRowCell>
              );
            }

            if (name === 'quality') {
              return (
                <TableRowCell key={name}>
                  <IssueQuality
                    quality={quality}
                    isCutoffMet={qualityCutoffNotMet}
                  />
                </TableRowCell>
              );
            }

            if (name === 'customFormats') {
              return (
                <TableRowCell key={name}>
                  <IssueFormats
                    formats={customFormats}
                  />
                </TableRowCell>
              );
            }

            if (name === 'date') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  date={date}
                />
              );
            }

            if (name === 'downloadClient') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.downloadClient}
                >
                  {data.downloadClient}
                </TableRowCell>
              );
            }

            if (name === 'indexer') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexer}
                >
                  {data.indexer}
                </TableRowCell>
              );
            }

            if (name === 'customFormatScore') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.customFormatScore}
                >
                  <Tooltip
                    anchor={formatCustomFormatScore(
                      customFormatScore,
                      customFormats.length
                    )}
                    tooltip={<IssueFormats formats={customFormats} />}
                    position={tooltipPositions.BOTTOM}
                  />
                </TableRowCell>
              );
            }

            if (name === 'releaseGroup') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.releaseGroup}
                >
                  {data.releaseGroup}
                </TableRowCell>
              );
            }

            if (name === 'sourceTitle') {
              return (
                <TableRowCell
                  key={name}
                >
                  {sourceTitle}
                </TableRowCell>
              );
            }

            if (name === 'details') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.details}
                >
                  <div className={styles.actionContents}>
                    <IconButton
                      name={icons.INFO}
                      onPress={this.onDetailsPress}
                    />
                  </div>
                </TableRowCell>
              );
            }

            return null;
          })
        }

        <HistoryDetailsModal
          isOpen={this.state.isDetailsModalOpen}
          eventType={eventType}
          sourceTitle={sourceTitle}
          data={data}
          isMarkingAsFailed={isMarkingAsFailed}
          shortDateFormat={shortDateFormat}
          timeFormat={timeFormat}
          onMarkAsFailedPress={onMarkAsFailedPress}
          onModalClose={this.onDetailsModalClose}
        />
      </TableRow>
    );
  }

}

HistoryRow.propTypes = {
  issueId: PropTypes.number,
  series: PropTypes.object.isRequired,
  issue: PropTypes.object,
  quality: PropTypes.object.isRequired,
  customFormats: PropTypes.arrayOf(PropTypes.object),
  customFormatScore: PropTypes.number.isRequired,
  qualityCutoffNotMet: PropTypes.bool.isRequired,
  eventType: PropTypes.string.isRequired,
  sourceTitle: PropTypes.string.isRequired,
  date: PropTypes.string.isRequired,
  data: PropTypes.object.isRequired,
  isMarkingAsFailed: PropTypes.bool,
  markAsFailedError: PropTypes.object,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  onMarkAsFailedPress: PropTypes.func.isRequired
};

HistoryRow.defaultProps = {
  customFormats: []
};

export default HistoryRow;
