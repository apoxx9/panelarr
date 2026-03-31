import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes, filterTypePredicates, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import sortByName from 'Utilities/Array/sortByName';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, updateItem } from './baseActions';
import { filterPredicates, filters, sortPredicates } from './issueActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'issueIndex';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  sortKey: 'title',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'title',
  secondarySortDirection: sortDirections.ASCENDING,
  view: 'posters',

  posterOptions: {
    detailedProgressBar: false,
    size: 'large',
    showTitle: true,
    showSeries: true,
    showMonitored: true,
    showQualityProfile: true,
    showSearchAction: false
  },

  overviewOptions: {
    detailedProgressBar: false,
    size: 'medium',
    showReleaseDate: true,
    showMonitored: true,
    showQualityProfile: true,
    showAdded: false,
    showPath: false,
    showSizeOnDisk: false,
    showSearchAction: false
  },

  tableOptions: {
    showSearchAction: false
  },

  columns: [
    {
      name: 'select',
      columnLabel: 'Select',
      isSortable: false,
      isVisible: true,
      isModifiable: false,
      isHidden: true
    },
    {
      name: 'status',
      columnLabel: 'Status',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'title',
      label: 'Issue',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'seriesName',
      label: 'Series',
      isSortable: true,
      isVisible: true,
      isModifiable: true
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'qualityProfileId',
      label: 'Format Profile',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'added',
      label: 'Added',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'issueFileCount',
      label: 'File Count',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'path',
      label: 'Path',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'sizeOnDisk',
      label: 'Size on Disk',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'genres',
      label: 'Genres',
      isSortable: false,
      isVisible: false
    },
    {
      name: 'ratings',
      label: 'Rating',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'tags',
      label: 'Tags',
      isSortable: false,
      isVisible: false
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ],

  sortPredicates: {
    ...sortPredicates,

    seriesName: function(item) {
      return item.series.sortName;
    },

    issueFileCount: function(item) {
      const { statistics = {} } = item;

      return statistics.issueFileCount || 0;
    },

    ratings: function(item) {
      const { ratings = {} } = item;

      return ratings.value;
    }
  },

  selectedFilterKey: 'all',

  filters,

  filterPredicates: {
    ...filterPredicates,

    series: function(item, filterValue, type) {
      const predicate = filterTypePredicates[type];

      return predicate(item.series.seriesName, filterValue);
    },

    anyEditionOk: function(item, filterValue, type) {
      const predicate = filterTypePredicates[type];

      return predicate(item.anyEditionOk, filterValue);
    }
  },

  filterBuilderProps: [
    {
      name: 'series',
      label: 'Series',
      type: filterBuilderTypes.STRING
    },
    {
      name: 'title',
      label: 'Title',
      type: filterBuilderTypes.STRING
    },
    {
      name: 'monitored',
      label: 'Monitored',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },


    {
      name: 'qualityProfileId',
      label: 'Format Profile',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'added',
      label: 'Added',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'issueFileCount',
      label: 'File Count',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'path',
      label: 'Path',
      type: filterBuilderTypes.STRING
    },
    {
      name: 'sizeOnDisk',
      label: 'Size on Disk',
      type: filterBuilderTypes.NUMBER,
      valueType: filterBuilderValueTypes.BYTES
    },
    {
      name: 'genres',
      label: 'Genres',
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function(items) {
        const tagList = items.reduce((acc, Issue) => {
          Issue.genres.forEach((genre) => {
            acc.push({
              id: genre,
              name: genre
            });
          });

          return acc;
        }, []);

        return tagList.sort(sortByName);
      }
    },
    {
      name: 'ratings',
      label: 'Rating',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'tags',
      label: 'Tags',
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG
    }
  ]
};

export const persistState = [
  'issueIndex.sortKey',
  'issueIndex.sortDirection',
  'issueIndex.selectedFilterKey',
  'issueIndex.customFilters',
  'issueIndex.view',
  'issueIndex.columns',
  'issueIndex.posterOptions',
  'issueIndex.bannerOptions',
  'issueIndex.overviewOptions',
  'issueIndex.tableOptions'
];

//
// Actions Types

export const SET_ISSUE_SORT = 'issueIndex/setIssueSort';
export const SET_ISSUE_FILTER = 'issueIndex/setIssueFilter';
export const SET_ISSUE_VIEW = 'issueIndex/setIssueView';
export const SET_ISSUE_TABLE_OPTION = 'issueIndex/setIssueTableOption';
export const SET_ISSUE_POSTER_OPTION = 'issueIndex/setIssuePosterOption';
export const SET_ISSUE_BANNER_OPTION = 'issueIndex/setIssueBannerOption';
export const SET_ISSUE_OVERVIEW_OPTION = 'issueIndex/setIssueOverviewOption';
export const SAVE_ISSUE_EDITOR = 'issueEditor/saveIssueEditor';
export const BULK_DELETE_ISSUE = 'issueEditor/bulkDeleteIssue';

//
// Action Creators

export const setIssueSort = createAction(SET_ISSUE_SORT);
export const setIssueFilter = createAction(SET_ISSUE_FILTER);
export const setIssueView = createAction(SET_ISSUE_VIEW);
export const setIssueTableOption = createAction(SET_ISSUE_TABLE_OPTION);
export const setIssuePosterOption = createAction(SET_ISSUE_POSTER_OPTION);
export const setIssueBannerOption = createAction(SET_ISSUE_BANNER_OPTION);
export const setIssueOverviewOption = createAction(SET_ISSUE_OVERVIEW_OPTION);
export const saveIssueEditor = createThunk(SAVE_ISSUE_EDITOR);
export const bulkDeleteIssue = createThunk(BULK_DELETE_ISSUE);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SAVE_ISSUE_EDITOR]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/issue/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((issue) => {
          return updateItem({
            id: issue.id,
            section: 'issues',
            ...issue
          });
        }),

        set({
          section,
          isSaving: false,
          saveError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  },

  [BULK_DELETE_ISSUE]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isDeleting: true
    }));

    const promise = createAjaxRequest({
      url: '/issue/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done(() => {
      // SignalR will take care of removing the issue from the collection

      dispatch(set({
        section,
        isDeleting: false,
        deleteError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ISSUE_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_ISSUE_FILTER]: createSetClientSideCollectionFilterReducer(section),

  [SET_ISSUE_VIEW]: function(state, { payload }) {
    return Object.assign({}, state, { view: payload.view });
  },

  [SET_ISSUE_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_ISSUE_POSTER_OPTION]: function(state, { payload }) {
    const posterOptions = state.posterOptions;

    return {
      ...state,
      posterOptions: {
        ...posterOptions,
        ...payload
      }
    };
  },

  [SET_ISSUE_BANNER_OPTION]: function(state, { payload }) {
    const bannerOptions = state.bannerOptions;

    return {
      ...state,
      bannerOptions: {
        ...bannerOptions,
        ...payload
      }
    };
  },

  [SET_ISSUE_OVERVIEW_OPTION]: function(state, { payload }) {
    const overviewOptions = state.overviewOptions;

    return {
      ...state,
      overviewOptions: {
        ...overviewOptions,
        ...payload
      }
    };
  }

}, defaultState, section);
