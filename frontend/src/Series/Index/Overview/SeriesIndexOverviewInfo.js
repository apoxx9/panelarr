import PropTypes from 'prop-types';
import React from 'react';
import { icons } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import SeriesIndexOverviewInfoRow from './SeriesIndexOverviewInfoRow';
import styles from './SeriesIndexOverviewInfo.css';

const infoRowHeight = parseInt(dimensions.seriesIndexOverviewInfoRowHeight);

const rows = [
  {
    name: 'monitored',
    showProp: 'showMonitored',
    valueProp: 'monitored'

  },
  {
    name: 'qualityProfileId',
    showProp: 'showQualityProfile',
    valueProp: 'qualityProfileId'
  },
  {
    name: 'lastIssue',
    showProp: 'showLastIssue',
    valueProp: 'lastIssue'
  },
  {
    name: 'added',
    showProp: 'showAdded',
    valueProp: 'added'
  },
  {
    name: 'issueCount',
    showProp: 'showIssueCount',
    valueProp: 'issueCount'
  },
  {
    name: 'path',
    showProp: 'showPath',
    valueProp: 'path'
  },
  {
    name: 'sizeOnDisk',
    showProp: 'showSizeOnDisk',
    valueProp: 'sizeOnDisk'
  }
];

function isVisible(row, props) {
  const {
    name,
    showProp,
    valueProp
  } = row;

  if (props[valueProp] == null) {
    return false;
  }

  return props[showProp] || props.sortKey === name;
}

function getInfoRowProps(row, props) {
  const { name } = row;

  if (name === 'monitored') {
    const monitoredText = props.monitored ? 'Monitored' : 'Unmonitored';

    return {
      title: monitoredText,
      iconName: props.monitored ? icons.MONITORED : icons.UNMONITORED,
      label: monitoredText
    };
  }

  if (name === 'qualityProfileId' && !!props.qualityProfile?.name) {
    return {
      title: translate('QualityProfile'),
      iconName: icons.PROFILE,
      label: props.qualityProfile.name
    };
  }

  if (name === 'lastIssue') {
    const {
      lastIssue,
      showRelativeDates,
      shortDateFormat,
      timeFormat
    } = props;

    return {
      title: `Last Issue: ${lastIssue.title}`,
      iconName: icons.CALENDAR,
      label: getRelativeDate(
        lastIssue.releaseDate,
        shortDateFormat,
        showRelativeDates,
        {
          timeFormat,
          timeForToday: true
        }
      )
    };
  }

  if (name === 'added') {
    const {
      added,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat
    } = props;

    return {
      title: `Added: ${formatDateTime(added, longDateFormat, timeFormat)}`,
      iconName: icons.ADD,
      label: getRelativeDate(
        added,
        shortDateFormat,
        showRelativeDates,
        {
          timeFormat,
          timeForToday: true
        }
      )
    };
  }

  if (name === 'issueCount') {
    const { issueCount } = props;
    let issues = '1 issue';

    if (issueCount === 0) {
      issues = 'No issues';
    } else if (issueCount > 1) {
      issues = `${issueCount} issues`;
    }

    return {
      title: 'Issue Count',
      iconName: icons.ISSUE,
      label: issues
    };
  }

  if (name === 'path') {
    return {
      title: 'Path',
      iconName: icons.FOLDER,
      label: props.path
    };
  }

  if (name === 'sizeOnDisk') {
    return {
      title: 'Size on Disk',
      iconName: icons.DRIVE,
      label: formatBytes(props.sizeOnDisk)
    };
  }
}

function SeriesIndexOverviewInfo(props) {
  const {
    height,
    nextAiring,
    showRelativeDates,
    shortDateFormat,
    longDateFormat,
    timeFormat
  } = props;

  let shownRows = 1;

  const maxRows = Math.floor(height / (infoRowHeight + 4));

  return (
    <div className={styles.infos}>
      {
        !!nextAiring &&
          <SeriesIndexOverviewInfoRow
            title={formatDateTime(nextAiring, longDateFormat, timeFormat)}
            iconName={icons.SCHEDULED}
            label={getRelativeDate(
              nextAiring,
              shortDateFormat,
              showRelativeDates,
              {
                timeFormat,
                timeForToday: true
              }
            )}
          />
      }

      {
        rows.map((row) => {
          if (!isVisible(row, props)) {
            return null;
          }

          if (shownRows >= maxRows) {
            return null;
          }

          shownRows++;

          const infoRowProps = getInfoRowProps(row, props);

          return (
            <SeriesIndexOverviewInfoRow
              key={row.name}
              {...infoRowProps}
            />
          );
        })
      }
    </div>
  );
}

SeriesIndexOverviewInfo.propTypes = {
  height: PropTypes.number.isRequired,
  showMonitored: PropTypes.bool.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  showAdded: PropTypes.bool.isRequired,
  showIssueCount: PropTypes.bool.isRequired,
  showPath: PropTypes.bool.isRequired,
  showSizeOnDisk: PropTypes.bool.isRequired,
  monitored: PropTypes.bool.isRequired,
  nextAiring: PropTypes.string,
  qualityProfile: PropTypes.object.isRequired,
  lastIssue: PropTypes.object,
  added: PropTypes.string,
  issueCount: PropTypes.number.isRequired,
  path: PropTypes.string.isRequired,
  sizeOnDisk: PropTypes.number,
  sortKey: PropTypes.string.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default SeriesIndexOverviewInfo;
