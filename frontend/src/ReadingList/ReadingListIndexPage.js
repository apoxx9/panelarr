import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import { icons } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import styles from './ReadingListIndexPage.css';

// Reading lists (docs/story-arcs.md): user-curated ordered issue lists —
// story arcs, events, reading orders — living on top of the library.
// Sources: ComicVine arc search, or a community .cbl file (the format
// Kavita/Komga/Mylar exchange). Imports never modify the library.
class ReadingListIndexPage extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isFetching: true,
      error: null,
      lists: [],
      showSearch: false,
      searchQuery: '',
      isSearching: false,
      searchResults: null,
      addingId: null,
      report: null
    };

    this._fileInputRef = React.createRef();
  }

  componentDidMount() {
    this.fetchLists();
  }

  fetchLists = () => {
    const { request } = createAjaxRequest({ url: '/readinglist', dataType: 'json' });

    request.done((lists) => this.setState({ isFetching: false, lists }));
    request.fail(() => this.setState({ isFetching: false, error: translate('ReadingListsLoadError') }));
  };

  //
  // Add from ComicVine

  onToggleSearchPress = () => {
    this.setState((prev) => ({ showSearch: !prev.showSearch, report: null }));
  };

  onSearchQueryChange = (event) => {
    this.setState({ searchQuery: event.target.value });
  };

  onSearchKeyDown = (event) => {
    if (event.key === 'Enter') {
      this.onSearchPress();
    }
  };

  onSearchPress = () => {
    const { searchQuery } = this.state;

    if (!searchQuery.trim()) {
      return;
    }

    this.setState({ isSearching: true, searchResults: null });

    const { request } = createAjaxRequest({
      url: `/readinglist/search?query=${encodeURIComponent(searchQuery)}`,
      dataType: 'json'
    });

    request.done((searchResults) => this.setState({ isSearching: false, searchResults }));
    request.fail(() => this.setState({ isSearching: false, error: translate('ReadingListSearchError') }));
  };

  onAddPress = (cvStoryArcId) => {
    this.setState({ addingId: cvStoryArcId });

    const { request } = createAjaxRequest({
      url: '/readinglist',
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({ cvStoryArcId, type: 'arc' })
    });

    request.done((report) => {
      this.setState({ addingId: null, report, showSearch: false, searchResults: null, searchQuery: '' });
      this.fetchLists();
    });

    request.fail((xhr) => {
      const message = xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.content);

      this.setState({ addingId: null, error: message || translate('ReadingListAddError') });
    });
  };

  //
  // CBL import

  onImportPress = () => {
    this._fileInputRef.current.click();
  };

  onFileSelected = (event) => {
    const file = event.target.files[0];

    event.target.value = null;

    if (!file) {
      return;
    }

    const reader = new FileReader();

    reader.onload = () => {
      const { request } = createAjaxRequest({
        url: '/readinglist/import',
        method: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify({ cbl: reader.result, type: 'readingOrder' })
      });

      request.done((report) => {
        this.setState({ report, error: null });
        this.fetchLists();
      });

      request.fail((xhr) => {
        const message = xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.content);

        this.setState({ error: message || translate('ReadingListImportError') });
      });
    };

    reader.readAsText(file);
  };

  //
  // Render

  render() {
    const {
      isFetching,
      error,
      lists,
      showSearch,
      searchQuery,
      isSearching,
      searchResults,
      addingId,
      report
    } = this.state;

    return (
      <PageContent title={translate('ReadingLists')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('AddFromComicVine')}
              iconName={icons.SEARCH}
              onPress={this.onToggleSearchPress}
            />
            <PageToolbarButton
              label={translate('ImportCbl')}
              iconName={icons.FILEIMPORT}
              onPress={this.onImportPress}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody>
          <input
            ref={this._fileInputRef}
            type="file"
            accept=".cbl,.xml"
            className={styles.hiddenFileInput}
            onChange={this.onFileSelected}
          />

          {
            error &&
              <div className={styles.error}>{error}</div>
          }

          {
            report &&
              <div className={styles.report}>
                <div className={styles.reportTitle}>
                  {translate('ReadingListImported', { name: report.name, count: report.slotCount })}
                  {report.skippedCollectedEditions ? ` — ${translate('SkippedCollectedEditions', { count: report.skippedCollectedEditions })}` : ''}
                </div>
                {
                  !!(report.unresolved && report.unresolved.length) &&
                    <ul className={styles.unresolvedList}>
                      {report.unresolved.map((u, i) => <li key={i}>{u}</li>)}
                    </ul>
                }
              </div>
          }

          {
            showSearch &&
              <div className={styles.searchPanel}>
                <div className={styles.searchRow}>
                  <input
                    className={styles.searchInput}
                    type="text"
                    value={searchQuery}
                    placeholder={translate('SearchComicVineArcs')}
                    onChange={this.onSearchQueryChange}
                    onKeyDown={this.onSearchKeyDown}
                  />
                  <Button
                    isDisabled={isSearching}
                    onPress={this.onSearchPress}
                  >
                    {translate('Search')}
                  </Button>
                </div>

                {isSearching && <LoadingIndicator />}

                {
                  searchResults && !searchResults.length &&
                    <div className={styles.empty}>{translate('NoResultsFound')}</div>
                }

                {
                  !!(searchResults && searchResults.length) &&
                    <table className={styles.table}>
                      <tbody>
                        {
                          searchResults.map((r) => {
                            return (
                              <tr key={r.cvStoryArcId}>
                                <td>{r.name}</td>
                                <td>{r.publisher || '-'}</td>
                                <td className={styles.deck}>{r.deck || ''}</td>
                                <td className={styles.actionsCell}>
                                  <Button
                                    isDisabled={addingId === r.cvStoryArcId}
                                    onPress={() => this.onAddPress(r.cvStoryArcId)}
                                  >
                                    {addingId === r.cvStoryArcId ? translate('Adding') : translate('Add')}
                                  </Button>
                                </td>
                              </tr>
                            );
                          })
                        }
                      </tbody>
                    </table>
                }
              </div>
          }

          {isFetching && <LoadingIndicator />}

          {
            !isFetching && !lists.length &&
              <div className={styles.empty}>
                {translate('NoReadingLists')}
              </div>
          }

          {
            !isFetching && !!lists.length &&
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{translate('Name')}</th>
                    <th>{translate('Type')}</th>
                    <th>{translate('Publisher')}</th>
                    <th>{translate('Coverage')}</th>
                  </tr>
                </thead>
                <tbody>
                  {
                    lists.map((list) => {
                      const notInLibrary = list.slotCount - list.resolvedCount;

                      return (
                        <tr key={list.id}>
                          <td>
                            <Link to={`/readinglists/${list.id}`}>
                              {list.name}
                            </Link>
                          </td>
                          <td>{translate(`ReadingListType_${list.type}`)}</td>
                          <td>{list.publisher || '-'}</td>
                          <td>
                            <span className={list.haveCount === list.slotCount ? styles.complete : styles.incomplete}>
                              {translate('ReadingListCoverage', { have: list.haveCount, total: list.slotCount })}
                            </span>
                            {notInLibrary ? ` · ${translate('NotInLibraryCount', { count: notInLibrary })}` : ''}
                          </td>
                        </tr>
                      );
                    })
                  }
                </tbody>
              </table>
          }
        </PageContentBody>
      </PageContent>
    );
  }
}

ReadingListIndexPage.propTypes = {
  history: PropTypes.object
};

export default ReadingListIndexPage;
