import PropTypes from 'prop-types';
import React, { Component } from 'react';
import PathInputConnector from 'Components/Form/PathInputConnector';
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
import styles from './StagingImportModalContent.css';

const COMMAND_POLL_INTERVAL = 1500;

// Staging import: identify series in a folder OUTSIDE the library and
// transfer their files into a root folder, renamed per the naming settings
// (docs/staging-folder-import.md). Unlike the in-place library import,
// existing series are importable targets, and the modal stays open to show
// the per-file report — files that stay in staging list the reason why.
class StagingImportModalContent extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      isFetching: true,
      error: null,
      rootFolders: [],
      qualityProfiles: [],
      folder: '',
      isScanning: false,
      scanError: null,
      proposals: null,
      checkedState: {},
      selectedRootFolderId: null,
      qualityProfileId: null,
      monitored: true,
      keepSourceFiles: false,
      isImporting: false,
      commandId: null,
      commandStatus: null,
      commandMessage: null,
      report: null,
      reportError: null
    };
  }

  componentDidMount() {
    const rootFoldersRequest = createAjaxRequest({ url: '/rootfolder', dataType: 'json' }).request;
    const profilesRequest = createAjaxRequest({ url: '/qualityprofile', dataType: 'json' }).request;
    const configRequest = createAjaxRequest({ url: '/config/mediamanagement', dataType: 'json' }).request;

    rootFoldersRequest.done((rootFolders) => {
      profilesRequest.done((qualityProfiles) => {
        configRequest.done((config) => {
          const firstRootFolder = rootFolders[0];
          const folder = config.stagingFolder || '';

          this.setState({
            isFetching: false,
            rootFolders,
            qualityProfiles,
            folder,
            selectedRootFolderId: firstRootFolder ? firstRootFolder.id : null,
            qualityProfileId: firstRootFolder ? firstRootFolder.defaultQualityProfileId : null
          }, () => {
            if (folder) {
              this.onScanPress();
            }
          });
        });

        configRequest.fail(() => this.setState({ isFetching: false, error: translate('LibraryImportLoadError') }));
      });

      profilesRequest.fail(() => this.setState({ isFetching: false, error: translate('LibraryImportLoadError') }));
    });

    rootFoldersRequest.fail(() => this.setState({ isFetching: false, error: translate('LibraryImportLoadError') }));
  }

  componentWillUnmount() {
    this.stopPolling();
  }

  //
  // Scan

  onFolderChange = ({ value }) => {
    this.setState({ folder: value });
  };

  onScanPress = () => {
    const { folder } = this.state;

    if (!folder) {
      return;
    }

    this.setState({ isScanning: true, scanError: null, proposals: null });

    const { request } = createAjaxRequest({
      url: `/libraryimport/proposal?folder=${encodeURIComponent(folder)}`,
      dataType: 'json'
    });

    request.done((proposals) => {
      const checkedState = {};

      proposals.forEach((p) => {
        checkedState[p.id] = p.confidence === 'exact';
      });

      this.setState({ isScanning: false, proposals, checkedState });
    });

    request.fail((xhr) => {
      const message = xhr.responseJSON && xhr.responseJSON.message;

      this.setState({
        isScanning: false,
        scanError: message || translate('StagingImportScanError')
      });
    });
  };

  //
  // Review controls

  onRootFolderChange = (event) => {
    this.setState({ selectedRootFolderId: parseInt(event.target.value) });
  };

  onQualityProfileChange = (event) => {
    this.setState({ qualityProfileId: parseInt(event.target.value) });
  };

  onMonitoredChange = (event) => {
    this.setState({ monitored: event.target.checked });
  };

  onKeepSourceFilesChange = (event) => {
    this.setState({ keepSourceFiles: event.target.checked });
  };

  onToggleProposal = (id) => {
    this.setState((prev) => ({
      checkedState: {
        ...prev.checkedState,
        [id]: !prev.checkedState[id]
      }
    }));
  };

  //
  // Import + report

  onImportPress = () => {
    const {
      proposals,
      checkedState,
      rootFolders,
      selectedRootFolderId,
      qualityProfileId,
      monitored,
      keepSourceFiles
    } = this.state;

    const rootFolder = rootFolders.find((r) => r.id === selectedRootFolderId);

    const series = proposals
      .filter((p) => checkedState[p.id])
      .map((p) => ({ foreignSeriesId: p.foreignSeriesId, folder: p.folder }));

    if (!series.length || !rootFolder) {
      return;
    }

    this.setState({ isImporting: true });

    const { request } = createAjaxRequest({
      url: '/command',
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({
        name: 'StagingImport',
        series,
        qualityProfileId,
        monitored,
        monitorNewItems: monitored ? 'all' : 'none',
        rootFolderPath: rootFolder.path,
        keepSourceFiles
      })
    });

    request.done((command) => {
      this.setState({
        commandId: command.id,
        commandStatus: command.status,
        commandMessage: command.message
      }, this.startPolling);
    });

    request.fail(() => this.setState({ isImporting: false, error: translate('LibraryImportQueueError') }));
  };

  startPolling = () => {
    this.stopPolling();
    this._pollTimer = setInterval(this.pollCommand, COMMAND_POLL_INTERVAL);
  };

  stopPolling = () => {
    if (this._pollTimer) {
      clearInterval(this._pollTimer);
      this._pollTimer = null;
    }
  };

  pollCommand = () => {
    const { commandId } = this.state;

    const { request } = createAjaxRequest({
      url: `/command/${commandId}`,
      dataType: 'json'
    });

    request.done((command) => {
      this.setState({
        commandStatus: command.status,
        commandMessage: command.message
      });

      if (command.status === 'completed' || command.status === 'failed' || command.status === 'aborted') {
        this.stopPolling();
        this.fetchReport();
      }
    });
  };

  fetchReport = () => {
    const { request } = createAjaxRequest({
      url: `/libraryimport/stagingreport?commandId=${this.state.commandId}`,
      dataType: 'json'
    });

    request.done((report) => this.setState({ report }));
    request.fail(() => this.setState({ reportError: translate('StagingImportReportError') }));
  };

  //
  // Render

  renderReview() {
    const {
      rootFolders,
      qualityProfiles,
      folder,
      isScanning,
      scanError,
      proposals,
      checkedState,
      selectedRootFolderId,
      qualityProfileId,
      monitored,
      keepSourceFiles
    } = this.state;

    return (
      <div>
        <div className={styles.folderRow}>
          <div className={styles.pathInput}>
            <PathInputConnector
              name="stagingImportFolder"
              value={folder}
              includeFiles={false}
              onChange={this.onFolderChange}
            />
          </div>

          <Button
            isDisabled={isScanning || !folder}
            onPress={this.onScanPress}
          >
            {translate('Scan')}
          </Button>
        </div>

        <div className={styles.controls}>
          <label className={styles.control}>
            {translate('TargetRootFolder')}
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

          <label
            className={styles.control}
            title={translate('KeepSourceFilesHelpText')}
          >
            <input
              type="checkbox"
              checked={keepSourceFiles}
              onChange={this.onKeepSourceFilesChange}
            />
            {translate('KeepSourceFiles')}
          </label>
        </div>

        {isScanning && <LoadingIndicator />}

        {
          scanError &&
            <div className={styles.error}>{scanError}</div>
        }

        {
          !isScanning && proposals && !proposals.length &&
            <div className={styles.empty}>
              {translate('StagingImportNoProposals')}
            </div>
        }

        {
          !isScanning && proposals && !!proposals.length &&
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
                    return (
                      <tr key={p.id}>
                        <td>
                          <input
                            type="checkbox"
                            checked={!!checkedState[p.id]}
                            onChange={() => this.onToggleProposal(p.id)}
                          />
                        </td>
                        <td>
                          {p.name}{p.year ? ` (${p.year})` : ''}
                          {p.existingSeriesId ? ` — ${translate('AlreadyInLibrary')}` : ''}
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
    );
  }

  renderProgress() {
    const { commandStatus, commandMessage, report, reportError } = this.state;
    const isDone = !!report || !!reportError;

    if (!isDone) {
      return (
        <div className={styles.progress}>
          <LoadingIndicator />
          <div className={styles.progressMessage}>
            {commandMessage || translate('StagingImportRunning')}
          </div>
        </div>
      );
    }

    if (reportError) {
      return (
        <div>
          <div className={styles.error}>{reportError}</div>
          <div className={styles.progressMessage}>
            {commandStatus === 'failed' ? translate('StagingImportFailed') : commandMessage}
          </div>
        </div>
      );
    }

    const importedCount = report.files.filter((f) => f.outcome === 'imported').length;
    const leftCount = report.files.length - importedCount;

    return (
      <div>
        <div className={styles.reportSummary}>
          {translate('StagingImportReportSummary', { importedCount, leftCount })}
        </div>

        {
          !!report.errors.length &&
            report.errors.map((e, i) => {
              return (
                <div key={i} className={styles.error}>{e}</div>
              );
            })
        }

        {
          !!report.files.length &&
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>{translate('Path')}</th>
                  <th>{translate('Series')}</th>
                  <th>{translate('Result')}</th>
                  <th>{translate('Reasons')}</th>
                </tr>
              </thead>
              <tbody>
                {
                  report.files.map((f, i) => {
                    return (
                      <tr key={i}>
                        <td className={styles.folder}>{f.path}</td>
                        <td>{f.series}</td>
                        <td>
                          <span className={f.outcome === 'imported' ? styles.imported : styles.rejected}>
                            {f.outcome}
                          </span>
                        </td>
                        <td>{f.reasons && f.reasons.length ? f.reasons.join(', ') : '-'}</td>
                      </tr>
                    );
                  })
                }
              </tbody>
            </table>
        }
      </div>
    );
  }

  render() {
    const { onModalClose } = this.props;
    const {
      isFetching,
      error,
      proposals,
      checkedState,
      selectedRootFolderId,
      isImporting,
      commandId
    } = this.state;

    const selectedCount = proposals ?
      proposals.filter((p) => checkedState[p.id]).length :
      0;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('ImportFromStaging')}
        </ModalHeader>

        <ModalBody>
          {isFetching && <LoadingIndicator />}

          {
            error &&
              <div className={styles.error}>{error}</div>
          }

          {!isFetching && !error && !commandId && this.renderReview()}

          {!isFetching && !error && commandId && this.renderProgress()}
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {commandId ? translate('Close') : translate('Cancel')}
          </Button>

          {
            !commandId &&
              <Button
                kind={kinds.PRIMARY}
                size={sizes.MEDIUM}
                isDisabled={isImporting || selectedCount === 0 || !selectedRootFolderId}
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

StagingImportModalContent.propTypes = {
  onModalClose: PropTypes.func.isRequired
};

export default StagingImportModalContent;

export function StagingImportModal({ isOpen, onModalClose }) {
  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      onModalClose={onModalClose}
    >
      {
        isOpen &&
          <StagingImportModalContent onModalClose={onModalClose} />
      }
    </Modal>
  );
}

StagingImportModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};
