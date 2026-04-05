import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import RetagSeriesModal from 'Series/Editor/AudioTags/RetagSeriesModal';
import SeriesEditorFooter from 'Series/Editor/SeriesEditorFooter';
import OrganizeSeriesModal from 'Series/Editor/Organize/OrganizeSeriesModal';
import NoSeries from 'Series/NoSeries';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, sortDirections } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import SeriesIndexFooterConnector from './SeriesIndexFooterConnector';
import SeriesIndexFilterMenu from './Menus/SeriesIndexFilterMenu';
import SeriesIndexSortMenu from './Menus/SeriesIndexSortMenu';
import SeriesIndexViewMenu from './Menus/SeriesIndexViewMenu';
import SeriesIndexOverviewsConnector from './Overview/SeriesIndexOverviewsConnector';
import SeriesIndexOverviewOptionsModal from './Overview/Options/SeriesIndexOverviewOptionsModal';
import SeriesIndexPostersConnector from './Posters/SeriesIndexPostersConnector';
import SeriesIndexPosterOptionsModal from './Posters/Options/SeriesIndexPosterOptionsModal';
import SeriesIndexTableConnector from './Table/SeriesIndexTableConnector';
import SeriesIndexTableOptionsConnector from './Table/SeriesIndexTableOptionsConnector';
import styles from './SeriesIndex.css';

function getViewComponent(view) {
  if (view === 'posters') {
    return SeriesIndexPostersConnector;
  }

  if (view === 'overview') {
    return SeriesIndexOverviewsConnector;
  }

  return SeriesIndexTableConnector;
}

