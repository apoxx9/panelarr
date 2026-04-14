import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesNameLink from 'Series/SeriesNameLink';
import DeleteSeriesModal from 'Series/Delete/DeleteSeriesModal';
import EditSeriesModalConnector from 'Series/Edit/EditSeriesModalConnector';
import IssueNameLink from 'Issue/IssueNameLink';
import EditIssueModalConnector from 'Issue/Edit/EditIssueModalConnector';
import HeartRating from 'Components/HeartRating';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import IssueStatusCell from './IssueStatusCell';
import styles from './IssueIndexRow.css';

class IssueIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasBannerError: false,
      isEditSeriesModalOpen: false,
      isDeleteSeriesModalOpen: false,
      isEditIssueModalOpen: false
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

  onEditIssuePress = () => {
    this.setState({ isEditIssueModalOpen: true });
  };

  onEditIssueModalClose = () => {
    this.setState({ isEditIssueModalOpen: false });
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
      seriesId,
      monitored,
      title,
      series,
      titleSlug,
      qualityProfile,
      releaseDate,
      added,
      statistics,
      genres,
      ratings,
      tags,
      showSearchAction,
      columns,
      isRefreshingIssue,
      isSearchingIssue,
      isEditorActive,
      isSelected,
      onRefreshIssuePress,
      onSearchPress,
      onSelectedChange
    } = this.props;

    const {
      issueFileCount,
      sizeOnDisk
    } = statistics;

    const {
      isEditSeriesModalOpen,
      isDeleteSeriesModalOpen,
      isEditIssueModalOpen
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
                <IssueStatusCell
                  key={name}
                  className={styles[name]}
                  monitored={monitored}
                  status={status}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'title') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={classNames(
                    styles[name]
                  )}
                >
                  <IssueNameLink
                    titleSlug={titleSlug}
                    title={title}
                  />
                </VirtualTableRowCell>
              );
            }

            if (name === 'seriesName') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={classNames(
                    styles[name]
                  )}
                >
                  <SeriesNameLink
                    titleSlug={series.titleSlug}
                    seriesName={series.seriesName}
                  />
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

            if (name === 'releaseDate') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  className={styles[name]}
                  date={releaseDate}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'added') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  className={styles[name]}
                  date={added}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (name === 'issueFileCount') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {issueFileCount}
                </VirtualTableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <VirtualTableRowCell
                  key={name}
                  className={styles[name]}
                >
                  {series.path}
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
                    name={icons.INTERACTIVE}
                    title={translate('EditSeries')}
                    onPress={this.onEditSeriesPress}
                  />

                  <IconButton
                    className={styles.action}
                    name={icons.EDIT}
                    title={translate('EditIssue')}
                    onPress={this.onEditIssuePress}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

        <EditSeriesModalConnector
          isOpen={isEditSeriesModalOpen}
          seriesId={seriesId}
          onModalClose={this.onEditSeriesModalClose}
          onDeleteSeriesPress={this.onDeleteSeriesPress}
        />

        <DeleteSeriesModal
          isOpen={isDeleteSeriesModalOpen}
          seriesId={seriesId}
          onModalClose={this.onDeleteSeriesModalClose}
        />

        <EditIssueModalConnector
          isOpen={isEditIssueModalOpen}
          seriesId={seriesId}
          issueId={id}
          onModalClose={this.onEditIssueModalClose}
        />
      </>
    );
  }
}

IssueIndexRow.propTypes = {
  id: PropTypes.number.isRequired,
  seriesId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  title: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  series: PropTypes.object.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  releaseDate: PropTypes.string,
  added: PropTypes.string,
  statistics: PropTypes.object.isRequired,
  genres: PropTypes.arrayOf(PropTypes.string).isRequired,
  ratings: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  showSearchAction: PropTypes.bool.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isRefreshingIssue: PropTypes.bool.isRequired,
  isSearchingIssue: PropTypes.bool.isRequired,
  onRefreshIssuePress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

IssueIndexRow.defaultProps = {
  statistics: {
    issueCount: 0,
    issueFileCount: 0,
    totalIssueCount: 0
  },
  genres: [],
  tags: []
};

export default IssueIndexRow;
