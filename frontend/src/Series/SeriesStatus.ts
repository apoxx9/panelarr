import { SeriesStatus } from 'Series/Series';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

export function getSeriesStatusDetails(status: SeriesStatus) {
  let statusDetails = {
    icon: icons.SERIES_CONTINUING,
    title: translate('StatusEndedContinuing'),
    message: translate('ContinuingMoreIssuesAreExpected'),
  };

  if (status === 'ended') {
    statusDetails = {
      icon: icons.SERIES_ENDED,
      title: translate('StatusEndedEnded'),
      message: translate('ContinuingNoAdditionalIssuesAreExpected'),
    };
  }

  return statusDetails;
}
