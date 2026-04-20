import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import SeriesNameLink from 'Series/SeriesNameLink';
import IssueCover from 'Issue/IssueCover';
import HeartRating from 'Components/HeartRating';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import Marquee from 'Components/Marquee';
import Measure from 'Components/Measure';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import fonts from 'Styles/Variables/fonts';
import formatBytes from 'Utilities/Number/formatBytes';
import stripHtml from 'Utilities/String/stripHtml';
import IssueDetailsLinks from './IssueDetailsLinks';
import styles from './IssueDetailsHeader.css';

const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function getCoverUrl(images) {
  return images.find((x) => x.coverType === 'cover')?.url;
}

class IssueDetailsHeader extends Component {

  //
  // Lifecycle

  constructor(props) {
    super(props);

    this.state = {
      overviewHeight: 0,
      titleWidth: 0,
      isOverviewExpanded: false
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

  onExpandOverview = () => {
    this.setState({ isOverviewExpanded: !this.state.isOverviewExpanded });
  };

  //
  // Render

  render() {
    const {
      width,
      titleSlug,
      title,
      issueNumber,
      seriesTitle,
      pageCount,
      overview,
      statistics = {},
      monitored,
      releaseDate,
      ratings,
      images,
      links,
      isSaving,
      shortDateFormat,
      series,
      isSmallScreen,
      onMonitorTogglePress
    } = this.props;

    const {
      overviewHeight,
      titleWidth,
      isOverviewExpanded
    } = this.state;

    const coverUrl = getCoverUrl(images);
    const marqueeWidth = titleWidth - (isSmallScreen ? 85 : 160);

    return (
      <div className={styles.header} style={{ width }}>
        <div
          className={styles.backdrop}
          style={
            coverUrl ?
              { backgroundImage: `url(${coverUrl})` } :
              null
          }
        >
          <div className={styles.backdropOverlay} />
        </div>

        <div className={styles.headerContent}>
          <IssueCover
            className={styles.cover}
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
                    size={isSmallScreen ? 30 : 40}
                    onPress={onMonitorTogglePress}
                  />
                </div>

                {
                  !!issueNumber &&
                    <div className={styles.issueNumber}>
                      #{issueNumber}
                    </div>
                }

                <div className={styles.title} style={{ width: marqueeWidth }}>
                  <Marquee text={title} />
                </div>

              </div>
            </Measure>

            <div className={styles.details}>
              <div>
                <SeriesNameLink
                  className={styles.seriesLink}
                  titleSlug={series.titleSlug}
                  seriesName={series.seriesName}
                />

                {
                  series.publisherName &&
                    <span className={styles.publisher}>
                      {series.publisherName}
                    </span>
                }

                <HeartRating
                  rating={ratings.value}
                  iconSize={20}
                />
              </div>
            </div>

            <div className={styles.detailsLabels}>
              {
                releaseDate &&
                  <Label
                    className={styles.detailsLabel}
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={icons.CALENDAR}
                      size={17}
                    />

                    <span className={styles.sizeOnDisk}>
                      {
                        moment(releaseDate).format(shortDateFormat)
                      }
                    </span>
                  </Label>
              }

              {
                !!pageCount &&
                  <Label
                    className={styles.detailsLabel}
                    size={sizes.LARGE}
                  >
                    <Icon
                      name={icons.ISSUE}
                      size={17}
                    />

                    <span className={styles.sizeOnDisk}>
                      {`${pageCount} pages`}
                    </span>
                  </Label>
              }

              <Label
                className={styles.detailsLabel}
                size={sizes.LARGE}
              >
                <Icon
                  name={icons.DRIVE}
                  size={17}
                />

                <span className={styles.sizeOnDisk}>
                  {
                    formatBytes(statistics.sizeOnDisk)
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
                  <IssueDetailsLinks
                    titleSlug={titleSlug}
                    links={links}
                  />
                }
                kind={kinds.INVERSE}
                position={tooltipPositions.BOTTOM}
              />

            </div>
            <Measure
              onMeasure={this.onOverviewMeasure}
              className={isOverviewExpanded ? styles.overviewExpanded : styles.overview}
            >
              {
                isOverviewExpanded ?
                  <span className={styles.overviewText}>
                    {stripHtml(overview)}
                    <button
                      className={styles.overviewToggle}
                      onClick={this.onExpandOverview}
                    >
                      less
                    </button>
                  </span> :
                  <TextTruncate
                    line={Math.floor(overviewHeight / (defaultFontSize * lineHeight))}
                    text={stripHtml(overview)}
                    truncateText="…"
                    textTruncateChild={
                      <button
                        className={styles.overviewToggle}
                        onClick={this.onExpandOverview}
                      >
                        more
                      </button>
                    }
                  />
              }
            </Measure>
          </div>
        </div>
      </div>
    );
  }
}

IssueDetailsHeader.propTypes = {
  id: PropTypes.number.isRequired,
  width: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  issueNumber: PropTypes.number,
  seriesTitle: PropTypes.string.isRequired,
  pageCount: PropTypes.number,
  overview: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  releaseDate: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  monitored: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired,
  series: PropTypes.object,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired
};

IssueDetailsHeader.defaultProps = {
  isSaving: false
};

export default IssueDetailsHeader;
