import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Tab, TabList, TabPanel, Tabs } from 'react-tabs';
import DeleteSeriesModal from 'Series/Delete/DeleteSeriesModal';
import EditSeriesModalConnector from 'Series/Edit/EditSeriesModalConnector';
import SeriesHistoryTable from 'Series/History/SeriesHistoryTable';
import MonitoringOptionsModal from 'Series/MonitoringOptions/MonitoringOptionsModal';
import IssueEditorFooter from 'Issue/Editor/IssueEditorFooter';
import IssueFileEditorTable from 'IssueFile/Editor/IssueFileEditorTable';
import Alert from 'Components/Alert';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import SwipeHeaderConnector from 'Components/Swipe/SwipeHeaderConnector';
import { align, icons, kinds, sizes } from 'Helpers/Props';
import InteractiveSearchFilterMenuConnector from 'InteractiveSearch/InteractiveSearchFilterMenuConnector';
import InteractiveSearchTable from 'InteractiveSearch/InteractiveSearchTable';
import OrganizePreviewModalConnector from 'Organize/OrganizePreviewModalConnector';
// RetagPreviewModalConnector removed — not relevant for comics
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import InteractiveImportModal from '../../InteractiveImport/InteractiveImportModal';
import SeriesDetailsHeaderConnector from './SeriesDetailsHeaderConnector';
import SeriesDetailsSeasonConnector from './SeriesDetailsSeasonConnector';
import SeriesDetailsSeriesConnector from './SeriesDetailsSeriesConnector';
import styles from './SeriesDetails.css';

function getExpandedState(newState) {
  return {
    allExpanded: newState.allSelected,
    allCollapsed: newState.allUnselected,
    expandedState: newState.selectedState
  };
}

