import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import getNewSeries from 'Utilities/Series/getNewSeries';
import monitorNewItemsOptions from 'Utilities/Series/monitorNewItemsOptions';
import monitorOptions from 'Utilities/Series/monitorOptions';
import getNewIssue from 'Utilities/Issue/getNewIssue';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import { set, update, updateItem } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'search';
let abortCurrentRequest = null;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isAdding: false,
  isAdded: false,
  addError: null,
  items: [],

  seriesDefaults: {
    rootFolderPath: '',
    monitor: monitorOptions[0].key,
    monitorNewItems: monitorNewItemsOptions[0].key,
    qualityProfileId: 0,
    tags: []
  },

  issueDefaults: {
    rootFolderPath: '',
    monitor: monitorOptions[0].key,
    monitorNewItems: monitorNewItemsOptions[0].key,
    qualityProfileId: 0,
    tags: []
  }
};

export const persistState = [
  'search.issueDefaults',
  'search.seriesDefaults'
];

//
// Actions Types

export const GET_SEARCH_RESULTS = 'search/getSearchResults';
export const ADD_SERIES = 'search/addSeries';
export const ADD_ISSUE = 'search/addIssue';
export const CLEAR_SEARCH_RESULTS = 'search/clearSearchResults';
export const SET_SERIES_ADD_DEFAULT = 'search/setSeriesAddDefault';
export const SET_ISSUE_ADD_DEFAULT = 'search/setIssueAddDefault';

//
// Action Creators

export const getSearchResults = createThunk(GET_SEARCH_RESULTS);
export const addSeries = createThunk(ADD_SERIES);
export const addIssue = createThunk(ADD_ISSUE);
export const clearSearchResults = createAction(CLEAR_SEARCH_RESULTS);
export const setSeriesAddDefault = createAction(SET_SERIES_ADD_DEFAULT);
export const setIssueAddDefault = createAction(SET_ISSUE_ADD_DEFAULT);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [GET_SEARCH_RESULTS]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    if (abortCurrentRequest) {
      abortCurrentRequest();
    }

    const { request, abortRequest } = createAjaxRequest({
      url: '/search',
      data: {
        term: payload.term
      }
    });

    abortCurrentRequest = abortRequest;

    request.done((data) => {
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
  },

  [ADD_SERIES]: function(getState, payload, dispatch) {
    dispatch(set({ section, isAdding: true }));

    const foreignSeriesId = payload.foreignSeriesId;
    const items = getState().search.items;
    const itemToAdd = _.find(items, { foreignId: foreignSeriesId });
    const newSeries = getNewSeries(_.cloneDeep(itemToAdd.series), payload);

    const promise = createAjaxRequest({
      url: '/series',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(newSeries)
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        updateItem({ section: 'series', ...data }),

        set({
          section,
          isAdding: false,
          isAdded: true,
          addError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        isAdded: false,
        addError: xhr
      }));
    });
  },

  [ADD_ISSUE]: function(getState, payload, dispatch) {
    dispatch(set({ section, isAdding: true }));

    const foreignIssueId = payload.foreignIssueId;
    const items = getState().search.items;
    const itemToAdd = _.find(items, { foreignId: foreignIssueId });
    const newIssue = getNewIssue(_.cloneDeep(itemToAdd.issue), payload);

    const promise = createAjaxRequest({
      url: '/issue',
      method: 'POST',
      dataType: 'json',
      contentType: 'application/json',
      data: JSON.stringify(newIssue)
    }).request;

    promise.done((data) => {
      itemToAdd.issue = data;
      dispatch(batchActions([
        updateItem({ section: 'series', ...data.series }),
        updateItem({ section: 'issues', ...data }),
        updateItem({ section, ...itemToAdd }),

        set({
          section,
          isAdding: false,
          isAdded: true,
          addError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isAdding: false,
        isAdded: false,
        addError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_SERIES_ADD_DEFAULT]: function(state, { payload }) {
    const newState = getSectionState(state, section);

    newState.seriesDefaults = {
      ...newState.seriesDefaults,
      ...payload
    };

    return updateSectionState(state, section, newState);
  },

  [SET_ISSUE_ADD_DEFAULT]: function(state, { payload }) {
    const newState = getSectionState(state, section);

    newState.issueDefaults = {
      ...newState.issueDefaults,
      ...payload
    };

    return updateSectionState(state, section, newState);
  },

  [CLEAR_SEARCH_RESULTS]: function(state) {
    const {
      seriesDefaults,
      issueDefaults,
      ...otherDefaultState
    } = defaultState;

    return Object.assign({}, state, otherDefaultState);
  }

}, defaultState, section);
