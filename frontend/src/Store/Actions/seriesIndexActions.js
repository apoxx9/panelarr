import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes, filterTypePredicates, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import sortByName from 'Utilities/Array/sortByName';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { filterPredicates, filters, sortPredicates } from './seriesActions';
import { set, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'seriesIndex';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  sortKey: 'sortNameLastFirst',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortNameLastFirst',
  secondarySortDirection: sortDirections.ASCENDING,
  view: 'posters',

  posterOptions: {
    detailedProgressBar: false,
    size: 'large',
    showTitle: 'lastFirst',
    showMonitored: true,
    showQualityProfile: true,
    showSearchAction: false
  },

  overviewOptions: {
    showTitle: 'lastFirst',
    detailedProgressBar: false,
    size: 'medium',
    showMonitored: true,
    showQualityProfile: true,
    showLastIssue: false,
    showAdded: false,
    showIssueCount: true,
    showPath: false,
    showSizeOnDisk: false,
    showSearchAction: false
  },

  tableOptions: {
    showTitle: 'lastFirst',
    showBanners: false,
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
      name: 'publisher',
      label: 'Publisher',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'sortName',
      label: 'Comic',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'year',
      label: 'Year',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'lastIssue',
      label: 'Last Issue',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'added',
      label: 'Published',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'issueProgress',
      label: 'Issues',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'seriesStatus',
      label: 'Status',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      isSortable: true,
      isVisible: false,
      isHidden: true
    },
    {
      name: 'nextIssue',
      label: 'Next Issue',
      isSortable: true,
      isVisible: false
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

    publisher: function(item) {
      return item.disambiguation || '';
    },

    year: function(item) {
      return item.year || 0;
    },

    issueProgress: function(item) {
      const { statistics = {} } = item;

      const {
        issueCount = 0,
        issueFileCount
      } = statistics;

      const progress = issueCount ? issueFileCount / issueCount * 100 : 100;

      return progress + issueCount / 1000000;
    },

    nextIssue: function(item) {
      if (item.nextIssue) {
        return item.nextIssue.releaseDate;
      }
      return '1/1/1000';
    },

    lastIssue: function(item) {
      if (item.lastIssue) {
        return item.lastIssue.releaseDate;
      }
      return '1/1/1000';
    },

    issueCount: function(item) {
      const { statistics = {} } = item;

      return statistics.issueCount || 0;
    },

    ratings: function(item) {
      const { ratings = {} } = item;

      return ratings.value;
    },

    seriesStatus: function(item) {
      return item.status;
    }
  },

  selectedFilterKey: 'all',

  filters,

  filterPredicates: {
    ...filterPredicates,

    issueProgress: function(item, filterValue, type) {
      const { statistics = {} } = item;

      const {
        issueCount = 0,
        issueFileCount
      } = statistics;

      const progress = issueCount ?
        issueFileCount / issueCount * 100 :
        100;

      const predicate = filterTypePredicates[type];

      return predicate(progress, filterValue);
    }
  },

  filterBuilderProps: [
    {
      name: 'monitored',
      label: 'Monitored',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'status',
      label: 'Status',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.SERIES_STATUS
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'nextIssue',
      label: 'Next Issue',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'lastIssue',
      label: 'Last Issue',
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
      name: 'issueCount',
      label: 'Issue Count',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'issueProgress',
      label: 'Issue Progress',
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
        const tagList = items.reduce((acc, series) => {
          series.genres.forEach((genre) => {
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
  'seriesIndex.sortKey',
  'seriesIndex.sortDirection',
  'seriesIndex.selectedFilterKey',
  'seriesIndex.customFilters',
  'seriesIndex.view',
  'seriesIndex.columns',
  'seriesIndex.posterOptions',
  'seriesIndex.bannerOptions',
  'seriesIndex.overviewOptions',
  'seriesIndex.tableOptions'
];

//
// Actions Types

export const SET_SERIES_SORT = 'seriesIndex/setSeriesSort';
export const SET_SERIES_FILTER = 'seriesIndex/setSeriesFilter';
export const SET_SERIES_VIEW = 'seriesIndex/setSeriesView';
export const SET_SERIES_TABLE_OPTION = 'seriesIndex/setSeriesTableOption';
export const SET_SERIES_POSTER_OPTION = 'seriesIndex/setSeriesPosterOption';
export const SET_SERIES_BANNER_OPTION = 'seriesIndex/setSeriesBannerOption';
export const SET_SERIES_OVERVIEW_OPTION = 'seriesIndex/setSeriesOverviewOption';
export const SAVE_SERIES_EDITOR = 'seriesIndex/saveSeriesEditor';
export const BULK_DELETE_SERIES = 'seriesIndex/bulkDeleteSeries';

//
// Action Creators

export const setSeriesSort = createAction(SET_SERIES_SORT);
export const setSeriesFilter = createAction(SET_SERIES_FILTER);
export const setSeriesView = createAction(SET_SERIES_VIEW);
export const setSeriesTableOption = createAction(SET_SERIES_TABLE_OPTION);
export const setSeriesPosterOption = createAction(SET_SERIES_POSTER_OPTION);
export const setSeriesBannerOption = createAction(SET_SERIES_BANNER_OPTION);
export const setSeriesOverviewOption = createAction(SET_SERIES_OVERVIEW_OPTION);
export const saveSeriesEditor = createThunk(SAVE_SERIES_EDITOR);
export const bulkDeleteSeries = createThunk(BULK_DELETE_SERIES);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SAVE_SERIES_EDITOR]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/series/editor',
      method: 'PUT',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((series) => {
          return updateItem({
            id: series.id,
            section: 'series',
            ...series
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

  [BULK_DELETE_SERIES]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isDeleting: true
    }));

    const promise = createAjaxRequest({
      url: '/series/editor',
      method: 'DELETE',
      data: JSON.stringify(payload),
      dataType: 'json'
    }).request;

    promise.done(() => {
      // SignaR will take care of removing the series from the collection

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

  [SET_SERIES_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_SERIES_FILTER]: createSetClientSideCollectionFilterReducer(section),

  [SET_SERIES_VIEW]: function(state, { payload }) {
    return Object.assign({}, state, { view: payload.view });
  },

  [SET_SERIES_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_SERIES_POSTER_OPTION]: function(state, { payload }) {
    const posterOptions = state.posterOptions;

    return {
      ...state,
      posterOptions: {
        ...posterOptions,
        ...payload
      }
    };
  },

  [SET_SERIES_BANNER_OPTION]: function(state, { payload }) {
    const bannerOptions = state.bannerOptions;

    return {
      ...state,
      bannerOptions: {
        ...bannerOptions,
        ...payload
      }
    };
  },

  [SET_SERIES_OVERVIEW_OPTION]: function(state, { payload }) {
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
