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
          title={translate('AllIssues')}
          data={translate('DataAllIssues')}
        />

        <DescriptionListItem
          title={translate('FutureIssues')}
          data={translate('DataFutureIssues')}
        />

        <DescriptionListItem
          title={translate('MissingIssues')}
          data={translate('DataMissingIssues')}
        />

        <DescriptionListItem
          title={translate('ExistingIssues')}
          data={translate('DataExistingIssues')}
        />

        <DescriptionListItem
          title={translate('FirstIssue')}
          data={translate('DataFirstIssue')}
        />

        <DescriptionListItem
          title={translate('LatestIssue')}
          data={translate('DataLatestIssue')}
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
