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
import { icons } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import styles from './ReadingListDetailPage.css';

// Detail view of one reading list: the ordered slots with live statuses.
// "Not in library" slots can add their series explicitly (root folder +
// quality profile picked here) — the only path from a list into the library.
class ReadingListDetailPage extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isFetching: true,
      error: null,
      list: null,
      showAddSeries: false,
      rootFolders: [],
      qualityProfiles: [],
      selectedRootFolderId: null,
      qualityProfileId: null,
      addingForeignId: null,
      searchQueued: false,
      isPushing: false,
      pushResults: null,
      editingSlotId: null,
      allSeries: null,
      editorSeriesId: null,
      editorIssues: [],
      editorIssueId: null,
      isRemapping: false
    };
  }

  componentDidMount() {
    this.fetchList();

    const rootFoldersRequest = createAjaxRequest({ url: '/rootfolder', dataType: 'json' }).request;

    rootFoldersRequest.done((rootFolders) => {
      const first = rootFolders[0];

      this.setState({
        rootFolders,
        selectedRootFolderId: first ? first.id : null,
        qualityProfileId: first ? first.defaultQualityProfileId : null
      });
    });

    const profilesRequest = createAjaxRequest({ url: '/qualityprofile', dataType: 'json' }).request;

    profilesRequest.done((qualityProfiles) => this.setState({ qualityProfiles }));
  }

  get listId() {
    return parseInt(this.props.match.params.id);
  }

  fetchList = () => {
    const { request } = createAjaxRequest({
      url: `/readinglist/${this.listId}`,
      dataType: 'json'
    });

    request.done((list) => this.setState({ isFetching: false, list }));
    request.fail(() => this.setState({ isFetching: false, error: translate('ReadingListsLoadError') }));
  };

  //
  // Actions

  onSearchMissingPress = () => {
    const missingIds = this.state.list.slots
      .filter((s) => s.status === 'missing')
      .map((s) => s.issueId);

    if (!missingIds.length) {
      return;
    }

    const { request } = createAjaxRequest({
      url: '/command',
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({ name: 'IssueSearch', issueIds: missingIds })
    });

    request.done(() => this.setState({ searchQueued: true }));
  };

  onExportPress = () => {
    const { request } = createAjaxRequest({
      url: `/readinglist/${this.listId}/export`,
      dataType: 'text'
    });

    request.done((xml) => {
      const blob = new Blob([xml], { type: 'application/xml' });
      const url = window.URL.createObjectURL(blob);
      const anchor = document.createElement('a');

      anchor.href = url;
      anchor.download = `${this.state.list.name}.cbl`;
      anchor.click();
      window.URL.revokeObjectURL(url);
    });
  };

  onEditSlotPress = (slot) => {
    if (this.state.editingSlotId === slot.id) {
      this.setState({ editingSlotId: null });
      return;
    }

    this.setState({
      editingSlotId: slot.id,
      editorSeriesId: slot.seriesId || null,
      editorIssues: [],
      editorIssueId: null
    });

    if (!this.state.allSeries) {
      const { request } = createAjaxRequest({ url: '/series', dataType: 'json' });

      request.done((allSeries) => {
        allSeries.sort((a, b) => a.seriesName.localeCompare(b.seriesName));
        this.setState({ allSeries });
      });
    }

    if (slot.seriesId) {
      this.fetchEditorIssues(slot.seriesId);
    }
  };

  fetchEditorIssues = (seriesId) => {
    const { request } = createAjaxRequest({ url: `/issue?seriesId=${seriesId}`, dataType: 'json' });

    request.done((editorIssues) => {
      editorIssues.sort((a, b) => a.issueNumber.localeCompare(b.issueNumber, undefined, { numeric: true }));
      this.setState({ editorIssues });
    });
  };

  onEditorSeriesChange = (event) => {
    const editorSeriesId = parseInt(event.target.value);

    this.setState({ editorSeriesId, editorIssues: [], editorIssueId: null });
    this.fetchEditorIssues(editorSeriesId);
  };

  onEditorIssueChange = (event) => {
    this.setState({ editorIssueId: parseInt(event.target.value) });
  };

  onRemapSavePress = () => {
    const { editingSlotId, editorIssueId } = this.state;

    if (!editorIssueId) {
      return;
    }

    this.setState({ isRemapping: true });

    const { request } = createAjaxRequest({
      url: `/readinglist/${this.listId}/slots/${editingSlotId}`,
      method: 'PUT',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({ issueId: editorIssueId })
    });

    request.done(() => {
      this.setState({ isRemapping: false, editingSlotId: null });
      this.fetchList();
    });

    request.fail(() => this.setState({ isRemapping: false, error: translate('ReadingListRelinkError') }));
  };

  onPushPress = () => {
    this.setState({ isPushing: true, pushResults: null });

    const { request } = createAjaxRequest({
      url: `/readinglist/${this.listId}/push`,
      method: 'POST',
      dataType: 'json'
    });

    request.done((pushResults) => this.setState({ isPushing: false, pushResults }));
    request.fail(() => this.setState({ isPushing: false, error: translate('ReadingListPushError') }));
  };

  onDeletePress = () => {
    // eslint-disable-next-line no-alert
    if (!window.confirm(translate('DeleteReadingListConfirm', { name: this.state.list.name }))) {
      return;
    }

    const { request } = createAjaxRequest({
      url: `/readinglist/${this.listId}`,
      method: 'DELETE'
    });

    request.done(() => this.props.history.push('/readinglists'));
  };

  onToggleAddSeriesPress = () => {
    this.setState((prev) => ({ showAddSeries: !prev.showAddSeries }));
  };

  onRootFolderChange = (event) => {
    this.setState({ selectedRootFolderId: parseInt(event.target.value) });
  };

  onQualityProfileChange = (event) => {
    this.setState({ qualityProfileId: parseInt(event.target.value) });
  };

  onAddSeriesPress = (foreignSeriesId) => {
    const { rootFolders, selectedRootFolderId, qualityProfileId } = this.state;
    const rootFolder = rootFolders.find((r) => r.id === selectedRootFolderId);

    if (!rootFolder) {
      return;
    }

    this.setState({ addingForeignId: foreignSeriesId });

    const { request } = createAjaxRequest({
      url: `/readinglist/${this.listId}/addseries`,
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({
        foreignSeriesId,
        rootFolderPath: rootFolder.path,
        qualityProfileId,
        monitored: true
      })
    });

    request.done(() => {
      this.setState({ addingForeignId: null });
      this.fetchList();
    });

    request.fail((xhr) => {
      const message = xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.content);

      this.setState({ addingForeignId: null, error: message || translate('ReadingListAddError') });
    });
  };

  //
  // Render

  renderAddSeriesPanel(missingSeries) {
    const {
      rootFolders,
      qualityProfiles,
      selectedRootFolderId,
      qualityProfileId,
      addingForeignId
    } = this.state;

    return (
      <div className={styles.addSeriesPanel}>
        <div className={styles.controls}>
          <label className={styles.control}>
            {translate('RootFolder')}
            <select
              className={styles.select}
              value={selectedRootFolderId || ''}
              onChange={this.onRootFolderChange}
            >
              {rootFolders.map((r) => <option key={r.id} value={r.id}>{r.path}</option>)}
            </select>
          </label>

          <label className={styles.control}>
            {translate('QualityProfile')}
            <select
              className={styles.select}
              value={qualityProfileId || ''}
              onChange={this.onQualityProfileChange}
            >
              {qualityProfiles.map((q) => <option key={q.id} value={q.id}>{q.name}</option>)}
            </select>
          </label>
        </div>

        <table className={styles.table}>
          <tbody>
            {
              missingSeries.map((s) => {
                return (
                  <tr key={s.foreignSeriesId}>
                    <td>{s.seriesName}</td>
                    <td>{translate('IssueCountInList', { count: s.count })}</td>
                    <td className={styles.actionsCell}>
                      <Button
                        isDisabled={addingForeignId === s.foreignSeriesId}
                        onPress={() => this.onAddSeriesPress(s.foreignSeriesId)}
                      >
                        {addingForeignId === s.foreignSeriesId ? translate('Adding') : translate('Add')}
                      </Button>
                    </td>
                  </tr>
                );
              })
            }
          </tbody>
        </table>
      </div>
    );
  }

  renderSlotEditor() {
    const { allSeries, editorSeriesId, editorIssues, editorIssueId, isRemapping } = this.state;

    return (
      <tr className={styles.slotEditorRow}>
        <td />
        <td colSpan={4}>
          <div className={styles.controls}>
            <label className={styles.control}>
              {translate('Series')}
              <select
                className={styles.select}
                value={editorSeriesId || ''}
                onChange={this.onEditorSeriesChange}
              >
                <option value="" disabled={true}>...</option>
                {(allSeries || []).map((s) => <option key={s.id} value={s.id}>{s.seriesName}</option>)}
              </select>
            </label>

            <label className={styles.control}>
              {translate('Issue')}
              <select
                className={styles.select}
                value={editorIssueId || ''}
                onChange={this.onEditorIssueChange}
              >
                <option value="" disabled={true}>...</option>
                {editorIssues.map((i) => <option key={i.id} value={i.id}>{`#${i.issueNumber}${i.title ? ` — ${i.title}` : ''}`}</option>)}
              </select>
            </label>

            <Button
              isDisabled={!editorIssueId || isRemapping}
              onPress={this.onRemapSavePress}
            >
              {translate('Save')}
            </Button>
          </div>
        </td>
        <td />
      </tr>
    );
  }

  renderPushResults(pushResults) {
    if (!pushResults.length) {
      return (
        <div className={styles.pushPanel}>
          {translate('ReadingListPushNoConnections')}
        </div>
      );
    }

    return (
      <div className={styles.pushPanel}>
        {
          pushResults.map((result) => {
            return (
              <div key={`${result.reader}-${result.connectionName}`} className={styles.pushRow}>
                <span className={result.success ? styles.pushSuccess : styles.pushFailure}>
                  {result.connectionName} ({result.reader})
                </span>
                {' — '}
                {
                  result.success ?
                    translate(result.updated ? 'ReadingListPushUpdated' : 'ReadingListPushCreated', { count: result.matchedCount }) :
                    translate('ReadingListPushFailed', { message: result.errorMessage })
                }

                {
                  result.success && !!result.unmatched.length &&
                    <ul className={styles.unmatchedList}>
                      {result.unmatched.map((entry) => <li key={entry}>{entry}</li>)}
                    </ul>
                }
              </div>
            );
          })
        }
      </div>
    );
  }

  render() {
    const { isFetching, error, list, showAddSeries, searchQueued, isPushing, pushResults } = this.state;

    const missingCount = list ? list.slots.filter((s) => s.status === 'missing').length : 0;

    const missingSeriesMap = new Map();

    if (list) {
      list.slots
        .filter((s) => s.status === 'notInLibrary' && s.foreignSeriesId)
        .forEach((s) => {
          const existing = missingSeriesMap.get(s.foreignSeriesId);

          if (existing) {
            existing.count++;
          } else {
            missingSeriesMap.set(s.foreignSeriesId, { foreignSeriesId: s.foreignSeriesId, seriesName: s.seriesName, count: 1 });
          }
        });
    }

    const missingSeries = [...missingSeriesMap.values()];

    return (
      <PageContent title={list ? list.name : translate('ReadingLists')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={searchQueued ? translate('SearchQueued') : translate('SearchMissingCount', { count: missingCount })}
              iconName={icons.SEARCH}
              isDisabled={missingCount === 0 || searchQueued}
              onPress={this.onSearchMissingPress}
            />
            <PageToolbarButton
              label={translate('AddMissingSeriesCount', { count: missingSeries.length })}
              iconName={icons.ADD_MISSING_SERIES_LIST}
              isDisabled={missingSeries.length === 0}
              onPress={this.onToggleAddSeriesPress}
            />
            <PageToolbarButton
              label={translate('ExportCbl')}
              iconName={icons.EXPORT}
              isDisabled={!list}
              onPress={this.onExportPress}
            />
            <PageToolbarButton
              label={isPushing ? translate('ReadingListPushing') : translate('PushToReaders')}
              iconName={icons.UPLOAD}
              isDisabled={!list || isPushing}
              onPress={this.onPushPress}
            />
            <PageToolbarButton
              label={translate('Delete')}
              iconName={icons.DELETE}
              isDisabled={!list}
              onPress={this.onDeletePress}
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
            list &&
              <div>
                <div className={styles.header}>
                  <span className={styles.typeBadge}>{translate(`ReadingListType_${list.type}`)}</span>
                  {list.publisher ? <span className={styles.publisher}>{list.publisher}</span> : null}
                  <span className={styles.coverage}>
                    {translate('ReadingListCoverage', { have: list.haveCount, total: list.slotCount })}
                  </span>
                </div>

                {list.description ? <div className={styles.description}>{list.description}</div> : null}

                {pushResults && this.renderPushResults(pushResults)}

                {showAddSeries && !!missingSeries.length && this.renderAddSeriesPanel(missingSeries)}

                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th className={styles.positionCell}>#</th>
                      <th>{translate('Series')}</th>
                      <th>{translate('Issue')}</th>
                      <th>{translate('Year')}</th>
                      <th>{translate('Status')}</th>
                      <th />
                    </tr>
                  </thead>
                  <tbody>
                    {
                      list.slots.map((slot) => {
                        return (
                          <React.Fragment key={slot.id}>
                            <tr>
                              <td className={styles.positionCell}>{slot.position}</td>
                              <td>
                                {
                                  slot.seriesTitleSlug ?
                                    <Link to={`/series/${slot.seriesTitleSlug}`}>{slot.seriesName}</Link> :
                                    slot.seriesName
                                }
                              </td>
                              <td>#{slot.issueNumber || '?'}</td>
                              <td>{slot.year || '-'}</td>
                              <td>
                                <span className={styles[slot.status] || styles.notInLibrary}>
                                  {translate(`ReadingListStatus_${slot.status}`)}
                                </span>
                              </td>
                              <td className={styles.actionsCell}>
                                {
                                  slot.status === 'missing' &&
                                    <IconButton
                                      name={icons.SEARCH}
                                      title={translate('Search')}
                                      onPress={() => {
                                        createAjaxRequest({
                                          url: '/command',
                                          method: 'POST',
                                          contentType: 'application/json',
                                          dataType: 'json',
                                          data: JSON.stringify({ name: 'IssueSearch', issueIds: [slot.issueId] })
                                        });
                                      }}
                                    />
                                }
                                <IconButton
                                  name={icons.EDIT}
                                  title={translate('ReadingListRelink')}
                                  onPress={() => this.onEditSlotPress(slot)}
                                />
                              </td>
                            </tr>
                            {this.state.editingSlotId === slot.id && this.renderSlotEditor()}
                          </React.Fragment>
                        );
                      })
                    }
                  </tbody>
                </table>
              </div>
          }
        </PageContentBody>
      </PageContent>
    );
  }
}

ReadingListDetailPage.propTypes = {
  match: PropTypes.object.isRequired,
  history: PropTypes.object.isRequired
};

export default ReadingListDetailPage;
