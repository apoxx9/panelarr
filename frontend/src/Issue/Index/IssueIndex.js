import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import NoSeries from 'Series/NoSeries';
import IssueEditorFooter from 'Issue/Editor/IssueEditorFooter';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, kinds, sortDirections } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import IssueIndexFooterConnector from './IssueIndexFooterConnector';
import IssueIndexFilterMenu from './Menus/IssueIndexFilterMenu';
import IssueIndexSortMenu from './Menus/IssueIndexSortMenu';
import IssueIndexViewMenu from './Menus/IssueIndexViewMenu';
import IssueIndexOverviewsConnector from './Overview/IssueIndexOverviewsConnector';
import IssueIndexOverviewOptionsModal from './Overview/Options/IssueIndexOverviewOptionsModal';
import IssueIndexPostersConnector from './Posters/IssueIndexPostersConnector';
import IssueIndexPosterOptionsModal from './Posters/Options/IssueIndexPosterOptionsModal';
import IssueIndexTableConnector from './Table/IssueIndexTableConnector';
import IssueIndexTableOptionsConnector from './Table/IssueIndexTableOptionsConnector';
import styles from './IssueIndex.css';

function getViewComponent(view) {
  if (view === 'posters') {
    return IssueIndexPostersConnector;
  }

  if (view === 'overview') {
    return IssueIndexOverviewsConnector;
  }

  return IssueIndexTableConnector;
}

class IssueIndex extends Component {

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
      isConfirmSearchModalOpen: false,
      isEditorActive: false,
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

    items.forEach((issue) => {
      const isItemSelected = selectedState[issue.id];

      if (isItemSelected) {
        newSelectedState[issue.id] = isItemSelected;
      } else {
        newSelectedState[issue.id] = false;
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
      sortDirection,
      isPopulated
    } = this.props;

    // Reset if not sorting by sortName
    if (!isPopulated || (sortKey !== 'title' && sortKey !== 'seriesTitle')) {
      this.setState({ jumpBarItems: { order: [] } });
      return;
    }

    const characters = _.reduce(items, (acc, item) => {
      let char = item[sortKey].charAt(0);

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
      issueIds: this.getSelectedIds(),
      ...changes
    });
  };

  onSearchPress = () => {
    this.setState({ isConfirmSearchModalOpen: true });
  };

  onRefreshIssuePress = () => {
    const selectedIds = this.getSelectedIds();
    const refreshIds = this.state.isEditorActive && selectedIds.length > 0 ? selectedIds : [];

    this.props.onRefreshIssuePress(refreshIds);
  };

  onSearchConfirmed = () => {
    const selectedIssueIds = this.getSelectedIds();
    const searchIds = this.state.isEditorActive && selectedIssueIds.length > 0 ? selectedIssueIds : this.props.items.map((m) => m.id);

    this.props.onSearchPress(searchIds);
    this.setState({ isConfirmSearchModalOpen: false });
  };

  onConfirmSearchModalClose = () => {
    this.setState({ isConfirmSearchModalOpen: false });
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
      isRefreshingIssue,
      isRssSyncExecuting,
      isSearching,
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
      isConfirmSearchModalOpen,
      isEditorActive,
      selectedState,
      allSelected,
      allUnselected
    } = this.state;

    const selectedIssueIds = this.getSelectedIds();

    const ViewComponent = getViewComponent(view);
    const isLoaded = !!(!error && isPopulated && items.length && scroller);
    const hasNoSeries = !totalItems;

    const refreshLabel = isEditorActive && selectedIssueIds.length > 0 ? translate('UpdateSelected') : translate('UpdateAll');
    const searchIndexLabel = selectedFilterKey === 'all' ? translate('SearchAll') : translate('SearchFiltered');
    const searchEditorLabel = selectedIssueIds.length > 0 ? translate('SearchSelected') : translate('SearchAll');
    const searchWarningCount = isEditorActive && selectedIssueIds.length > 0 ? selectedIssueIds.length : items.length;

    return (
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={refreshLabel}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              isSpinning={isRefreshingIssue}
              onPress={this.onRefreshIssuePress}
            />

            <PageToolbarButton
              label={translate('RSSSync')}
              iconName={icons.RSS}
              isSpinning={isRssSyncExecuting}
              isDisabled={hasNoSeries}
              onPress={onRssSyncPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={isEditorActive ? searchEditorLabel : searchIndexLabel}
              iconName={icons.SEARCH}
              isDisabled={isSearching || !items.length}
              onPress={this.onSearchPress}
            />

            <PageToolbarSeparator />

            {
              isEditorActive ?
                <PageToolbarButton
                  label={translate('IssueIndex')}
                  iconName={icons.SERIES_CONTINUING}
                  isDisabled={hasNoSeries}
                  onPress={this.onEditorTogglePress}
                /> :
                <PageToolbarButton
                  label={translate('IssueEditor')}
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
                  optionsComponent={IssueIndexTableOptionsConnector}
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

            <IssueIndexViewMenu
              view={view}
              isDisabled={hasNoSeries}
              onViewSelect={onViewSelect}
            />

            <IssueIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoSeries}
              onSortSelect={onSortSelect}
            />

            <IssueIndexFilterMenu
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
                  {getErrorMessage(error, 'Failed to load issues from API')}
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

                  <IssueIndexFooterConnector />
                </div>
            }

            {
              !error && isPopulated && !items.length &&
                <NoSeries
                  totalItems={totalItems}
                  itemType={'issues'}
                />
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

        <IssueIndexPosterOptionsModal
          isOpen={isPosterOptionsModalOpen}
          onModalClose={this.onPosterOptionsModalClose}
        />

        <IssueIndexOverviewOptionsModal
          isOpen={isOverviewOptionsModalOpen}
          onModalClose={this.onOverviewOptionsModalClose}

        />

        <ConfirmModal
          isOpen={isConfirmSearchModalOpen}
          kind={kinds.DANGER}
          title={translate('MassIssueSearch')}
          message={
            <div>
              <div>
                {translate('MassIssueSearchWarning', [searchWarningCount])}
              </div>
              <div>
                {translate('ThisCannotBeCancelled')}
              </div>
            </div>
          }
          confirmLabel={translate('Search')}
          onConfirm={this.onSearchConfirmed}
          onCancel={this.onConfirmSearchModalClose}
        />
      </PageContent>
    );
  }
}

IssueIndex.propTypes = {
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
  isRefreshingIssue: PropTypes.bool.isRequired,
  isSearching: PropTypes.bool.isRequired,
  isRssSyncExecuting: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onViewSelect: PropTypes.func.isRequired,
  onRefreshIssuePress: PropTypes.func.isRequired,
  onRssSyncPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  onScroll: PropTypes.func.isRequired,
  onSaveSelected: PropTypes.func.isRequired
};

export default IssueIndex;
