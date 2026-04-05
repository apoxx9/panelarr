/* eslint max-params: 0 */
import moment from 'moment';

function getStatusStyle(issueNumber, downloading, startTime, isMonitored, percentOfIssues) {
  const currentTime = moment();

  if (percentOfIssues === 100) {
    return 'downloaded';
  }

  if (percentOfIssues > 0) {
    return 'partial';
  }

  if (downloading) {
    return 'downloading';
  }

  if (!isMonitored) {
    return 'unmonitored';
  }

  if (currentTime.isAfter(startTime)) {
    return 'missing';
  }

  return 'unreleased';
}

export default getStatusStyle;
