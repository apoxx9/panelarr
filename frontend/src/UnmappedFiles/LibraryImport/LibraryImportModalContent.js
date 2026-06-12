import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds, sizes } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import styles from './LibraryImportModalContent.css';

// Review screen for the tagged-library-import flow: proposals come from the
// folder metadata Mylar leaves behind (cvinfo / tagged ids / name search).
// Exact matches are pre-checked; probable ones need a deliberate tick.
class LibraryImportModalContent extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isFetching: true,
      error: null,
      rootFolders: [],
      qualityProfiles: [],
      selectedRootFolderId: null,
      proposals: null,
      checkedState: {},
      qualityProfileId: null,
      monitored: true,
      isImporting: false,
      importQueued: false
    };
  }

  componentDidMount() {
    const rootFoldersRequest = createAjaxRequest({ url: '/rootfolder', dataType: 'json' }).request;
    const profilesRequest = createAjaxRequest({ url: '/qualityprofile', dataType: 'json' }).request;

    rootFoldersRequest.done((rootFolders) => {
      profilesRequest.done((qualityProfiles) => {
        const firstRootFolder = rootFolders[0];

        this.setState({
          isFetching: false,
          rootFolders,
          qualityProfiles,
          selectedRootFolderId: firstRootFolder ? firstRootFolder.id : null,
          qualityProfileId: firstRootFolder ? firstRootFolder.defaultQualityProfileId : null
        }, () => {
          if (firstRootFolder) {
            this.fetchProposals(firstRootFolder.id);
          }
        });
      });

      profilesRequest.fail(() => this.setState({ isFetching: false, error: translate('LibraryImportLoadError') }));
    });

    rootFoldersRequest.fail(() => this.setState({ isFetching: false, error: translate('LibraryImportLoadError') }));
  }

  fetchProposals = (rootFolderId) => {
    this.setState({ isFetching: true, error: null, proposals: null });

    const { request } = createAjaxRequest({
      url: `/libraryimport/proposal?rootFolderId=${rootFolderId}`,
      dataType: 'json'
    });

    request.done((proposals) => {
      const checkedState = {};

      proposals.forEach((p) => {
        checkedState[p.id] = p.confidence === 'exact' && !p.existingSeriesId;
      });

      this.setState({ isFetching: false, proposals, checkedState });
    });

    request.fail(() => this.setState({ isFetching: false, error: translate('LibraryImportScanError') }));
  };

  onRootFolderChange = (event) => {
    const rootFolderId = parseInt(event.target.value);
    const rootFolder = this.state.rootFolders.find((r) => r.id === rootFolderId);

    this.setState({
      selectedRootFolderId: rootFolderId,
      qualityProfileId: rootFolder ? rootFolder.defaultQualityProfileId : this.state.qualityProfileId
    });

    this.fetchProposals(rootFolderId);
  };

  onQualityProfileChange = (event) => {
    this.setState({ qualityProfileId: parseInt(event.target.value) });
  };

  onMonitoredChange = (event) => {
    this.setState({ monitored: event.target.checked });
  };

  onToggleProposal = (id) => {
    this.setState((prev) => ({
      checkedState: {
        ...prev.checkedState,
        [id]: !prev.checkedState[id]
      }
    }));
  };

  onImportPress = () => {
    const { proposals, checkedState, qualityProfileId, monitored } = this.state;

    const series = proposals
      .filter((p) => checkedState[p.id] && !p.existingSeriesId)
      .map((p) => ({ foreignSeriesId: p.foreignSeriesId, folder: p.folder }));

    if (!series.length) {
      return;
    }

    this.setState({ isImporting: true });

    const { request } = createAjaxRequest({
      url: '/command',
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({
        name: 'LibraryImport',
        series,
        qualityProfileId,
        monitored,
        monitorNewItems: monitored ? 'all' : 'none'
      })
    });

    request.done(() => this.setState({ isImporting: false, importQueued: true }));
    request.fail(() => this.setState({ isImporting: false, error: translate('LibraryImportQueueError') }));
  };

  render() {
    const { onModalClose } = this.props;
    const {
      isFetching,
      error,
      rootFolders,
      qualityProfiles,
      selectedRootFolderId,
      proposals,
      checkedState,
      qualityProfileId,
      monitored,
      isImporting,
      importQueued
    } = this.state;

    const selectedCount = proposals ?
      proposals.filter((p) => checkedState[p.id] && !p.existingSeriesId).length :
      0;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('ScanForNewSeries')}
        </ModalHeader>

        <ModalBody>
          {
            importQueued ?
              <div className={styles.queuedMessage}>
                {translate('LibraryImportQueued')}
              </div> :
              <div>
                <div className={styles.controls}>
                  {
                    rootFolders.length > 1 &&
                      <label className={styles.control}>
                        {translate('RootFolder')}
                        <select
                          className={styles.select}
                          value={selectedRootFolderId || ''}
                          onChange={this.onRootFolderChange}
                        >
                          {
                            rootFolders.map((r) => {
                              return (
                                <option key={r.id} value={r.id}>{r.path}</option>
                              );
                            })
                          }
                        </select>
                      </label>
                  }

                  <label className={styles.control}>
                    {translate('QualityProfile')}
                    <select
                      className={styles.select}
                      value={qualityProfileId || ''}
                      onChange={this.onQualityProfileChange}
                    >
                      {
                        qualityProfiles.map((q) => {
                          return (
                            <option key={q.id} value={q.id}>{q.name}</option>
                          );
                        })
                      }
                    </select>
                  </label>

                  <label className={styles.control}>
                    <input
                      type="checkbox"
                      checked={monitored}
                      onChange={this.onMonitoredChange}
                    />
                    {translate('Monitored')}
                  </label>
                </div>

                {isFetching && <LoadingIndicator />}

                {
                  error &&
                    <div className={styles.error}>{error}</div>
                }

                {
                  !isFetching && proposals && !proposals.length &&
                    <div className={styles.empty}>
                      {translate('LibraryImportNoProposals')}
                    </div>
                }

                {
                  !isFetching && proposals && !!proposals.length &&
                    <table className={styles.table}>
                      <thead>
                        <tr>
                          <th />
                          <th>{translate('Series')}</th>
                          <th>{translate('Confidence')}</th>
                          <th>{translate('Source')}</th>
                          <th>{translate('Files')}</th>
                          <th>{translate('Folder')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {
                          proposals.map((p) => {
                            const existing = !!p.existingSeriesId;

                            return (
                              <tr key={p.id} className={existing ? styles.existingRow : undefined}>
                                <td>
                                  <input
                                    type="checkbox"
                                    checked={!!checkedState[p.id] && !existing}
                                    disabled={existing}
                                    onChange={() => this.onToggleProposal(p.id)}
                                  />
                                </td>
                                <td>
                                  {p.name}{p.year ? ` (${p.year})` : ''}
                                  {existing ? ` — ${translate('AlreadyInLibrary')}` : ''}
                                </td>
                                <td>
                                  <span className={p.confidence === 'exact' ? styles.exact : styles.probable}>
                                    {p.confidence}
                                  </span>
                                </td>
                                <td>{p.idSource}</td>
                                <td>{p.fileCount}</td>
                                <td className={styles.folder}>{p.folder}</td>
                              </tr>
                            );
                          })
                        }
                      </tbody>
                    </table>
                }
              </div>
          }
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {importQueued ? translate('Close') : translate('Cancel')}
          </Button>

          {
            !importQueued &&
              <Button
                kind={kinds.PRIMARY}
                size={sizes.MEDIUM}
                isDisabled={isImporting || selectedCount === 0}
                onPress={this.onImportPress}
              >
                {translate('ImportCountSeries', { count: selectedCount })}
              </Button>
          }
        </ModalFooter>
      </ModalContent>
    );
  }
}

LibraryImportModalContent.propTypes = {
  onModalClose: PropTypes.func.isRequired
};

export default LibraryImportModalContent;

export function LibraryImportModal({ isOpen, onModalClose }) {
  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      onModalClose={onModalClose}
    >
      <LibraryImportModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

LibraryImportModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};
