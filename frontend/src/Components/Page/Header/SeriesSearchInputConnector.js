import { push } from 'connected-react-router';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllSeriesSelector from 'Store/Selectors/createAllSeriesSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import SeriesSearchInput from './SeriesSearchInput';

function createCleanSeriesSelector() {
  return createSelector(
    createAllSeriesSelector(),
    createTagsSelector(),
    (allSeries, allTags) => {
      return allSeries.map((series) => {
        const {
          seriesName,
          sortName,
          images,
          titleSlug,
          tags = []
        } = series;

        return {
          type: 'series',
          name: seriesName,
          sortName,
          titleSlug,
          images,
          firstCharacter: seriesName.charAt(0).toLowerCase(),
          tags: tags.reduce((acc, id) => {
            const matchingTag = allTags.find((tag) => tag.id === id);

            if (matchingTag) {
              acc.push(matchingTag);
            }

            return acc;
          }, [])
        };
      });
    }
  );
}

function createCleanIssueSelector() {
  return createSelector(
    (state) => state.issues.items,
    (allIssues) => {
      return allIssues.map((issue) => {
        const {
          title,
          images,
          titleSlug
        } = issue;

        return {
          type: 'issue',
          name: title,
          sortName: title,
          titleSlug,
          images,
          firstCharacter: title.charAt(0).toLowerCase(),
          tags: []
        };
      });
    }
  );
}

function createMapStateToProps() {
  return createDeepEqualSelector(
    createCleanSeriesSelector(),
    createCleanIssueSelector(),
    (series, issues) => {
      const items = [
        ...series,
        ...issues
      ];
      return {
        items
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGoToSeries(titleSlug) {
      dispatch(push(`${window.Panelarr.urlBase}/series/${titleSlug}`));
    },

    onGoToIssue(titleSlug) {
      dispatch(push(`${window.Panelarr.urlBase}/issue/${titleSlug}`));
    },

    onGoToAddNewSeries(query) {
      dispatch(push(`${window.Panelarr.urlBase}/add/search?term=${encodeURIComponent(query)}`));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(SeriesSearchInput);
