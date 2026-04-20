import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import SeriesPoster from 'Series/SeriesPoster';
import DeleteSeriesModal from 'Series/Delete/DeleteSeriesModal';
import EditSeriesModalConnector from 'Series/Edit/EditSeriesModalConnector';
import IssueIndexProgressBar from 'Issue/Index/ProgressBar/IssueIndexProgressBar';
import CheckInput from 'Components/Form/CheckInput';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import IssueIndexOverviewInfo from './IssueIndexOverviewInfo';
import styles from './IssueIndexOverview.css';

const columnPadding = parseInt(dimensions.seriesIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.seriesIndexColumnPaddingSmallScreen);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height beased on line-height of 32 + bottom margin of 10.
// Less side-effecty than using react-measure.
const titleRowHeight = 42;

function getContentHeight(rowHeight, isSmallScreen) {
  const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

  return rowHeight - padding;
}

class IssueIndexOverview extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditSeriesModalOpen: false,
      isDeleteSeriesModalOpen: false,
      overview: ''
    };
  }

  componentDidMount() {
    const { id } = this.props;

    // Note that this component is lazy loaded by the virtualised view.
    // We want to avoid storing overviews for *all* issues which is
    // why it's not put into the redux store
    const promise = createAjaxRequest({
      url: `/issue/${id}/overview`
    }).request;

    promise.done((data) => {
      this.setState({ overview: data.overview });
    });
  }

  //
  // Listeners

  onEditSeriesPress = () => {
    this.setState({ isEditSeriesModalOpen: true });
  };

  onEditSeriesModalClose = () => {
    this.setState({ isEditSeriesModalOpen: false });
  };

  onDeleteSeriesPress = () => {
    this.setState({
      isEditSeriesModalOpen: false,
      isDeleteSeriesModalOpen: true
    });
  };

  onDeleteSeriesModalClose = () => {
    this.setState({ isDeleteSeriesModalOpen: false });
  };

  onChange = ({ value, shiftKey }) => {
    const {
      id,
      onSelectedChange
    } = this.props;

    onSelectedChange({ id, value, shiftKey });
  };

  //
  // Render

  render() {
    const {
      id,
      title,
      monitored,
      titleSlug,
      nextAiring,
      statistics,
      images,
      posterWidth,
      posterHeight,
      qualityProfile,
      overviewOptions,
      showSearchAction,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat,
      rowHeight,
      isSmallScreen,
      isRefreshingIssue,
      isSearchingIssue,
      onRefreshIssuePress,
      onSearchPress,
      isEditorActive,
      isSelected,
      ...otherProps
    } = this.props;

    const {
      issueCount,
      sizeOnDisk,
      issueFileCount,
      totalIssueCount
    } = statistics;

    const {
      overview,
      isEditSeriesModalOpen,
      isDeleteSeriesModalOpen
    } = this.state;

    const link = `/issue/${titleSlug}`;

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`,
      objectFit: 'contain'
    };

    const contentHeight = getContentHeight(rowHeight, isSmallScreen);
    const overviewHeight = contentHeight - titleRowHeight;

    return (
      <div className={styles.container}>
        <div className={styles.content}>
          <div className={styles.posterContainer}>
            {
              isEditorActive &&
                <div className={styles.editorSelect}>
                  <CheckInput
                    className={styles.checkInput}
                    name={id.toString()}
                    value={isSelected}
                    onChange={this.onChange}
                  />
                </div>
            }
            {
              status === 'ended' &&
                <div
                  className={styles.ended}
                  title={translate('Ended')}
                />
            }

            <Link
              className={styles.link}
              style={elementStyle}
              to={link}
            >
              <SeriesPoster
                className={styles.poster}
                style={elementStyle}
                images={images}
                coverType={'cover'}
                size={250}
                lazy={false}
                overflow={true}
                blurBackground={true}
              />
            </Link>

            <IssueIndexProgressBar
              monitored={monitored}
              issueCount={issueCount}
              issueFileCount={issueFileCount}
              totalIssueCount={totalIssueCount}
              posterWidth={posterWidth}
              detailedProgressBar={overviewOptions.detailedProgressBar}
            />
          </div>

          <div className={styles.info} style={{ maxHeight: contentHeight }}>
            <div className={styles.titleRow}>
              <Link
                className={styles.title}
                to={link}
              >
                {title}
              </Link>

              <div className={styles.actions}>
                <SpinnerIconButton
                  name={icons.REFRESH}
                  title={translate('RefreshIssue')}
                  isSpinning={isRefreshingIssue}
                  onPress={onRefreshIssuePress}
                />

                {
                  showSearchAction &&
                    <SpinnerIconButton
                      className={styles.action}
                      name={icons.SEARCH}
                      title={translate('SearchForMonitoredIssues')}
                      isSpinning={isSearchingIssue}
                      onPress={onSearchPress}
                    />
                }

                <IconButton
                  name={icons.EDIT}
                  title={translate('EditSeries')}
                  onPress={this.onEditSeriesPress}
                />
              </div>
            </div>

            <div className={styles.details}>

              <Link
                className={styles.overview}
                to={link}
              >
                <TextTruncate
                  line={Math.floor(overviewHeight / (defaultFontSize * lineHeight))}
                  text={stripHtml(overview)}
                />
              </Link>

              <IssueIndexOverviewInfo
                height={overviewHeight}
                monitored={monitored}
                sizeOnDisk={sizeOnDisk}
                qualityProfile={qualityProfile}
                showRelativeDates={showRelativeDates}
                shortDateFormat={shortDateFormat}
                longDateFormat={longDateFormat}
                timeFormat={timeFormat}
                {...overviewOptions}
                {...otherProps}
              />
            </div>
          </div>
        </div>

        <EditSeriesModalConnector
          isOpen={isEditSeriesModalOpen}
          seriesId={id}
          onModalClose={this.onEditSeriesModalClose}
          onDeleteSeriesPress={this.onDeleteSeriesPress}
        />

        <DeleteSeriesModal
          isOpen={isDeleteSeriesModalOpen}
          seriesId={id}
          onModalClose={this.onDeleteSeriesModalClose}
        />
      </div>
    );
  }
}

IssueIndexOverview.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  titleSlug: PropTypes.string.isRequired,
  nextAiring: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  rowHeight: PropTypes.number.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  overviewOptions: PropTypes.object.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isRefreshingIssue: PropTypes.bool.isRequired,
  isSearchingIssue: PropTypes.bool.isRequired,
  onRefreshIssuePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

IssueIndexOverview.defaultProps = {
  statistics: {
    issueCount: 0,
    issueFileCount: 0,
    totalIssueCount: 0
  }
};

export default IssueIndexOverview;
