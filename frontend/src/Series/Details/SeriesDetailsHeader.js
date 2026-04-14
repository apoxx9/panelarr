import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import SeriesPoster from 'Series/SeriesPoster';
import { getSeriesStatusDetails } from 'Series/SeriesStatus';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Marquee from 'Components/Marquee';
import Measure from 'Components/Measure';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import ProgressBar from 'Components/ProgressBar';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import QualityProfileName from 'Settings/Profiles/Quality/QualityProfileName';
import fonts from 'Styles/Variables/fonts';
import formatBytes from 'Utilities/Number/formatBytes';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import SeriesAlternateTitles from './SeriesAlternateTitles';
import SeriesDetailsLinks from './SeriesDetailsLinks';
import SeriesTagsConnector from './SeriesTagsConnector';
import styles from './SeriesDetailsHeader.css';

const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function getFanartUrl(images) {
  // Comics don't have fanart — use poster as blurred backdrop
  const fanart = images.find((x) => x.coverType === 'fanart');
  if (fanart) {
    return fanart.url;
  }

  // Fall back to poster image (with remoteUrl as backup)
  const poster = images.find((x) => x.coverType === 'poster');
  return poster?.remoteUrl || poster?.url;
}

class SeriesDetailsHeader extends Component {

  //
  // Lifecyle

  constructor(props) {
    super(props);

    this.state = {
      overviewHeight: 0,
      titleWidth: 0
    };
  }

  //
  // Listeners

  onOverviewMeasure = ({ height }) => {
    this.setState({ overviewHeight: height });
  };

  onTitleMeasure = ({ width }) => {
    this.setState({ titleWidth: width });
  };

  //
  // Render

  render() {
    const {
      id,
      width,
      seriesName,
      year,
      disambiguation,
      ratings,
      path,
      statistics,
      qualityProfileId,
      monitored,
      status,
      overview,
      links,
      images,
      alternateTitles,
      tags,
      isSaving,
      isSmallScreen,
      onMonitorTogglePress
    } = this.props;

    const {
      issueFileCount,
      sizeOnDisk,
      availableIssueCount = 0,
      totalIssueCount = 0
    } = statistics;

    const {
      overviewHeight,
      titleWidth
    } = this.state;

    const statusDetails = getSeriesStatusDetails(status);

    const fanartUrl = getFanartUrl(images);
    const marqueeWidth = titleWidth - (isSmallScreen ? 85 : 160);

    let issueFilesCountMessage = translate('IssueFilesCountMessage');

    if (issueFileCount === 1) {
      issueFilesCountMessage = '1 issue file';
    } else if (issueFileCount > 1) {
      issueFilesCountMessage = `${issueFileCount} issue files`;
    }

    return (
      <div className={styles.header} style={{ width }} >
        <div
          className={styles.backdrop}
          style={
            fanartUrl ?
              { backgroundImage: `url(${fanartUrl})` } :
              null
          }
        >
          <div className={styles.backdropOverlay} />
        </div>

        <div className={styles.headerContent}>
          <SeriesPoster
            className={styles.poster}
            images={images}
            size={250}
            lazy={false}
          />

          <div className={styles.info}>
            <Measure
              className={styles.titleRow}
              onMeasure={this.onTitleMeasure}
            >
              <div className={styles.titleContainer}>
                <div className={styles.toggleMonitoredContainer}>
                  <MonitorToggleButton
                    className={styles.monitorToggleButton}
                    monitored={monitored}
                    isSaving={isSaving}
                    size={isSmallScreen ? 30: 40}
                    onPress={onMonitorTogglePress}
                  />
                </div>

                <div className={styles.title} style={{ width: marqueeWidth }}>
                  <Marquee text={seriesName} />
                  {
                    year ?
                      <span className={styles.year}>
                        ({year})
                      </span> :
                      null
                  }
                </div>

                {
                  !!alternateTitles.length &&
                    <div className={styles.alternateTitlesIconContainer}>
                      <Popover
                        anchor={
                          <Icon
                            name={icons.ALTERNATE_TITLES}
                            size={20}
                          />
                        }
                        title={translate('AlternateTitles')}
                        body={<SeriesAlternateTitles alternateTitles={alternateTitles} />}
                        position={tooltipPositions.BOTTOM}
                      />
                    </div>
                }
              </div>
            </Measure>

            {
              disambiguation ?
                <div className={styles.publisher}>
                  {disambiguation}
                </div> :
                null
            }

            {
              ratings.value > 0 ?
                <div className={styles.details}>
                  <div>
                    <HeartRating
                      rating={ratings.value}
                      iconSize={20}
                    />
                  </div>
                </div> :
                null
            }

            <div className={styles.detailsLabels}>
              <Label
                className={styles.detailsLabel}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.FOLDER}
                  size={17}
                />

                <span className={styles.path}>
                  {path}
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                title={issueFilesCountMessage}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.DRIVE}
                  size={17}
                />

                <span className={styles.sizeOnDisk}>
                  {
                    formatBytes(sizeOnDisk || 0)
                  }
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                title={translate('QualityProfile')}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.PROFILE}
                  size={17}
                />

                <span className={styles.qualityProfileName}>
                  {
                    <QualityProfileName
                      qualityProfileId={qualityProfileId}
                    />
                  }
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                size={sizes.LARGE}
              >
                <Icon
                  name={monitored ? icons.MONITORED : icons.UNMONITORED}
                  size={17}
                />

                <span className={styles.qualityProfileName}>
                  {monitored ? 'Monitored' : 'Unmonitored'}
                </span>
              </Label>

              <Label
                className={styles.detailsLabel}
                title={statusDetails.message}
                size={sizes.LARGE}
              >
                <Icon
                  name={statusDetails.icon}
                  size={17}
                />

                <span className={styles.qualityProfileName}>
                  {statusDetails.title}
                </span>
              </Label>

              {
                !!links.length &&
                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <Icon
                          name={icons.EXTERNAL_LINK}
                          size={17}
                        />

                        <span className={styles.links}>
                          Links
                        </span>
                      </Label>
                    }
                    tooltip={
                      <SeriesDetailsLinks
                        links={links}
                      />
                    }
                    kind={kinds.INVERSE}
                    position={tooltipPositions.BOTTOM}
                  />
              }

              {
                !!tags.length &&
                  <Tooltip
                    anchor={
                      <Label
                        className={styles.detailsLabel}
                        size={sizes.LARGE}
                      >
                        <Icon
                          name={icons.TAGS}
                          size={17}
                        />

                        <span className={styles.tags}>
                          Tags
                        </span>
                      </Label>
                    }
                    tooltip={<SeriesTagsConnector seriesId={id} />}
                    kind={kinds.INVERSE}
                    position={tooltipPositions.BOTTOM}
                  />

              }
            </div>

            <Measure
              onMeasure={this.onOverviewMeasure}
              className={styles.overview}
            >
              <TextTruncate
                line={Math.floor(overviewHeight / (defaultFontSize * lineHeight))}
                text={stripHtml(overview)}
              />
            </Measure>
          </div>
        </div>
      </div>
    );
  }
}

SeriesDetailsHeader.propTypes = {
  id: PropTypes.number.isRequired,
  width: PropTypes.number.isRequired,
  seriesName: PropTypes.string.isRequired,
  year: PropTypes.number,
  disambiguation: PropTypes.string,
  ratings: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  alternateTitles: PropTypes.arrayOf(PropTypes.string).isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired
};

export default SeriesDetailsHeader;
