import _ from 'lodash';
import moment from 'moment';
import React from 'react';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import issueEntities from 'Issue/issueEntities';
import Icon from 'Components/Icon';
import { filterTypePredicates, filterTypes, icons, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import translate from 'Utilities/String/translate';
import { removeItem, set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'issues';

export const filters = [
  {
    key: 'all',
    label: () => translate('All'),
    filters: []
  },
  {
    key: 'monitored',
    label: () => translate('Monitored'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'unmonitored',
    label: () => translate('Unmonitored'),
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: () => translate('Missing'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'wanted',
    label: () => translate('Wanted'),
    filters: [
      {
        key: 'monitored',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      },
      {
        key: 'releaseDate',
        value: moment(),
        type: filterTypes.LESS_THAN
      }
    ]
  }
];

export const filterPredicates = {
  missing: function(item) {
    const { statistics = {} } = item;

    return !statistics.hasOwnProperty('issueFileCount') || statistics.issueFileCount === 0;
  },

  releaseDate: function(item, filterValue, type) {
    return dateFilterPredicate(item.releaseDate, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  qualityProfileId: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.series.qualityProfileId, filterValue);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  path: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.series.path, filterValue);
  },

  issueFileCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const issueCount = item.statistics ? item.statistics.issueFileCount : 0;

    return predicate(issueCount, filterValue);
  },

  sizeOnDisk: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const sizeOnDisk = item.statistics && item.statistics.sizeOnDisk ?
      item.statistics.sizeOnDisk :
      0;

    return predicate(sizeOnDisk, filterValue);
  }
};

export const sortPredicates = {
  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
  },

  path: function(item) {
    return item.series.path;
  },

  series: function(item) {
    return item.seriesTitle;
  },

  rating: function(item) {
    return item.ratings.value;
  },

  status: function(item) {
    let result = 0;

    const hasIssueFile = !!item.statistics?.issueFileCount;
    const isAvailable = Date.parse(item.releaseDate) < new Date();

    if (isAvailable) {
      result++;
    }

    if (item.monitored) {
      result += 2;
    }

    if (hasIssueFile) {
      result += 4;
    }

    return result;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  items: [],
  pendingChanges: {},
  sortPredicates: {
    rating: function(item) {
      return item.ratings.value;
    }
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
      name: 'monitored',
      columnLabel: 'Monitored',
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'issueNumber',
      label: '#',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'title',
      label: 'Title',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'series',
      label: 'Series',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'indexerFlags',
      columnLabel: () => translate('IndexerFlags'),
      label: React.createElement(Icon, {
        name: icons.FLAG,
        title: () => translate('IndexerFlags')
      }),
      isVisible: false
    },
    {
      name: 'status',
      label: 'Status',
      isVisible: true,
      isSortable: true
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ]
};

export const persistState = [
  'issues.sortKey',
  'issues.sortDirection',
  'issues.columns'
];

//
// Actions Types

export const FETCH_ISSUES_LIST = 'issues/fetchIssues';
export const SET_ISSUES_LIST_SORT = 'issues/setIssuesSort';
export const SET_ISSUES_LIST_TABLE_OPTION = 'issues/setIssuesTableOption';
export const CLEAR_ISSUES_LIST = 'issues/clearIssues';
export const SET_ISSUE_VALUE = 'issues/setIssueValue';
export const SAVE_ISSUE = 'issues/saveIssue';
export const DELETE_ISSUE = 'issues/deleteIssue';
export const DELETE_SERIES_ISSUES_LIST = 'issues/deleteSeriesIssues';
export const TOGGLE_ISSUE_MONITORED = 'issues/toggleIssueMonitored';
export const TOGGLE_ISSUES_LIST_MONITORED = 'issues/toggleIssuesMonitored';

//
// Action Creators

export const fetchIssues = createThunk(FETCH_ISSUES_LIST);
export const setIssuesSort = createAction(SET_ISSUES_LIST_SORT);
export const setIssuesTableOption = createAction(SET_ISSUES_LIST_TABLE_OPTION);
export const clearIssues = createAction(CLEAR_ISSUES_LIST);
export const toggleIssueMonitored = createThunk(TOGGLE_ISSUE_MONITORED);
export const toggleIssuesMonitored = createThunk(TOGGLE_ISSUES_LIST_MONITORED);

export const saveIssue = createThunk(SAVE_ISSUE);

export const deleteIssue = createThunk(DELETE_ISSUE, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const deleteSeriesIssues = createThunk(DELETE_SERIES_ISSUES_LIST, (payload) => {
  return {
    ...payload,
    queryParams: {
      seriesId: payload.seriesId
    }
  };
});

export const setIssueValue = createAction(SET_ISSUE_VALUE, (payload) => {
  return {
    section: 'issues',
    ...payload
  };
});

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_ISSUES_LIST]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const { request, abortRequest } = createAjaxRequest({
      url: '/issue',
      data: payload,
      traditional: true
    });

    request.done((data) => {
      // Preserve issues for other series we didn't fetch
      if (payload.hasOwnProperty('seriesId')) {
        const oldIssues = getState().issues.items;
        const newIssues = oldIssues.filter((x) => x.seriesId !== payload.seriesId);
        data = newIssues.concat(data);
      }

      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    request.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr.aborted ? null : xhr
      }));
    });

    return abortRequest;
  },

  [SAVE_ISSUE]: createSaveProviderHandler(section, '/issue'),
  [DELETE_ISSUE]: createRemoveItemHandler(section, '/issue'),

  [DELETE_SERIES_ISSUES_LIST]: function(getState, payload, dispatch) {
    const { seriesId } = payload;
    const issues = getState().issues.items;

    const toDelete = issues.filter((x) => x.seriesId === seriesId);

    dispatch(batchActions(toDelete.map((b) => removeItem({ section, id: b.id }))));
  },

  [TOGGLE_ISSUE_MONITORED]: function(getState, payload, dispatch) {
    const {
      issueId,
      issueEntity = issueEntities.ISSUES_LIST,
      monitored
    } = payload;

    const issueSection = _.last(issueEntity.split('.'));

    dispatch(updateItem({
      id: issueId,
      section: issueSection,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/issue/${issueId}`,
      method: 'PUT',
      data: JSON.stringify({ monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id: issueId,
        section: issueSection,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id: issueId,
        section: issueSection,
        isSaving: false
      }));
    });
  },

  [TOGGLE_ISSUES_LIST_MONITORED]: function(getState, payload, dispatch) {
    const {
      issueIds,
      issueEntity = issueEntities.ISSUES_LIST,
      monitored
    } = payload;

    dispatch(batchActions(
      issueIds.map((issueId) => {
        return updateItem({
          id: issueId,
          section: issueEntity,
          isSaving: true
        });
      })
    ));

    const promise = createAjaxRequest({
      url: '/issue/monitor',
      method: 'PUT',
      data: JSON.stringify({ issueIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(batchActions(
        issueIds.map((issueId) => {
          return updateItem({
            id: issueId,
            section: issueEntity,
            isSaving: false,
            monitored
          });
        })
      ));
    });

    promise.fail((xhr) => {
      dispatch(batchActions(
        issueIds.map((issueId) => {
          return updateItem({
            id: issueId,
            section: issueEntity,
            isSaving: false
          });
        })
      ));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_ISSUES_LIST_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_ISSUES_LIST_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_ISSUE_VALUE]: createSetSettingValueReducer(section),

  [CLEAR_ISSUES_LIST]: (state) => {
    return Object.assign({}, state, {
      isFetching: false,
      isPopulated: false,
      error: null,
      items: []
    });
  }

}, defaultState, section);
