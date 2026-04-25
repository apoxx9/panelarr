import React, { Component } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';

class MoreInfo extends Component {

  //
  // Render

  render() {
    return (
      <FieldSet legend={translate('MoreInfo')}>
        <DescriptionList>
          <DescriptionListItemTitle>Home page</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://panelarr.com/">panelarr.com</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Wiki</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://wiki.servarr.com/panelarr">Wiki</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Reddit</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://www.reddit.com/r/Panelarr/">Panelarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Discord</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://panelarr.com/discord">Panelarr on Discord</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Source</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/panelarr/Panelarr/">github.com/Panelarr/Panelarr</Link>
          </DescriptionListItemDescription>

          <DescriptionListItemTitle>Feature Requests</DescriptionListItemTitle>
          <DescriptionListItemDescription>
            <Link to="https://github.com/panelarr/Panelarr/issues">github.com/Panelarr/Panelarr/issues</Link>
          </DescriptionListItemDescription>

        </DescriptionList>
      </FieldSet>
    );
  }
}

MoreInfo.propTypes = {

};

export default MoreInfo;
