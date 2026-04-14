import _ from 'lodash';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import issueEntities from 'Issue/issueEntities';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { removeItem, set, updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createClearReducer from './Creators/Reducers/createClearReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'issueFiles';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  sortKey: 'path',
  sortDirection: sortDirections.ASCENDING,

  error: null,
  isDeleting: false,
  deleteError: null,
  isSaving: false,
  saveError: null,
  items: [],

  sortPredicates: {
    quality: function(item, direction) {
      return item.quality ? item.qualityWeight : 0;
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
      name: 'path',
      label: 'Path',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'size',
      label: 'Size',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'dateAdded',
      label: 'Date Added',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'quality',
      label: 'Quality',
      isSortable: true,
      isVisible: true
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
  'issueFiles.sortKey',
  'issueFiles.sortDirection'
];

//
// Actions Types

export const FETCH_ISSUE_FILES = 'issueFiles/fetchIssueFiles';
export const DELETE_ISSUE_FILE = 'issueFiles/deleteIssueFile';
export const DELETE_ISSUE_FILES = 'issueFiles/deleteIssueFiles';
export const UPDATE_ISSUE_FILES = 'issueFiles/updateIssueFiles';
export const SET_ISSUE_FILES_SORT = 'issueFiles/setIssueFilesSort';
export const SET_ISSUE_FILES_TABLE_OPTION = 'issueFiles/setIssueFilesTableOption';
export const CLEAR_ISSUE_FILES = 'issueFiles/clearIssueFiles';

//
// Action Creators

export const fetchIssueFiles = createThunk(FETCH_ISSUE_FILES);
export const deleteIssueFile = createThunk(DELETE_ISSUE_FILE);
export const deleteIssueFiles = createThunk(DELETE_ISSUE_FILES);
export const updateIssueFiles = createThunk(UPDATE_ISSUE_FILES);
export const setIssueFilesSort = createAction(SET_ISSUE_FILES_SORT);
export const setIssueFilesTableOption = createAction(SET_ISSUE_FILES_TABLE_OPTION);
export const clearIssueFiles = createAction(CLEAR_ISSUE_FILES);

//
// Helpers

const deleteIssueFileHelper = createRemoveItemHandler(section, '/comicfile');

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_ISSUE_FILES]: createFetchHandler(section, '/comicfile'),

  [DELETE_ISSUE_FILE]: function(getState, payload, dispatch) {
    const {
      id: issueFileId,
      issueEntity: issueEntity = issueEntities.ISSUES_LIST
    } = payload;

    const issueSection = _.last(issueEntity.split('.'));
    const deletePromise = deleteIssueFileHelper(getState, payload, dispatch);

    deletePromise.done(() => {
      const issues = getState().issues.items;
      const issuesWithRemovedFiles = _.filter(issues, { issueFileId });

      dispatch(batchActions([
        ...issuesWithRemovedFiles.map((issue) => {
          return updateItem({
            section: issueSection,
            ...issue,
            issueFileId: 0,
            hasFile: false
          });
        })
      ]));
    });
  },

  [DELETE_ISSUE_FILES]: function(getState, payload, dispatch) {
    const {
      issueFileIds: issueFileIds
    } = payload;

    dispatch(set({ section, isDeleting: true }));

    const promise = createAjaxRequest({
      url: '/comicfile/bulk',
      method: 'DELETE',
      dataType: 'json',
      data: JSON.stringify({ issueFileIds })
    }).request;

    promise.done(() => {
      const issues = getState().issues.items;
      const issuesWithRemovedFiles = issueFileIds.reduce((acc, issueFileId) => {
        acc.push(..._.filter(issues, { issueFileId }));

        return acc;
      }, []);

      dispatch(batchActions([
        ...issueFileIds.map((id) => {
          return removeItem({ section, id });
        }),

        ...issuesWithRemovedFiles.map((issue) => {
          return updateItem({
            section: 'issues',
            ...issue,
            issueFileId: 0,
            hasFile: false
          });
        }),

        set({
          section,
          isDeleting: false,
          deleteError: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isDeleting: false,
        deleteError: xhr
      }));
    });
  },

  [UPDATE_ISSUE_FILES]: function(getState, payload, dispatch) {
    const {
      issueFileIds,
      quality
    } = payload;

    dispatch(set({ section, isSaving: true }));

    const requestData = {
      issueFileIds
    };

    if (quality) {
      requestData.quality = quality;
    }

    const promise = createAjaxRequest({
      url: '/comicfile/editor',
      method: 'PUT',
      dataType: 'json',
      data: JSON.stringify(requestData)
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...issueFileIds.map((id) => {
          const props = {};

          const issueFile = data.find((file) => file.id === id);

          props.qualityCutoffNotMet = issueFile.qualityCutoffNotMet;

          if (quality) {
            props.quality = quality;
          }

          return updateItem({ section, id, ...props });
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
  }
});

//
// Reducers

export const reducers = createHandleActions({
  [SET_ISSUE_FILES_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_ISSUE_FILES_TABLE_OPTION]: createSetTableOptionReducer(section),

  [CLEAR_ISSUE_FILES]: createClearReducer(section, {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: []
  })

}, defaultState, section);
