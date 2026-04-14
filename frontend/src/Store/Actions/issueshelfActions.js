import { createAction } from 'redux-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { filterPredicates, filters } from './seriesActions';
import { set } from './baseActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'issueshelf';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortName',
  secondarySortDirection: sortDirections.ASCENDING,
  selectedFilterKey: 'all',
  filters,
  filterPredicates,

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
      label: 'Format Profile',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'rootFolderPath',
      label: 'Root Folder Path',
      type: filterBuilderTypes.EXACT
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
  'issueshelf.sortKey',
  'issueshelf.sortDirection',
  'issueshelf.selectedFilterKey',
  'issueshelf.customFilters'
];

//
// Actions Types

export const SET_ISSUES_LISTHELF_SORT = 'issueshelf/setIssueshelfSort';
export const SET_ISSUES_LISTHELF_FILTER = 'issueshelf/setIssueshelfFilter';
export const SAVE_ISSUES_LISTHELF = 'issueshelf/saveIssueshelf';

//
// Action Creators

export const setIssueshelfSort = createAction(SET_ISSUES_LISTHELF_SORT);
export const setIssueshelfFilter = createAction(SET_ISSUES_LISTHELF_FILTER);
export const saveIssueshelf = createThunk(SAVE_ISSUES_LISTHELF);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [SAVE_ISSUES_LISTHELF]: function(getState, payload, dispatch) {
    const {
      seriesIds,
      monitored,
      monitor,
      monitorNewItems
    } = payload;

    const series = [];

    seriesIds.forEach((id) => {
      const seriesToUpdate = { id };

      if (payload.hasOwnProperty('monitored')) {
        seriesToUpdate.monitored = monitored;
      }

      series.push(seriesToUpdate);
    });

    dispatch(set({
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/issueshelf',
      method: 'POST',
      data: JSON.stringify({
        series,
        monitoringOptions: { monitor },
        monitorNewItems
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
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

  [SET_ISSUES_LISTHELF_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_ISSUES_LISTHELF_FILTER]: createSetClientSideCollectionFilterReducer(section)

}, defaultState, section);