class SeriesDetails extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isEditSeriesModalOpen: false,
      isDeleteSeriesModalOpen: false,
      isInteractiveImportModalOpen: false,
      isMonitorOptionsModalOpen: false,
      isEditorActive: false,
      allExpanded: false,
      allCollapsed: false,
      expandedState: {},
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {},
      selectedTabIndex: 0
    };
  }

  //
  // Control

  setSelectedState = (items) => {
    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((item) => {
      const isItemSelected = selectedState[item.id];

      if (isItemSelected) {
        newSelectedState[item.id] = isItemSelected;
      } else {
        newSelectedState[item.id] = false;
      }
    });

    const selectedCount = getSelectedIds(newSelectedState).length;
    const newStateCount = Object.keys(newSelectedState).length;
    let isAllSelected = false;
    let isAllUnselected = false;

    if (selectedCount === 0) {
      isAllUnselected = true;
    } else if (selectedCount === newStateCount) {
      isAllSelected = true;
    }

    this.setState({ selectedState: newSelectedState, allSelected: isAllSelected, allUnselected: isAllUnselected });
  };

  getSelectedIds = () => {
    return getSelectedIds(this.state.selectedState);
  };

  //
  // Listeners

  onOrganizePress = () => {
    this.setState({ isOrganizeModalOpen: true });
  };

  onOrganizeModalClose = () => {
    this.setState({ isOrganizeModalOpen: false });
  };

  onInteractiveImportPress = () => {
    this.setState({ isInteractiveImportModalOpen: true });
  };

  onInteractiveImportModalClose = () => {
    this.setState({ isInteractiveImportModalOpen: false });
  };

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

  onMonitorOptionsPress = () => {
    this.setState({ isMonitorOptionsModalOpen: true });
  };

  onMonitorOptionsClose = () => {
    this.setState({ isMonitorOptionsModalOpen: false });
  };

  onIssueEditorTogglePress = () => {
    this.setState({ isEditorActive: !this.state.isEditorActive });
  };

  onExpandAllPress = () => {
    const {
      allExpanded,
      expandedState
    } = this.state;

    this.setState(getExpandedState(selectAll(expandedState, !allExpanded)));
  };

  onExpandPress = (issueId, isExpanded) => {
    this.setState((state) => {
      const convertedState = {
        allSelected: state.allExpanded,
        allUnselected: state.allCollapsed,
        selectedState: state.expandedState
      };

      const newState = toggleSelected(convertedState, [], issueId, isExpanded, false);

      return getExpandedState(newState);
    });
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onSelectedChange = (items, id, value, shiftKey = false) => {
    this.setState((state) => {
      return toggleSelected(state, items, id, value, shiftKey);
    });
  };

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      issueIds: this.getSelectedIds(),
      ...changes
    });
  };

  onTabSelect = (index, lastIndex) => {
    this.setState({ selectedTabIndex: index });
  };

  //
  // Render

  render() {
    const {
      id,
      seriesName,
      path,
      monitored,
      isRefreshing,
      isSearching,
      isFetching,
      isPopulated,
      issuesError,
      issueFilesError,
      hasIssues,
      hasMonitoredIssues,
      hasSeries,
      series,
      hasIssueFiles,
      previousSeries,
      nextSeries,
      onRefreshPress,
      onSearchPress,
      isSaving,
      saveError,
      isDeleting,
      deleteError,
      statistics = {}
    } = this.props;

    const {
      issueFileCount = 0,
      availableIssueCount = 0,
      totalIssueCount = 0
    } = statistics;

    const {
      isOrganizeModalOpen,
      isEditSeriesModalOpen,
      isDeleteSeriesModalOpen,
      isInteractiveImportModalOpen,
      isMonitorOptionsModalOpen,
      isEditorActive,
      allSelected,
      selectedState,
      allExpanded,
      allCollapsed,
      expandedState,
      selectedTabIndex
    } = this.state;

    let expandIcon = icons.EXPAND_INDETERMINATE;

    if (allExpanded) {
      expandIcon = icons.COLLAPSE;
    } else if (allCollapsed) {
      expandIcon = icons.EXPAND;
    }

    const selectedIssueIds = this.getSelectedIds();

    return (
      <PageContent title={seriesName}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('RefreshAndScan')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              title={translate('RefreshInformationAndScanDisk')}
              isSpinning={isRefreshing}
              onPress={onRefreshPress}
            />

            <PageToolbarButton
              label={translate('SearchMonitored')}
              iconName={icons.SEARCH}
              isDisabled={!monitored || !hasMonitoredIssues || !hasIssues}
              isSpinning={isSearching}
              title={hasMonitoredIssues ? undefined : translate('HasMonitoredIssuesNoMonitoredIssuesForThisSeries')}
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
              label={translate('ManualImport')}
              iconName={icons.INTERACTIVE}
              onPress={this.onInteractiveImportPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('IssueMonitoring')}
              iconName={icons.MONITORED}
              onPress={this.onMonitorOptionsPress}
            />

            <PageToolbarButton
              label={translate('Edit')}
              iconName={icons.EDIT}
              onPress={this.onEditSeriesPress}
            />

            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              onPress={this.onDeleteSeriesPress}
            />

            <PageToolbarSeparator />

            {
              isEditorActive ?
                <PageToolbarButton
                  label={translate('IssueList')}
                  iconName={icons.SERIES_CONTINUING}
                  onPress={this.onIssueEditorTogglePress}
                /> :
                <PageToolbarButton
                  label={translate('IssueEditor')}
                  iconName={icons.EDIT}
                  onPress={this.onIssueEditorTogglePress}
                />
            }

            {
              isEditorActive ?
                <PageToolbarButton
                  label={allSelected ? translate('UnselectAll') : translate('SelectAll')}
                  iconName={icons.CHECK_SQUARE}
                  onPress={this.onSelectAllPress}
                /> :
                null
            }

          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <PageToolbarButton
              label={allExpanded ? translate('AllExpandedCollapseAll') : translate('AllExpandedExpandAll')}
              iconName={expandIcon}
              onPress={this.onExpandAllPress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody innerClassName={styles.innerContentBody}>
          <SwipeHeaderConnector
            className={styles.header}
            nextLink={`/series/${nextSeries.titleSlug}`}
            nextComponent={(width) => <SeriesDetailsHeaderConnector seriesId={nextSeries.id} width={width} />}
            prevLink={`/series/${previousSeries.titleSlug}`}
            prevComponent={(width) => <SeriesDetailsHeaderConnector seriesId={previousSeries.id} width={width} />}
            currentComponent={(width) => <SeriesDetailsHeaderConnector seriesId={id} width={width} />}
          >
            <div className={styles.seriesNavigationButtons}>
              <IconButton
                className={styles.seriesNavigationButton}
                name={icons.ARROW_LEFT}
                size={30}
                title={translate('GoToInterp', [previousSeries.seriesName])}
                to={`/series/${previousSeries.titleSlug}`}
              />

              <IconButton
                className={styles.seriesUpButton}
                name={icons.ARROW_UP}
                size={30}
                title={translate('GoToSeriesListing')}
                to={'/'}
              />

              <IconButton
                className={styles.seriesNavigationButton}
                name={icons.ARROW_RIGHT}
                size={30}
                title={translate('GoToInterp', [nextSeries.seriesName])}
                to={`/series/${nextSeries.titleSlug}`}
              />
            </div>
          </SwipeHeaderConnector>

          <div className={styles.contentContainer}>
            {
              !isPopulated && !issuesError && !issueFilesError ?
                <LoadingIndicator /> :
                null
            }

            {
              !isFetching && issuesError ?
                <Alert kind={kinds.DANGER}>
                  {translate('LoadingIssuesFailed')}
                </Alert> :
                null
            }

            {
              !isFetching && issueFilesError ?
                <Alert kind={kinds.DANGER}>
                  {translate('LoadingIssueFilesFailed')}
                </Alert> :
                null
            }

            {
              isPopulated &&
                <Tabs selectedIndex={this.state.tabIndex} onSelect={this.onTabSelect}>
                  <TabList
                    className={styles.tabList}
                  >
                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      <Label
                        kind={availableIssueCount >= totalIssueCount && totalIssueCount > 0 ? kinds.SUCCESS : kinds.DANGER}
                        size={sizes.LARGE}
                      >
                        {availableIssueCount}/{totalIssueCount}
                      </Label>
                    </Tab>

                    <Tab
                      className={styles.tab}
                      selectedClassName={styles.selectedTab}
                    >
                      {translate('History')}
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
                      {translate('FilesTotal', [issueFileCount])}
                    </Tab>

                    {
                      selectedTabIndex === 2 &&
                        <div className={styles.filterIcon}>
                          <InteractiveSearchFilterMenuConnector
                            type="series"
                          />
                        </div>
                    }
                  </TabList>

                  <TabPanel>
                    <SeriesDetailsSeasonConnector
                      seriesId={id}
                      isExpanded={true}
                      selectedState={selectedState}
                      onExpandPress={this.onExpandPress}
                      setSelectedState={this.setSelectedState}
                      onSelectedChange={this.onSelectedChange}
                      isEditorActive={isEditorActive}
                    />
                  </TabPanel>

                  <TabPanel>
                    <SeriesHistoryTable
                      seriesId={id}
                    />
                  </TabPanel>

                  <TabPanel>
                    <InteractiveSearchTable
                      type="series"
                      seriesId={id}
                    />
                  </TabPanel>

                  <TabPanel>
                    <IssueFileEditorTable
                      seriesId={id}
                    />
                  </TabPanel>
                </Tabs>
            }
          </div>


          <OrganizePreviewModalConnector
            isOpen={isOrganizeModalOpen}
            seriesId={id}
            onModalClose={this.onOrganizeModalClose}
          />

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

          <InteractiveImportModal
            isOpen={isInteractiveImportModalOpen}
            seriesId={id}
            folder={path}
            allowSeriesChange={false}
            showFilterExistingFiles={true}
            showImportMode={false}
            onModalClose={this.onInteractiveImportModalClose}
          />

          <MonitoringOptionsModal
            isOpen={isMonitorOptionsModalOpen}
            seriesId={id}
            onModalClose={this.onMonitorOptionsClose}
          />
        </PageContentBody>

        {
          isEditorActive &&
            <IssueEditorFooter
              issueIds={selectedIssueIds}
              selectedCount={selectedIssueIds.length}
              isSaving={isSaving}
              saveError={saveError}
              isDeleting={isDeleting}
              deleteError={deleteError}
              onSaveSelected={this.onSaveSelected}
            />
        }
      </PageContent>
    );
  }
}

SeriesDetails.propTypes = {
  id: PropTypes.number.isRequired,
  seriesName: PropTypes.string.isRequired,
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
  isRefreshing: PropTypes.bool.isRequired,
  isSearching: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  issuesError: PropTypes.object,
  issueFilesError: PropTypes.object,
  hasIssues: PropTypes.bool.isRequired,
  hasMonitoredIssues: PropTypes.bool.isRequired,
  hasSeries: PropTypes.bool.isRequired,
  series: PropTypes.arrayOf(PropTypes.object).isRequired,
  hasIssueFiles: PropTypes.bool.isRequired,
  previousSeries: PropTypes.object.isRequired,
  nextSeries: PropTypes.object.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onMonitorTogglePress: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSaveSelected: PropTypes.func.isRequired
};

SeriesDetails.defaultProps = {
  statistics: {},
  tags: []
};

export default SeriesDetails;
