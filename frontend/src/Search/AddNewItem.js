import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import TextInput from 'Components/Form/TextInput';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons, kinds } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import AddNewSeriesSearchResultConnector from './Series/AddNewSeriesSearchResultConnector';
import AddNewIssueSearchResultConnector from './Issue/AddNewIssueSearchResultConnector';
import styles from './AddNewItem.css';

function parseDisambiguation(disambiguation) {
  if (!disambiguation) {
    return { publisher: '', issueCount: 0 };
  }

  const parts = disambiguation.split('|');
  return {
    publisher: parts[0] || '',
    issueCount: parts[1] ? parseInt(parts[1]) || 0 : 0
  };
}

class AddNewItem extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      term: props.term || '',
      isFetching: false,
      sortKey: null,
      sortDir: 'asc',
      filterText: ''
    };
  }

  componentDidMount() {
    const term = this.state.term;

    if (term) {
      this.props.onSearchChange(term);
    }
  }

  componentDidUpdate(prevProps) {
    const {
      term,
      isFetching
    } = this.props;

    if (term && term !== prevProps.term) {
      this.setState({
        term,
        isFetching: true
      });
      this.props.onSearchChange(term);
    } else if (isFetching !== prevProps.isFetching) {
      this.setState({
        isFetching
      });
    }
  }

  //
  // Listeners

  onSearchInputChange = ({ value }) => {
    const hasValue = !!value.trim();

    this.setState({ term: value, isFetching: hasValue, filterText: '' }, () => {
      if (hasValue) {
        this.props.onSearchChange(value);
      } else {
        this.props.onClearSearch();
      }
    });
  };

  onClearSearchPress = () => {
    this.setState({ term: '', filterText: '' });
    this.props.onClearSearch();
  };

  onFilterChange = ({ value }) => {
    this.setState({ filterText: value });
  };

  onSortPress = (key) => {
    this.setState((state) => {
      if (state.sortKey === key) {
        return { sortDir: state.sortDir === 'asc' ? 'desc' : 'asc' };
      }

      return { sortKey: key, sortDir: 'asc' };
    });
  };

  //
  // Helpers

  getSortedAndFiltered(seriesItems) {
    const { sortKey, sortDir, filterText } = this.state;
    const filterLower = filterText.toLowerCase().trim();

    let items = seriesItems;

    // Client-side filter
    if (filterLower) {
      items = items.filter((item) => {
        const series = item.series;
        const { publisher } = parseDisambiguation(series.disambiguation);
        const name = (series.seriesName || '').toLowerCase();
        const pub = publisher.toLowerCase();
        const year = String(series.year || '');
        const status = (series.status || '').toLowerCase();

        return name.includes(filterLower) ||
               pub.includes(filterLower) ||
               year.includes(filterLower) ||
               status.includes(filterLower);
      });
    }

    // Sort
    if (sortKey) {
      items = [...items].sort((a, b) => {
        const sa = a.series;
        const sb = b.series;
        const { publisher: pubA, issueCount: countA } = parseDisambiguation(sa.disambiguation);
        const { publisher: pubB, issueCount: countB } = parseDisambiguation(sb.disambiguation);

        let valA, valB;

        switch (sortKey) {
          case 'name':
            valA = (sa.seriesName || '').toLowerCase();
            valB = (sb.seriesName || '').toLowerCase();
            return sortDir === 'asc' ? valA.localeCompare(valB) : valB.localeCompare(valA);
          case 'year':
            valA = sa.year || 0;
            valB = sb.year || 0;
            break;
          case 'publisher':
            valA = pubA.toLowerCase();
            valB = pubB.toLowerCase();
            return sortDir === 'asc' ? valA.localeCompare(valB) : valB.localeCompare(valA);
          case 'issues':
            valA = countA;
            valB = countB;
            break;
          case 'status':
            valA = (sa.status || '').toLowerCase();
            valB = (sb.status || '').toLowerCase();
            return sortDir === 'asc' ? valA.localeCompare(valB) : valB.localeCompare(valA);
          default:
            return 0;
        }

        if (sortDir === 'asc') {
          return valA - valB;
        }

        return valB - valA;
      });
    }

    return items;
  }

  //
  // Render

  renderSortIcon(key) {
    const { sortKey, sortDir } = this.state;

    if (sortKey !== key) {
      return <Icon name={icons.SORT} size={10} className={styles.sortIcon} />;
    }

    return (
      <Icon
        name={sortDir === 'asc' ? icons.SORT_ASCENDING : icons.SORT_DESCENDING}
        size={10}
        className={styles.sortIconActive}
      />
    );
  }

  renderSeriesTable(seriesItems) {
    if (!seriesItems.length) {
      return null;
    }

    const sortedItems = this.getSortedAndFiltered(seriesItems);

    return (
      <div className={styles.tableContainer}>
        <div className={styles.tableControls}>
          <TextInput
            className={styles.filterInput}
            name="filterBox"
            value={this.state.filterText}
            placeholder="Filter results (e.g. DC, 2023, Continuing...)"
            onChange={this.onFilterChange}
          />
          <span className={styles.resultCount}>
            {sortedItems.length} of {seriesItems.length} results
          </span>
        </div>

        <table className={styles.resultsTable} aria-label="Search results">
          <thead>
            <tr>
              <th className={styles.posterHeader} />
              <th className={styles.nameHeader} onClick={() => this.onSortPress('name')}>
                Series {this.renderSortIcon('name')}
              </th>
              <th className={styles.yearHeader} onClick={() => this.onSortPress('year')}>
                Year {this.renderSortIcon('year')}
              </th>
              <th className={styles.publisherHeader} onClick={() => this.onSortPress('publisher')}>
                Publisher {this.renderSortIcon('publisher')}
              </th>
              <th className={styles.issueCountHeader} onClick={() => this.onSortPress('issues')}>
                Issues {this.renderSortIcon('issues')}
              </th>
              <th className={styles.statusHeader} onClick={() => this.onSortPress('status')}>
                Status {this.renderSortIcon('status')}
              </th>
              <th className={styles.actionHeader} />
            </tr>
          </thead>
          <tbody>
            {sortedItems.map((item) => {
              const series = item.series;
              return (
                <AddNewSeriesSearchResultConnector
                  key={item.id}
                  {...series}
                />
              );
            })}
          </tbody>
        </table>

        {sortedItems.length === 0 && seriesItems.length > 0 &&
          <div className={styles.message}>
            No results match your filter. Try a different term.
          </div>
        }
      </div>
    );
  }

  renderIssueResults(issueItems) {
    if (!issueItems.length) {
      return null;
    }

    return (
      <div className={styles.searchResults}>
        {issueItems.map((item) => {
          const issue = item.issue;
          return (
            <AddNewIssueSearchResultConnector
              key={item.id}
              isExistingIssue={'id' in issue && issue.id !== 0}
              isExistingSeries={'id' in issue.series && issue.series.id !== 0}
              {...issue}
            />
          );
        })}
      </div>
    );
  }

  render() {
    const {
      error,
      items,
      hasExistingSeries
    } = this.props;

    const term = this.state.term;
    const isFetching = this.state.isFetching;

    const seriesItems = items.filter((i) => i.series);
    const issueItems = items.filter((i) => i.issue);

    return (
      <PageContent title={translate('AddNewItem')}>
        <PageContentBody>
          <div className={styles.searchContainer}>
            <div className={styles.searchIconContainer}>
              <Icon
                name={icons.SEARCH}
                size={20}
              />
            </div>

            <TextInput
              className={styles.searchInput}
              name="searchBox"
              value={term}
              placeholder="Search by name (e.g. Batman, Ben 10, Spider-Man)"
              autoFocus={true}
              onChange={this.onSearchInputChange}
            />

            <Button
              className={styles.clearLookupButton}
              onPress={this.onClearSearchPress}
            >
              <Icon
                name={icons.REMOVE}
                size={20}
              />
            </Button>
          </div>

          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && !!error ?
              <div className={styles.message}>
                <div className={styles.helpText}>
                  {translate('FailedLoadingSearchResults')}
                </div>

                <Alert kind={kinds.WARNING}>{getErrorMessage(error)}</Alert>

                <div>
                  <Link to="https://github.com/apoxx9/panelarr/wiki/troubleshooting#invalid-response-received-from-metadata-api">
                    {translate('WhySearchesCouldBeFailing')}
                  </Link>
                </div>
              </div> : null
          }

          {
            !isFetching && !error && !!items.length &&
              <div>
                {this.renderSeriesTable(seriesItems)}
                {this.renderIssueResults(issueItems)}
              </div>
          }

          {
            !isFetching && !error && !items.length && !!term &&
              <div className={styles.message}>
                <div className={styles.noResults}>
                  {translate('CouldntFindAnyResultsForTerm', [term])}
                </div>
                <div>
                  {translate('SearchByMetronIdHelpText')}
                </div>
              </div>
          }

          {
            term ?
              null :
              <div className={styles.message}>
                <div className={styles.helpText}>
                  {translate('ItsEasyToAddANewSeriesOrIssueJustStartTypingTheNameOfTheItemYouWantToAdd')}
                </div>
                <div>
                  {translate('SearchByMetronIdHelpText')}
                </div>
              </div>
          }

          {
            !term && !hasExistingSeries ?
              <div className={styles.message}>
                <div className={styles.helpText}>
                  You haven't added any series yet, do you want to add an existing library location (Root Folder) and update?
                </div>
                <div>
                  <Button
                    to="/settings/mediamanagement"
                    kind={kinds.PRIMARY}
                  >
                    {translate('AddRootFolder')}
                  </Button>
                </div>
              </div> :
              null
          }

          <div />
        </PageContentBody>
      </PageContent>
    );
  }
}

AddNewItem.propTypes = {
  term: PropTypes.string,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  hasExistingSeries: PropTypes.bool.isRequired,
  onSearchChange: PropTypes.func.isRequired,
  onClearSearch: PropTypes.func.isRequired
};

export default AddNewItem;
