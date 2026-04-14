import classNames from 'classnames';
import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import CalendarEventQueueDetails from 'Calendar/Events/CalendarEventQueueDetails';
import getStatusStyle from 'Calendar/getStatusStyle';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import formatTime from 'Utilities/Date/formatTime';
import translate from 'Utilities/String/translate';
import styles from './AgendaEvent.css';

class AgendaEvent extends Component {
  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false
    };
  }

  //
  // Listeners

  onPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      id,
      series,
      title,
      titleSlug,
      releaseDate,
      monitored,
      statistics,
      grabbed,
      queueItem,
      showDate,
      timeFormat,
      longDateFormat,
      colorImpairedMode
    } = this.props;

    const startTime = moment(releaseDate);
    // const endTime = startTime.add(series.runtime, 'minutes');
    const downloading = !!(queueItem || grabbed);
    const isMonitored = series.monitored && monitored;
    const statusStyle = getStatusStyle(id, downloading, startTime, isMonitored, statistics.percentOfIssues);

    return (
      <div>
        <Link
          className={styles.event}
          component="div"
          onPress={this.onPress}
        >
          <div className={styles.date}>
            {
              showDate &&
                startTime.format(longDateFormat)
            }
          </div>

          <div
            className={classNames(
              styles.eventWrapper,
              styles[statusStyle],
              colorImpairedMode && 'colorImpaired'
            )}
          />

          <div className={styles.time}>
            {formatTime(releaseDate, timeFormat)}
          </div>

          <div className={styles.seriesName}>
            <Link to={`/series/${series.titleSlug}`}>
              {series.seriesName}
            </Link>
          </div>

          <div className={styles.issueSeparator}> - </div>

          <div className={styles.issueTitle}>
            <Link to={`/issue/${titleSlug}`}>
              {title}
            </Link>
          </div>

          {
            !!queueItem &&
              <CalendarEventQueueDetails
                {...queueItem}
              />
          }

          {
            !queueItem && grabbed &&
              <Icon
                name={icons.DOWNLOADING}
                title={translate('IssueIsDownloading')}
              />
          }
        </Link>
      </div>
    );
  }
}

AgendaEvent.propTypes = {
  id: PropTypes.number.isRequired,
  series: PropTypes.object.isRequired,
  title: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  issueType: PropTypes.string.isRequired,
  releaseDate: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  statistics: PropTypes.object.isRequired,
  grabbed: PropTypes.bool,
  queueItem: PropTypes.object,
  showDate: PropTypes.bool.isRequired,
  timeFormat: PropTypes.string.isRequired,
  colorImpairedMode: PropTypes.bool.isRequired,
  longDateFormat: PropTypes.string.isRequired
};

AgendaEvent.defaultProps = {
  statistics: {
    percentOfIssues: 0
  }
};

export default AgendaEvent;
