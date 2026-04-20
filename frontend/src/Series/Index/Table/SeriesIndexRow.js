import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesBanner from 'Series/SeriesBanner';
import SeriesNameLink from 'Series/SeriesNameLink';
import DeleteSeriesModal from 'Series/Delete/DeleteSeriesModal';
import EditSeriesModalConnector from 'Series/Edit/EditSeriesModalConnector';
import IssueTitleLink from 'Issue/IssueTitleLink';
import HeartRating from 'Components/HeartRating';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import ProgressBar from 'Components/ProgressBar';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Series/getProgressBarKind';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import SeriesStatusCell from './SeriesStatusCell';
import hasGrowableColumns from './hasGrowableColumns';
import styles from './SeriesIndexRow.css';

class SeriesIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasBannerError: false,
      isEditSeriesModalOpen: false,
      isDeleteSeriesModalOpen: false
    };
  }

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

  onUseSceneNumberingChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  };

  onBannerLoad = () => {
    if (this.state.hasBannerError) {
      this.setState({ hasBannerError: false });
    }
  };

  onBannerLoadError = () => {
    if (!this.state.hasBannerError) {
      this.setState({ hasBannerError: true });
    }
  };

  //
  // Render

  render() {
    const {
      id,
      monitored,
      status,
      seriesName,
      seriesNameLastFirst,
      disambiguation,
      year,
      titleSlug,
      qualityProfile,
      nextIssue,
      lastIssue,
      added,
      statistics = {},
      genres,
      ratings,
      path,
      tags,
      images,
      showBanners,
      showTitle,
      showSearchAction,
      columns,
      isRefreshingSeries,
      isSearchingSeries,
      isEditorActive,
      isSelected,
      onRefreshSeriesPress,
      onSearchPress,
      onSelectedChange
    } = this.props;

    const {
      issueCount = 0,
      availableIssueCount = 0,
      issueFileCount = 0,
      totalIssueCount = 0,
      sizeOnDisk = 0
    } = statistics;

    const {
      hasBannerError,
      isEditSeriesModalOpen,
      isDeleteSeriesModalOpen
    } = this.state;

    return (
      <>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (isEditorActive && name === 'select') {
              return (
                <VirtualTableSelectCell
                  inputClassName={styles.checkInput}
                  id={id}
                  key={name}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (name === 'status') {
              return (
                <SeriesStatusCell
                  key={name}
                  className={styles[name]}
                  monitored={monitored}
                  status={status}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'publisher') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {disambiguation || ''}
                </VirtualTableRowCell>
              );
            }

            if (name === 'year') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {year || ''}
                </VirtualTableRowCell>
              );
            }

            if (name === 'sortName') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={classNames(
                    styles[name],
                    showBanners && styles.banner,
                    showBanners && !hasGrowableColumns(columns) && styles.bannerGrow
                  )}
                >
                  {
                    showBanners ?
                      <Link
                        className={styles.link}
                        to={`/series/${titleSlug}`}
                      >
                        <SeriesBanner
                          className={styles.bannerImage}
                          images={images}
                          lazy={false}
                          overflow={true}
                          onError={this.onBannerLoadError}
                          onLoad={this.onBannerLoad}
                        />

                        {
                          hasBannerError &&
                            <div className={styles.overlayTitle}>
                              {showTitle === 'firstLast' ? seriesName : seriesNameLastFirst}
                            </div>
                        }
                      </Link> :

                      <SeriesNameLink
                        titleSlug={titleSlug}
                        seriesName={showTitle === 'firstLast' ? seriesName : seriesNameLastFirst}
                      />
                  }
                </VirtualTableRowCell>
              );
            }

            if (name === 'qualityProfileId') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {qualityProfile?.name ?? ''}
                </VirtualTableRowCell>
              );
            }

            if (name === 'nextIssue') {
              if (nextIssue) {
                return (
                  <VirtualTableRowCell
                    key={name}
                    className={styles[name]}
                  >
                    <IssueTitleLink
                      title={nextIssue.title}
                      disambiguation={nextIssue.disambiguation}
                      titleSlug={nextIssue.titleSlug}
                    />
                  </VirtualTableRowCell>
                );
              }
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  None
                </VirtualTableRowCell>
              );
            }

            if (name === 'lastIssue') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {lastIssue && lastIssue.issueNumber ? `#${lastIssue.issueNumber}` : ''}
                </VirtualTableRowCell>
              );
            }

            if (name === 'added') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  className={styles[name]}
                  date={lastIssue ? lastIssue.releaseDate : null}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'issueProgress') {
              const progress = issueCount ? (availableIssueCount / issueCount) * 100 : 100;

              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <ProgressBar
                    progress={progress}
                    kind={getProgressBarKind(status, monitored, progress)}
                    showText={true}
                    text={`${availableIssueCount} / ${issueCount}`}
                    title={translate('SeriesProgressBarText', { issueCount, availableIssueCount, issueFileCount, totalIssueCount })}
                    width={125}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'seriesStatus') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {status === 'ended' ? 'Ended' : 'Continuing'}
                </VirtualTableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {path}
                </VirtualTableRowCell>
              );
            }

            if (name === 'sizeOnDisk') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {formatBytes(sizeOnDisk)}
                </VirtualTableRowCell>
              );
            }

            if (name === 'genres') {
              const joinedGenres = genres.join(', ');

              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <span title={joinedGenres}>
                    {joinedGenres}
                  </span>
                </VirtualTableRowCell>
              );
            }

            if (name === 'ratings') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <HeartRating
                    rating={ratings.value}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'tags') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <TagListConnector
                    tags={tags}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'actions') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  <SpinnerIconButton
                    name={icons.REFRESH}
                    title={translate('RefreshSeries')}
                    isSpinning={isRefreshingSeries}
                    onPress={onRefreshSeriesPress}
                  />

                  {
                    showSearchAction &&
                      <SpinnerIconButton
                        className={styles.action}
                        name={icons.SEARCH}
                        title={translate('SearchForMonitoredIssues')}
                        isSpinning={isSearchingSeries}
                        onPress={onSearchPress}
                      />
                  }

                  <IconButton
                    name={icons.EDIT}
                    title={translate('EditSeries')}
                    onPress={this.onEditSeriesPress}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

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
      </>
    );
  }
}

SeriesIndexRow.propTypes = {
  id: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  seriesNameLastFirst: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  nextIssue: PropTypes.object,
  lastIssue: PropTypes.object,
  added: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  latestIssue: PropTypes.object,
  path: PropTypes.string.isRequired,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  ratings: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  showBanners: PropTypes.bool.isRequired,
  showTitle: PropTypes.string.isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isRefreshingSeries: PropTypes.bool.isRequired,
  isSearchingSeries: PropTypes.bool.isRequired,
  onRefreshSeriesPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

SeriesIndexRow.defaultProps = {
  statistics: {
    issueCount: 0,
    issueFileCount: 0,
    totalIssueCount: 0
  },
  genres: [],
  tags: []
};

export default SeriesIndexRow;
