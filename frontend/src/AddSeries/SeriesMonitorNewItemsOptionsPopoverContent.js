import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function SeriesMonitorNewItemsOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('AllIssues')}
        data={translate('DataNewAllIssues')}
      />

      <DescriptionListItem
        title={translate('NewIssues')}
        data={translate('DataNewIssues')}
      />

      <DescriptionListItem
        title={translate('None')}
        data={translate('DataNewNone')}
      />
    </DescriptionList>
  );
}

export default SeriesMonitorNewItemsOptionsPopoverContent;
