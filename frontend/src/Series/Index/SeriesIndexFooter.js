import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { PureComponent } from 'react';
import { ColorImpairedConsumer } from 'App/ColorImpairedContext';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './SeriesIndexFooter.css';

class SeriesIndexFooter extends PureComponent {

  //
  // Render

  render() {
    const { series } = this.props;
    const count = series.length;
    let issues = 0;
    let issueFiles = 0;
    let ended = 0;
    let continuing = 0;
    let monitored = 0;
    let totalFileSize = 0;

    series.forEach((s) => {
      const { statistics = {} } = s;

      const {
        issueCount = 0,
        issueFileCount = 0,
        sizeOnDisk = 0
      } = statistics;

      issues += issueCount;
      issueFiles += issueFileCount;

      if (s.status === 'ended') {
        ended++;
      } else {
        continuing++;
      }

      if (s.monitored) {
        monitored++;
      }

      totalFileSize += sizeOnDisk;
    });

    return (
      <ColorImpairedConsumer>
        {(enableColorImpairedMode) => {
          return (
            <div className={styles.footer}>
              <div>
                <div className={styles.legendItem}>
                  <div
                    className={classNames(
                      styles.continuing,
                      enableColorImpairedMode && 'colorImpaired'
                    )}
                  />
                  <div>
                    {translate('ContinuingAllIssuesDownloaded')}
                  </div>
                </div>

                <div className={styles.legendItem}>
                  <div
                    className={classNames(
                      styles.ended,
                      enableColorImpairedMode && 'colorImpaired'
                    )}
                  />
                  <div>
                    {translate('EndedAllIssuesDownloaded')}
                  </div>
                </div>

                <div className={styles.legendItem}>
                  <div
                    className={classNames(
                      styles.missingMonitored,
                      enableColorImpairedMode && 'colorImpaired'
                    )}
                  />
                  <div>
                    {translate('MissingIssuesSeriesMonitored')}
                  </div>
                </div>

                <div className={styles.legendItem}>
                  <div
                    className={classNames(
                      styles.missingUnmonitored,
                      enableColorImpairedMode && 'colorImpaired'
                    )}
                  />
                  <div>
                    {translate('MissingIssuesSeriesNotMonitored')}
                  </div>
                </div>
              </div>

              <div className={styles.statistics}>
                <DescriptionList>
                  <DescriptionListItem
                    title={translate('Series')}
                    data={count}
                  />

                  <DescriptionListItem
                    title={translate('Ended')}
                    data={ended}
                  />

                  <DescriptionListItem
                    title={translate('Continuing')}
                    data={continuing}
                  />
                </DescriptionList>

                <DescriptionList>
                  <DescriptionListItem
                    title={translate('Monitored')}
                    data={monitored}
                  />

                  <DescriptionListItem
                    title={translate('Unmonitored')}
                    data={count - monitored}
                  />
                </DescriptionList>

                <DescriptionList>
                  <DescriptionListItem
                    title={translate('Issues')}
                    data={issues}
                  />

                  <DescriptionListItem
                    title={translate('Files')}
                    data={issueFiles}
                  />
                </DescriptionList>

                <DescriptionList>
                  <DescriptionListItem
                    title={translate('TotalFileSize')}
                    data={formatBytes(totalFileSize)}
                  />
                </DescriptionList>
              </div>
            </div>
          );
        }}
      </ColorImpairedConsumer>
    );
  }
}

SeriesIndexFooter.propTypes = {
  series: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default SeriesIndexFooter;
