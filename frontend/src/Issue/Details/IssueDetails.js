import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-tabs';
import SeriesHistoryTable from 'Series/History/SeriesHistoryTable';
import DeleteIssueModal from 'Issue/Delete/DeleteIssueModal';
import EditIssueModalConnector from 'Issue/Edit/EditIssueModalConnector';
import IssueFileEditorTable from 'IssueFile/Editor/IssueFileEditorTable';
import IssueMetadataTable from './IssueMetadataTable';
import IconButton from 'Components/Link/IconButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import SwipeHeaderConnector from 'Components/Swipe/SwipeHeaderConnector';
import { icons } from 'Helpers/Props';
import InteractiveSearchFilterMenuConnector from 'InteractiveSearch/InteractiveSearchFilterMenuConnector';
import InteractiveSearchTable from 'InteractiveSearch/InteractiveSearchTable';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
import RetagPreviewModalConnector from 'Retag/RetagPreviewModalConnector';
import translate from 'Utilities/String/translate';
import IssueDetailsHeaderConnector from './IssueDetailsHeaderConnector';
import styles from './IssueDetails.css';

class IssueDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isRetagModalOpen: false,
      isEditIssueModalOpen: false,
      isDeleteIssueModalOpen: false,
      selectedTabIndex: 0
    };
  }

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  };

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  };

  onRetagPress = () => {
    this.setState({ isRetagModalOpen: true });
  };

  onRetagModalClose = () => {
    this.setState({ isRetagModalOpen: false });
  };

  onEditIssuePress = () => {
    this.setState({ isEditIssueModalOpen: true });
  };

  onEditIssueModalClose = () => {
    this.setState({ isEditIssueModalOpen: false });
  };

  onDeleteIssuePress = () => {
    this.setState({
      isEditIssueModalOpen: false,
      isDeleteIssueModalOpen: true
    });
  };

  onDeleteIssueModalClose = () => {
    this.setState({ isDeleteIssueModalOpen: false });
  };

  onTabSelect = (index, lastIndex) => {
    this.setState({ selectedTabIndex: index });
  };

  //
  // Render

  render() {
    const {
      id,
      title,
      isRefreshing,
      isFetching,
      isPopulated,
      issueFilesError,
      hasIssueFiles,
      series,
      previousIssue,
      nextIssue,
      isSearching,
      onRefreshPress,
      onSearchPress,
      statistics = {}
    } = this.props;

    const {
      issueFileCount = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isRetagModalOpen,
      isEditIssueModalOpen,
      isDeleteIssueModalOpen,
      selectedTabIndex
    } = this.state;

    return (
      <PageContent title={title}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('Refresh')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformation')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchIssue')}
              iconName={icons.SEARCH}
              isSpinning={isSearching}
              onPress={onSearchPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('PreviewRename')}
              iconName={icons.ORGANIZE}
              isDisabled={!hasIssueFiles}
              onPress={this.onOrganizePress}
            />

            <PageToolbarButton
              label={translate('PreviewRetag')}
              iconName={icons.RETAG}
              isDisabled={!hasIssueFiles}
              onPress={this.onRetagPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditIssuePress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteIssuePress}
            />

          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <SwipeHeaderConnector
            className={styles.header}
            nextLink={`/issue/${nextIssue.titleSlug}`}
            nextComponent={(width) => (
              <IssueDetailsHeaderConnector
                issueId={nextIssue.id}
                series={series}
                width={width}
              />
            )}
            prevLink={`/issue/${previousIssue.titleSlug}`}
            prevComponent={(width) => (
              <IssueDetailsHeaderConnector
                issueId={previousIssue.id}
                series={series}
                width={width}
              />
            )}
            currentComponent={(width) => (
              <IssueDetailsHeaderConnector
                issueId={id}
                series={series}
                width={width}
              />
            )}
          >
            <div className={styles.issueNavigationButtons}>
              <IconButton
                className={styles.issueNavigationButton}
                name={icons.ARROW_LEFT}
                size={30}
                title={translate('GoToInterp', [previousIssue.title])}
                to={`/issue/${previousIssue.titleSlug}`}
              />

              <IconButton
                className={styles.issueUpButton}
                name={icons.ARROW_UP}
                size={30}
                title={translate('GoToInterp', [series.seriesName])}
                to={`/series/${series.titleSlug}`}
              />

              <IconButton
                className={styles.issueNavigationButton}
                name={icons.ARROW_RIGHT}
                size={30}
                title={translate('GoToInterp', [nextIssue.title])}
                to={`/issue/${nextIssue.titleSlug}`}
              />
            </div>
          </SwipeHeaderConnector>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !issueFilesError &&
                <LoadingIndicator />
            }

            {
              !isFetching && issueFilesError &&
                <div>
                  {translate('LoadingIssueFilesFailed')}
                </div>
            }

            <Tabs selectedIndex={this.state.tabIndex} onSelect={this.onTabSelect}>
              <TabList
                className={styles.tabList}
              >
                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('Metadata')}
                </Tab>

                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('FilesTotal', [issueFileCount])}
                </Tab>

                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('Search')}
                </Tab>

                <Tab
                  className={styles.tab}
                  selectedClassName={styles.selectedTab}
                >
                  {translate('History')}
                </Tab>

                {
                  selectedTabIndex === 2 &&
                    <div className={styles.filterIcon}>
                      <InteractiveSearchFilterMenuConnector
                        type="issue"
                      />
                    </div>
                }

              </TabList>

              <TabPanel>
                <IssueMetadataTable
                  issueId={id}
                  isRefreshing={isRefreshing}
                  isRetagging={this.props.isRetagging}
                />
              </TabPanel>

              <TabPanel>
                <IssueFileEditorTable
                  seriesId={series.id}
                  issueId={id}
                />
              </TabPanel>

              <TabPanel>
                <InteractiveSearchTable
                  issueId={id}
                  type="issue"
                />
              </TabPanel>

              <TabPanel>
                <SeriesHistoryTable
                  seriesId={series.id}
                  issueId={id}
                />
              </TabPanel>
            </Tabs>
          </div>

          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            seriesId={series.id}
            issueId={id}
            onModalClose={this.onOrganizeModalClose}
          />

          <RetagPreviewModalConnector
            isOpen={isRetagModalOpen}
            seriesId={series.id}
            issueId={id}
            onModalClose={this.onRetagModalClose}
          />

          <EditIssueModalConnector
            isOpen={isEditIssueModalOpen}
            issueId={id}
            seriesId={series.id}
            onModalClose={this.onEditIssueModalClose}
            onDeleteSeriesPress={this.onDeleteIssuePress}
          />

          <DeleteIssueModal
            isOpen={isDeleteIssueModalOpen}
            issueId={id}
            seriesSlug={series.titleSlug}
            onModalClose={this.onDeleteIssueModalClose}
          />

        </PageContentBody>
      </PageContent>
    );
  }
}

IssueDetails.propTypes = {
  id: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string.isRequired,
  pageCount: PropTypes.number,
  overview: PropTypes.string,
  releaseDate: PropTypes.string.isRequired,
  ratings: PropTypes.object.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  links: PropTypes.arrayOf(PropTypes.object).isRequired,
  statistics: PropTypes.object.isRequired,
  monitored: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool,
  isRetagging: PropTypes.bool,
  isSearching: PropTypes.bool,
  isFetching: PropTypes.bool,
  isPopulated: PropTypes.bool,
  issueFilesError: PropTypes.object,
  hasIssueFiles: PropTypes.bool.isRequired,
  series: PropTypes.object,
  previousIssue: PropTypes.object,
  nextIssue: PropTypes.object,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func,
  onSearchPress: PropTypes.func.isRequired
};

IssueDetails.defaultProps = {
  isSaving: false
};

export default IssueDetails;
