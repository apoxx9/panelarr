import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import { set } from './baseActions';
import { filterPredicates, sortPredicates } from './issueActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'seriesDetails';

//
// State

export const defaultState = {
  sortKey: 'releaseDate',
  sortDirection: sortDirections.DESCENDING,
  secondarySortKey: 'releaseDate',
  secondarySortDirection: sortDirections.DESCENDING,

  selectedFilterKey: 'seriesId',

  sortPredicates: {
    ...sortPredicates
  },

  filters: [
    {
      key: 'seriesId',
      label: 'Series',
      filters: [
        {
          key: 'seriesId',
          value: 0
        }
      ]
    }
  ],

  filterPredicates

};

export const persistState = [
  'seriesDetails.sortKey',
  'seriesDetails.sortDirection'
];

//
// Actions Types

export const SET_SERIES_DETAILS_SORT = 'seriesIndex/setSeriesDetailsSort';
export const SET_SERIES_DETAILS_ID = 'seriesIndex/setSeriesDetailsId';

//
// Action Creators

export const setSeriesDetailsSort = createAction(SET_SERIES_DETAILS_SORT);
export const setSeriesDetailsId = createThunk(SET_SERIES_DETAILS_ID);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [SET_SERIES_DETAILS_ID]: function(getState, payload, dispatch) {
    const {
      seriesId
    } = payload;

    dispatch(set({
      section,
      filters: [
        {
          key: 'seriesId',
          label: 'Series',
          filters: [
            {
              key: 'seriesId',
              value: seriesId
            }
          ]
        }
      ]
    }));
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [SET_SERIES_DETAILS_SORT]: createSetClientSideCollectionSortReducer(section)

}, defaultState, section);
