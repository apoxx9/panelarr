import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import translate from 'Utilities/String/translate';
import styles from './PublisherIndex.css';

// Publisher browsing (docs/publisher-ui.md Tier 2B): a card grid derived
// client-side from the already-loaded series list. Publisher logo art is not
// populated by the metadata provider, so cards use an initials tile with a
// color derived from the name. Clicking a card applies a transient publisher
// filter to the series index rather than rendering its own series list.
function publisherInitials(name) {
  const words = name.split(/\s+/).filter((w) => (/[a-z0-9]/i).test(w));

  return words
    .slice(0, 2)
    .map((w) => w.replace(/[^a-z0-9]/gi, '').charAt(0).toUpperCase())
    .join('');
}

function publisherHue(name) {
  let hash = 0;

  for (let i = 0; i < name.length; i++) {
    hash = (hash * 31 + name.charCodeAt(i)) % 360;
  }

  return hash;
}

class PublisherIndex extends Component {

  onPublisherPress = (publisherName) => {
    this.props.onPublisherPress(publisherName);
    this.props.history.push('/');
  };

  render() {
    const {
      isFetching,
      isPopulated,
      publishers
    } = this.props;

    return (
      <PageContent title={translate('Publishers')}>
        <PageContentBody>
          {
            isFetching && !isPopulated &&
              <LoadingIndicator />
          }

          {
            isPopulated && !publishers.length &&
              <div className={styles.empty}>
                {translate('NoPublishersFound')}
              </div>
          }

          {
            isPopulated && !!publishers.length &&
              <div className={styles.grid}>
                {
                  publishers.map((publisher) => {
                    return (
                      <Link
                        key={publisher.name}
                        className={styles.card}
                        component="div"
                        onPress={() => this.onPublisherPress(publisher.name)}
                      >
                        <div
                          className={styles.tile}
                          style={{ backgroundColor: `hsl(${publisherHue(publisher.name)}, 45%, 35%)` }}
                        >
                          {publisherInitials(publisher.name)}
                        </div>

                        <div className={styles.name}>
                          {publisher.name}
                        </div>

                        <div className={styles.counts}>
                          {translate('PublisherSeriesCount', { count: publisher.seriesCount })}
                          {' · '}
                          {translate('PublisherMonitoredCount', { count: publisher.monitoredCount })}
                        </div>
                      </Link>
                    );
                  })
                }
              </div>
          }
        </PageContentBody>
      </PageContent>
    );
  }
}

PublisherIndex.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  publishers: PropTypes.arrayOf(PropTypes.object).isRequired,
  onPublisherPress: PropTypes.func.isRequired,
  history: PropTypes.object.isRequired
};

export default PublisherIndex;