class SeriesIndex extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scroller: null,
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      isPosterOptionsModalOpen: false,
      isOverviewOptionsModalOpen: false,
      isEditorActive: false,
      isOrganizingSeriesModalOpen: false,
      isRetaggingSeriesModalOpen: false,
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
      selectedState: {}
    };
  }

  componentDidMount() {
    this.setJumpBarItems();
    this.setSelectedState();
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      this.setJumpBarItems();
      this.setSelectedState();
    }

    if (this.state.jumpToCharacter != null) {
      this.setState({ jumpToCharacter: null });
    }
  }

  //
  // Control

  setScrollerRef = (ref) => {
    this.setState({ scroller: ref });
  };

  getSelectedIds = () => {
    if (this.state.allUnselected) {
      return [];
    }
    return getSelectedIds(this.state.selectedState);
  };

  setSelectedState() {
    const {
      items
    } = this.props;

    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((series) => {
      const isItemSelected = selectedState[series.id];

      if (isItemSelected) {
        newSelectedState[series.id] = isItemSelected;
      } else {
        newSelectedState[series.id] = false;
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
  }

  setJumpBarItems() {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    // Reset if not sorting by sortName
    if (sortKey !== 'sortName' && sortKey !== 'sortNameLastFirst') {
      this.setState({ jumpBarItems: { order: [] } });
      return;
    }

    const characters = _.reduce(items, (acc, item) => {
      const sortValue = item[sortKey] || '';
      let char = sortValue.charAt(0);

      if (!isNaN(char)) {
        char = '#';
      }

      if (char in acc) {
        acc[char] = acc[char] + 1;
      } else {
        acc[char] = 1;
      }

      return acc;
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === sortDirections.DESCENDING) {
      order.reverse();
    }

    const jumpBarItems = {
      characters,
      order
    };

    this.setState({ jumpBarItems });
  }

  //
  // Listeners

  onPosterOptionsPress = () => {
    this.setState({ isPosterOptionsModalOpen: true });
  };

  onPosterOptionsModalClose = () => {
    this.setState({ isPosterOptionsModalOpen: false });
  };

  onOverviewOptionsPress = () => {
    this.setState({ isOverviewOptionsModalOpen: true });
  };

  onOverviewOptionsModalClose = () => {
    this.setState({ isOverviewOptionsModalOpen: false });
  };

  onEditorTogglePress = () => {
    if (this.state.isEditorActive) {
      this.setState({ isEditorActive: false });
    } else {
      const newState = selectAll(this.state.selectedState, false);
      newState.isEditorActive = true;
      this.setState(newState);
    }
  };

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  };

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      seriesIds: this.getSelectedIds(),
      ...changes
    });
  };

  onOrganizeSeriesPress = () => {
    this.setState({ isOrganizingSeriesModalOpen: true });
  };

  onOrganizeSeriesModalClose = (organized) => {
    this.setState({ isOrganizingSeriesModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  };

  onRetagSeriesPress = () => {
    this.setState({ isRetaggingSeriesModalOpen: true });
  };

  onRetagSeriesModalClose = (organized) => {
    this.setState({ isRetaggingSeriesModalOpen: false });

    if (organized === true) {
      this.onSelectAllChange({ value: false });
    }
  };

  onRefreshSeriesPress = () => {
    const selectedIds = this.getSelectedIds();
    const refreshIds = this.state.isEditorActive && selectedIds.length > 0 ? selectedIds : [];

    this.props.onRefreshSeriesPress(refreshIds);
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      view,
      isRefreshingSeries,
      isRssSyncExecuting,
      isOrganizingSeries,
      isRetaggingSeries,
      isSaving,
      saveError,
      isDeleting,
      deleteError,
      onScroll,
      onSortSelect,
      onFilterSelect,
      onViewSelect,
      onRssSyncPress,
      ...otherProps
    } = this.props;

    const {
      scroller,
      jumpBarItems,
      jumpToCharacter,
      isPosterOptionsModalOpen,
      isOverviewOptionsModalOpen,
      isEditorActive,
      selectedState,
      allSelected,
      allUnselected
    } = this.state;

    const selectedSeriesIds = this.getSelectedIds();

    const ViewComponent = getViewComponent(view);
    const isLoaded = !!(!error && isPopulated && items.length && scroller);
    const hasNoSeries = !totalItems;

    const refreshLabel = isEditorActive && selectedSeriesIds.length > 0 ? translate('UpdateSelected') : translate('UpdateAll');

    return (
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={refreshLabel}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              isSpinning={isRefreshingSeries}
              onPress={this.onRefreshSeriesPress}
            />

            <PageToolbarButton
              label={translate('RSSSync')}
              iconName={icons.RSS}
              isSpinning={isRssSyncExecuting}
              isDisabled={hasNoSeries}
              onPress={onRssSyncPress}
            />

            <PageToolbarSeparator />

            {
              isEditorActive ?
                <PageToolbarButton
                  label={translate('SeriesIndex')}
                  iconName={icons.SERIES_CONTINUING}
                  isDisabled={hasNoSeries}
                  onPress={this.onEditorTogglePress}
                /> :
                <PageToolbarButton
                  label={translate('SeriesEditor')}
                  iconName={icons.EDIT}
                  isDisabled={hasNoSeries}
                  onPress={this.onEditorTogglePress}
                />
            }

            {
              isEditorActive ?
                <PageToolbarButton
                  label={allSelected ? translate('UnselectAll') : translate('SelectAll')}
                  iconName={icons.CHECK_SQUARE}
                  isDisabled={hasNoSeries}
                  onPress={this.onSelectAllPress}
                /> :
                null
            }

          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            {
              view === 'table' ?
                <TableOptionsModalWrapper
                  {...otherProps}
                  columns={columns}
                  optionsComponent={SeriesIndexTableOptionsConnector}
                >
                  <PageToolbarButton
                    label={translate('Options')}
                    iconName={icons.TABLE}
                  />
                </TableOptionsModalWrapper> :
                null
            }

            {
              view === 'posters' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.POSTER}
                  isDisabled={hasNoSeries}
                  onPress={this.onPosterOptionsPress}
                /> :
                null
            }

            {
              view === 'overview' ?
                <PageToolbarButton
                  label={translate('Options')}
                  iconName={icons.OVERVIEW}
                  isDisabled={hasNoSeries}
                  onPress={this.onOverviewOptionsPress}
                /> :
                null
            }

            <PageToolbarSeparator />

            <SeriesIndexViewMenu
              view={view}
              isDisabled={hasNoSeries}
              onViewSelect={onViewSelect}
            />

            <SeriesIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoSeries}
              onSortSelect={onSortSelect}
            />

            <SeriesIndexFilterMenu
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              isDisabled={hasNoSeries}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <div className={styles.pageContentBodyWrapper}>
          <PageContentBody
            registerScroller={this.setScrollerRef}
            className={styles.contentBody}
            innerClassName={styles[`${view}InnerContentBody`]}
            onScroll={onScroll}
          >
            {
              isFetching && !isPopulated &&
                <LoadingIndicator />
            }

            {
              !isFetching && !!error &&
                <div className={styles.errorMessage}>
                  {getErrorMessage(error, 'Failed to load series from API')}
                </div>
            }

            {
              isLoaded &&
                <div className={styles.contentBodyContainer}>
                  <ViewComponent
                    scroller={scroller}
                    items={items}
                    filters={filters}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    jumpToCharacter={jumpToCharacter}
                    isEditorActive={isEditorActive}
                    allSelected={allSelected}
                    allUnselected={allUnselected}
                    onSelectedChange={this.onSelectedChange}
                    onSelectAllChange={this.onSelectAllChange}
                    selectedState={selectedState}
                    {...otherProps}
                  />

                  <SeriesIndexFooterConnector />
                </div>
            }

            {
              !error && isPopulated && !items.length &&
                <NoSeries totalItems={totalItems} />
            }
          </PageContentBody>

          {
            isLoaded && !!jumpBarItems.order.length &&
              <PageJumpBar
                items={jumpBarItems}
                onItemPress={this.onJumpBarItemPress}
              />
          }
        </div>

        {
          isLoaded && isEditorActive &&
            <SeriesEditorFooter
              seriesIds={selectedSeriesIds}
              selectedCount={selectedSeriesIds.length}
              isSaving={isSaving}
              saveError={saveError}
              isDeleting={isDeleting}
              deleteError={deleteError}
              isOrganizingSeries={isOrganizingSeries}
              isRetaggingSeries={isRetaggingSeries}
              onSaveSelected={this.onSaveSelected}
              onOrganizeSeriesPress={this.onOrganizeSeriesPress}
              onRetagSeriesPress={this.onRetagSeriesPress}
            />
        }

        <SeriesIndexPosterOptionsModal
          isOpen={isPosterOptionsModalOpen}
          onModalClose={this.onPosterOptionsModalClose}
        />

        <SeriesIndexOverviewOptionsModal
          isOpen={isOverviewOptionsModalOpen}
          onModalClose={this.onOverviewOptionsModalClose}
        />

        <OrganizeSeriesModal
          isOpen={this.state.isOrganizingSeriesModalOpen}
          seriesIds={selectedSeriesIds}
          onModalClose={this.onOrganizeSeriesModalClose}
        />

        <RetagSeriesModal
          isOpen={this.state.isRetaggingSeriesModalOpen}
          seriesIds={selectedSeriesIds}
          onModalClose={this.onRetagSeriesModalClose}
        />

      </PageContent>
    );
  }
}

SeriesIndex.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  view: PropTypes.string.isRequired,
  isRefreshingSeries: PropTypes.bool.isRequired,
  isOrganizingSeries: PropTypes.bool.isRequired,
  isRetaggingSeries: PropTypes.bool.isRequired,
  isRssSyncExecuting: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onViewSelect: PropTypes.func.isRequired,
  onRefreshSeriesPress: PropTypes.func.isRequired,
  onRssSyncPress: PropTypes.func.isRequired,
  onScroll: PropTypes.func.isRequired,
  onSaveSelected: PropTypes.func.isRequired
};

export default SeriesIndex;
