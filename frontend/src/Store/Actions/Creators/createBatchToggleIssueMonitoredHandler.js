import createAjaxRequest from 'Utilities/createAjaxRequest';
import updateIssues from 'Utilities/Issue/updateIssues';
import getSectionState from 'Utilities/State/getSectionState';

function createBatchToggleIssueMonitoredHandler(section, fetchHandler) {
  return function(getState, payload, dispatch) {
    const {
      issueIds,
      monitored
    } = payload;

    const state = getSectionState(getState(), section, true);

    dispatch(updateIssues(section, state.items, issueIds, {
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: '/issue/monitor',
      method: 'PUT',
      data: JSON.stringify({ issueIds, monitored }),
      dataType: 'json'
    }).request;

    promise.done(() => {
      dispatch(updateIssues(section, state.items, issueIds, {
        isSaving: false,
        monitored
      }));

      dispatch(fetchHandler());
    });

    promise.fail(() => {
      dispatch(updateIssues(section, state.items, issueIds, {
        isSaving: false
      }));
    });
  };
}

export default createBatchToggleIssueMonitoredHandler;
