import _ from 'lodash';
import { createAction } from 'redux-actions';
import { filterTypePredicates, filterTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import { set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';
import { fetchIssues } from './issueActions';

//
// Variables

export const section = 'series';

export const filters = [
  {
    key: 'all',
    label: 'All',
    filters: []
  },
  {
    key: 'monitored',
    label: 'Monitored Only',
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
    label: 'Unmonitored Only',
    filters: [
      {
        key: 'monitored',
        value: false,
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'continuing',
    label: 'Continuing Only',
    filters: [
      {
        key: 'status',
        value: 'continuing',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'ended',
    label: 'Ended Only',
    filters: [
      {
        key: 'status',
        value: 'ended',
        type: filterTypes.EQUAL
      }
    ]
  },
  {
    key: 'missing',
    label: 'Missing Issues',
    filters: [
      {
        key: 'missing',
        value: true,
        type: filterTypes.EQUAL
      }
    ]
  }
];

export const filterPredicates = {
  missing: function(item) {
    const { statistics = {} } = item;

    return statistics.issueCount - statistics.issueFileCount > 0;
  },

  nextIssue: function(item, filterValue, type) {
    return dateFilterPredicate(item.nextIssue, filterValue, type);
  },

  lastIssue: function(item, filterValue, type) {
    return dateFilterPredicate(item.lastIssue, filterValue, type);
  },

  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  },

  ratings: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];

    return predicate(item.ratings.value * 10, filterValue);
  },

  issueCount: function(item, filterValue, type) {
    const predicate = filterTypePredicates[type];
    const issueCount = item.statistics ? item.statistics.issueCount : 0;

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
  status: function(item) {
    let result = 0;

    if (item.monitored) {
      result += 2;
    }

    if (item.status === 'continuing') {
      result++;
    }

    return result;
  },

  sizeOnDisk: function(item) {
    const { statistics = {} } = item;

    return statistics.sizeOnDisk || 0;
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
  items: [],
  itemMap: {},
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {}
};

//
// Actions Types

export const FETCH_SERIES = 'series/fetchSeries';
export const SET_SERIES_VALUE = 'series/setSeriesValue';
export const SAVE_SERIES = 'series/saveSeries';
export const DELETE_SERIES = 'series/deleteSeries';

export const SAVE_SERIES_OVERRIDE = 'series/saveSeriesOverride';
export const CLEAR_SERIES_OVERRIDE = 'series/clearSeriesOverride';
export const TOGGLE_SERIES_MONITORED = 'series/toggleSeriesMonitored';
export const UPDATE_ISSUE_MONITORED = 'series/updateIssueMonitored';

//
// Action Creators

export const fetchSeries = createThunk(FETCH_SERIES);
export const saveSeries = createThunk(SAVE_SERIES, (payload) => {
  const newPayload = {
    ...payload
  };

  if (payload.moveFiles) {
    newPayload.queryParams = {
      moveFiles: true
    };
  }

  delete newPayload.moveFiles;

  return newPayload;
});

export const deleteSeries = createThunk(DELETE_SERIES, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles,
      addImportListExclusion: payload.addImportListExclusion
    }
  };
});

export const saveSeriesOverride = createThunk(SAVE_SERIES_OVERRIDE);
export const clearSeriesOverride = createThunk(CLEAR_SERIES_OVERRIDE);
export const toggleSeriesMonitored = createThunk(TOGGLE_SERIES_MONITORED);
export const updateIssueMonitor = createThunk(UPDATE_ISSUE_MONITORED);

export const setSeriesValue = createAction(SET_SERIES_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_SERIES]: createFetchHandler(section, '/series'),
  [SAVE_SERIES]: createSaveProviderHandler(section, '/series'),
  [DELETE_SERIES]: createRemoveItemHandler(section, '/series'),

  [SAVE_SERIES_OVERRIDE]: (getState, payload, dispatch) => {
    const { id, fields } = payload;

    dispatch(updateItem({ id, section, isSaving: true }));

    const promise = createAjaxRequest({
      url: `/series/${id}/override`,
      method: 'PUT',
      data: JSON.stringify(fields),
      dataType: 'json',
      contentType: 'application/json'
    }).request;

    promise.done(() => {
      dispatch(fetchSeries());
      dispatch(updateItem({ id, section, isSaving: false }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({ id, section, isSaving: false }));
    });
  },

  [CLEAR_SERIES_OVERRIDE]: (getState, payload, dispatch) => {
    const { id } = payload;

    dispatch(updateItem({ id, section, isSaving: true }));

    const promise = createAjaxRequest({
      url: `/series/${id}/override`,
      method: 'DELETE'
    }).request;

    promise.done(() => {
      dispatch(fetchSeries());
      dispatch(updateItem({ id, section, isSaving: false }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({ id, section, isSaving: false }));
    });
  },

  [TOGGLE_SERIES_MONITORED]: (getState, payload, dispatch) => {
    const {
      seriesId: id,
      monitored
    } = payload;

    const series = _.find(getState().series.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/series/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...series,
        monitored
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false
      }));
    });
  },

  [UPDATE_ISSUE_MONITORED]: function(getState, payload, dispatch) {
    const {
      id,
      monitor
    } = payload;

    const seriesToUpdate = { id };

    if (monitor !== 'None') {
      seriesToUpdate.monitored = true;
    }

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/issueshelf',
      method: 'POST',
      data: JSON.stringify({
        series: [{ id }],
        monitoringOptions: { monitor }
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(fetchIssues({ seriesId: id }));

      dispatch(set({
        section,
        isSaving: false,
        saveError: null
      }));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isSaving: false,
        saveError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_SERIES_VALUE]: createSetSettingValueReducer(section)

}, defaultState, section);
