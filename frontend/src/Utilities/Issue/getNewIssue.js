import getNewSeries from 'Utilities/Series/getNewSeries';

function getNewIssue(issue, payload) {
  const {
    searchForNewIssue = false
  } = payload;

  if (!('id' in issue.series) || issue.series.id === 0) {
    getNewSeries(issue.series, payload);

    if (payload.monitor === 'specificIssue') {
      delete issue.series.addOptions.monitor;
      issue.series.addOptions.issuesToMonitor = [issue.foreignIssueId];
    }
  }

  issue.addOptions = {
    searchForNewIssue
  };
  issue.monitored = true;

  return issue;
}

export default getNewIssue;
