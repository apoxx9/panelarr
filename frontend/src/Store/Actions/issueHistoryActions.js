import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'issueHistory';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: []
};

//
// Actions Types

export const FETCH_ISSUE_HISTORY = 'issueHistory/fetchIssueHistory';
export const CLEAR_ISSUE_HISTORY = 'issueHistory/clearIssueHistory';
export const ISSUE_HISTORY_MARK_AS_FAILED = 'issueHistory/issueHistoryMarkAsFailed';

//
// Action Creators

export const fetchIssueHistory = createThunk(FETCH_ISSUE_HISTORY);
export const clearIssueHistory = createAction(CLEAR_ISSUE_HISTORY);
export const issueHistoryMarkAsFailed = createThunk(ISSUE_HISTORY_MARK_AS_FAILED);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_ISSUE_HISTORY]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const queryParams = {
      pageSize: 1000,
      page: 1,
      sortKey: 'date',
      sortDirection: sortDirections.DESCENDING,
      issueId: payload.issueId
    };

    const promise = createAjaxRequest({
      url: '/history',
      data: queryParams
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        update({ section, data: data.records }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr
      }));
    });
  },

  [ISSUE_HISTORY_MARK_AS_FAILED]: function(getState, payload, dispatch) {
    const {
      historyId,
      issueId
    } = payload;

    const promise = createAjaxRequest({
      url: `/history/failed/${historyId}`,
      method: 'POST'
    }).request;

    promise.done(() => {
      dispatch(fetchIssueHistory({ issueId }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [CLEAR_ISSUE_HISTORY]: (state) => {
    return Object.assign({}, state, defaultState);
  }

}, defaultState, section);

