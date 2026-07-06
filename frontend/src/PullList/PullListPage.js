import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import { align, icons } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import styles from './PullListPage.css';

const WEEKS_AHEAD = 4;
const WEEKS_BACK = 1;

// The weekly pull list (feature-landscape #1): comics are a weekly medium,
// so this is the calendar re-cut the way a shop reader thinks about it —
// Wednesday-anchored (new-comic-book-day) week sections from last week to
// four weeks out, with per-issue status and search.
function wednesdayOf(date) {
  const m = moment(date).startOf('day');

  return m.subtract((m.day() - 3 + 7) % 7, 'days');
}

function issueStatus(issue) {
  const hasFile = issue.statistics && issue.statistics.issueFileCount > 0;

  if (hasFile) {
    return 'have';
  }

  if (issue.grabbed) {
    return 'grabbed';
  }

  if (moment(issue.releaseDate).isAfter(moment())) {
    return 'unreleased';
  }

  return issue.monitored ? 'missing' : 'unmonitored';
}

class PullListPage extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      anchor: wednesdayOf(new Date()),
      isFetching: true,
      error: null,
      issues: [],
      showUnmonitored: false,
      searchQueued: {}
    };
  }

  componentDidMount() {
    this.fetchIssues();
  }

  fetchIssues = () => {
    const { anchor } = this.state;
    const start = moment(anchor).subtract(WEEKS_BACK * 7, 'days');
    const end = moment(anchor).add((WEEKS_AHEAD + 1) * 7, 'days');

    this.setState({ isFetching: true, error: null });

    const { request } = createAjaxRequest({
      url: `/calendar?start=${start.format('YYYY-MM-DD')}&end=${end.format('YYYY-MM-DD')}&unmonitored=true&includeSeries=true`,
      dataType: 'json'
    });

    request.done((issues) => this.setState({ isFetching: false, issues }));
    request.fail(() => this.setState({ isFetching: false, error: translate('PullListLoadError') }));
  };

  onPreviousWeekPress = () => {
    this.setState((prev) => ({ anchor: moment(prev.anchor).subtract(7, 'days') }), this.fetchIssues);
  };

  onNextWeekPress = () => {
    this.setState((prev) => ({ anchor: moment(prev.anchor).add(7, 'days') }), this.fetchIssues);
  };

  onTodayPress = () => {
    this.setState({ anchor: wednesdayOf(new Date()) }, this.fetchIssues);
  };

  onToggleUnmonitored = () => {
    this.setState((prev) => ({ showUnmonitored: !prev.showUnmonitored }));
  };

  onSearchPress = (issueIds, key) => {
    const { request } = createAjaxRequest({
      url: '/command',
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({ name: 'IssueSearch', issueIds })
    });

    request.done(() => {
      this.setState((prev) => ({ searchQueued: { ...prev.searchQueued, [key]: true } }));
    });
  };

  //
  // Render

  renderIssueRow(issue) {
    const { searchQueued } = this.state;
    const status = issueStatus(issue);
    const series = issue.series || {};
    const searchable = status === 'missing' || status === 'grabbed';

    return (
      <tr key={issue.id}>
        <td className={styles.seriesCell}>
          <Link to={`/series/${series.titleSlug}`}>
            {series.seriesName}{series.year ? ` (${series.year})` : ''}
          </Link>
        </td>
        <td>#{issue.issueNumber}</td>
        <td className={styles.titleCell}>{issue.title || '-'}</td>
        <td>{moment(issue.releaseDate).format('ddd, MMM D')}</td>
        <td>
          <span className={styles[status]}>
            {translate(`PullListStatus_${status}`)}
          </span>
        </td>
        <td className={styles.actionsCell}>
          {
            searchable &&
              <IconButton
                name={searchQueued[`issue-${issue.id}`] ? icons.CHECK : icons.SEARCH}
                title={translate('Search')}
                onPress={() => this.onSearchPress([issue.id], `issue-${issue.id}`)}
              />
          }
        </td>
      </tr>
    );
  }

  renderWeek(weekStart, issues) {
    const { searchQueued, showUnmonitored } = this.state;

    const visible = issues
      .filter((i) => showUnmonitored || i.monitored)
      .sort((a, b) => {
        const an = (a.series && a.series.sortName) || '';
        const bn = (b.series && b.series.sortName) || '';

        return an.localeCompare(bn) || `${a.issueNumber}`.localeCompare(`${b.issueNumber}`, undefined, { numeric: true });
      });

    const isThisWeek = weekStart.isSame(wednesdayOf(new Date()), 'day');
    const missingIds = visible.filter((i) => issueStatus(i) === 'missing').map((i) => i.id);
    const weekKey = `week-${weekStart.format('YYYY-MM-DD')}`;

    return (
      <div key={weekKey} className={styles.week}>
        <div className={styles.weekHeader}>
          <span className={isThisWeek ? styles.thisWeek : styles.weekTitle}>
            {translate('PullListWeekOf', { date: weekStart.format('ddd, MMM D') })}
            {isThisWeek ? ` — ${translate('ThisWeek')}` : ''}
          </span>

          {
            !!missingIds.length &&
              <Button
                className={styles.searchWeek}
                isDisabled={!!searchQueued[weekKey]}
                onPress={() => this.onSearchPress(missingIds, weekKey)}
              >
                {
                  searchQueued[weekKey] ?
                    translate('SearchQueued') :
                    translate('SearchMissingCount', { count: missingIds.length })
                }
              </Button>
          }
        </div>

        {
          visible.length ?
            <table className={styles.table}>
              <tbody>
                {visible.map((issue) => this.renderIssueRow(issue))}
              </tbody>
            </table> :
            <div className={styles.emptyWeek}>
              {translate('PullListEmptyWeek')}
            </div>
        }
      </div>
    );
  }

  render() {
    const { anchor, isFetching, error, issues, showUnmonitored } = this.state;

    const weeks = [];

    for (let offset = -WEEKS_BACK; offset <= WEEKS_AHEAD; offset++) {
      const weekStart = moment(anchor).add(offset * 7, 'days');
      const weekEnd = moment(weekStart).add(7, 'days');

      const weekIssues = issues.filter((i) => {
        const releaseDate = moment(i.releaseDate);

        return releaseDate.isSameOrAfter(weekStart) && releaseDate.isBefore(weekEnd);
      });

      weeks.push({ weekStart, weekIssues });
    }

    return (
      <PageContent title={translate('PullList')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('PreviousWeek')}
              iconName={icons.ARROW_LEFT}
              onPress={this.onPreviousWeekPress}
            />
            <PageToolbarButton
              label={translate('Today')}
              iconName={icons.CALENDAR_O}
              onPress={this.onTodayPress}
            />
            <PageToolbarButton
              label={translate('NextWeek')}
              iconName={icons.ARROW_RIGHT}
              onPress={this.onNextWeekPress}
            />
          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <PageToolbarButton
              label={translate('Unmonitored')}
              iconName={showUnmonitored ? icons.MONITORED : icons.UNMONITORED}
              onPress={this.onToggleUnmonitored}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody>
          {isFetching && <LoadingIndicator />}

          {
            error &&
              <div className={styles.error}>{error}</div>
          }

          {
            !isFetching && !error &&
              weeks.map(({ weekStart, weekIssues }) => this.renderWeek(weekStart, weekIssues))
          }
        </PageContentBody>
      </PageContent>
    );
  }
}

PullListPage.propTypes = {
  history: PropTypes.object
};

export default PullListPage;
