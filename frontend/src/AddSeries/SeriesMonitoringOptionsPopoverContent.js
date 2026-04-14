import React from 'react';
import Alert from 'Components/Alert';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function SeriesMonitoringOptionsPopoverContent() {
  return (
    <>
      <Alert>
        {translate('MonitoringOptionsHelpText')}
      </Alert>
      <DescriptionList>
        <DescriptionListItem
          title={translate('MonitorAll')}
          data={translate('DataAllIssues')}
        />

        <DescriptionListItem
          title={translate('MonitorFuture')}
          data={translate('DataFutureIssues')}
        />

        <DescriptionListItem
          title={translate('None')}
          data={translate('DataNone')}
        />
      </DescriptionList>
    </>
  );
}

export default SeriesMonitoringOptionsPopoverContent;
