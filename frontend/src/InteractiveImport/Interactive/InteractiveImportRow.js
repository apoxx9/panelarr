import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IssueFormats from 'Issue/IssueFormats';
import IssueQuality from 'Issue/IssueQuality';
import IndexerFlags from 'Issue/IndexerFlags';
import FileDetails from 'IssueFile/FileDetails';
import Icon from 'Components/Icon';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRowCellButton from 'Components/Table/Cells/TableRowCellButton';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import { icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import SelectSeriesModal from 'InteractiveImport/Series/SelectSeriesModal';
import SelectIssueModal from 'InteractiveImport/Issue/SelectIssueModal';
import SelectIndexerFlagsModal from 'InteractiveImport/IndexerFlags/SelectIndexerFlagsModal';
import SelectQualityModal from 'InteractiveImport/Quality/SelectQualityModal';
import SelectReleaseGroupModal from 'InteractiveImport/ReleaseGroup/SelectReleaseGroupModal';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import InteractiveImportRowCellPlaceholder from './InteractiveImportRowCellPlaceholder';
import styles from './InteractiveImportRow.css';

class InteractiveImportRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isSelectSeriesModalOpen: false,
      isSelectIssueModalOpen: false,
      isSelectReleaseGroupModalOpen: false,
      isSelectQualityModalOpen: false,
      isSelectIndexerFlagsModalOpen: false
    };
  }

  componentDidMount() {
    const {
      id,
      series,
      issue,
      quality,
      size
    } = this.props;

    if (
      series &&
      issue != null &&
      quality &&
      size > 0
    ) {
      this.props.onSelectedChange({ id, value: true });
    }
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      series,
      issue,
      quality,
      isSelected,
      onValidRowChange
    } = this.props;

    if (
      prevProps.series === series &&
      prevProps.issue === issue &&
      prevProps.quality === quality &&
      prevProps.isSelected === isSelected
    ) {
      return;
    }

    const isValid = !!(
      series &&
      issue &&
      quality
    );

    if (isSelected && !isValid) {
      onValidRowChange(id, false);
    } else {
      onValidRowChange(id, true);
    }
  }

  //
  // Control

  selectRowAfterChange = (value) => {
    const {
      id,
      isSelected
    } = this.props;

    if (!isSelected && value === true) {
      this.props.onSelectedChange({ id, value });
    }
  };

  //
  // Listeners

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onSelectSeriesPress = () => {
    this.setState({ isSelectSeriesModalOpen: true });
  };

  onSelectIssuePress = () => {
    this.setState({ isSelectIssueModalOpen: true });
  };

  onSelectReleaseGroupPress = () => {
    this.setState({ isSelectReleaseGroupModalOpen: true });
  };

  onSelectQualityPress = () => {
    this.setState({ isSelectQualityModalOpen: true });
  };

  onSelectIndexerFlagsPress = () => {
    this.setState({ isSelectIndexerFlagsModalOpen: true });
  };

  onSelectSeriesModalClose = (changed) => {
    this.setState({ isSelectSeriesModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectIssueModalClose = (changed) => {
    this.setState({ isSelectIssueModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectReleaseGroupModalClose = (changed) => {
    this.setState({ isSelectReleaseGroupModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectQualityModalClose = (changed) => {
    this.setState({ isSelectQualityModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  onSelectIndexerFlagsModalClose = (changed) => {
    this.setState({ isSelectIndexerFlagsModalOpen: false });
    this.selectRowAfterChange(changed);
  };

  //
  // Render

  render() {
    const {
      id,
      allowSeriesChange,
      path,
      series,
      issue,
      quality,
      releaseGroup,
      size,
      customFormats,
      indexerFlags,
      rejections,
      columns,
      additionalFile,
      isSelected,
      isReprocessing,
      onSelectedChange,
      fileTags
    } = this.props;

    const {
      isDetailsModalOpen,
      isSelectSeriesModalOpen,
      isSelectIssueModalOpen,
      isSelectReleaseGroupModalOpen,
      isSelectQualityModalOpen,
      isSelectIndexerFlagsModalOpen
    } = this.state;

    const seriesName = series ? series.seriesName : '';
    let issueTitle = '';
    if (issue) {
      issueTitle = issue.disambiguation ? `${issue.title} (${issue.disambiguation})` : issue.title;
    }

    const showSeriesPlaceholder = isSelected && !series;
    const showIssueNumberPlaceholder = !isReprocessing && isSelected && !!series && !issue;
    const showReleaseGroupPlaceholder = isSelected && !releaseGroup;
    const showQualityPlaceholder = isSelected && !quality;
    const showIndexerFlagsPlaceholder = isSelected && !indexerFlags;

    const pathCellContents = (
      <div onClick={this.onDetailsPress}>
        {path}
      </div>
    );

    const pathCell = additionalFile ? (
      <Tooltip
        anchor={pathCellContents}
        tooltip='This file is already in your library for a release you are currently importing'
        position={tooltipPositions.TOP}
      />
    ) : pathCellContents;

    const fileDetails = (
      <FileDetails
        fileTags={fileTags}
        filename={path}
      />
    );

    const isIndexerFlagsColumnVisible = columns.find((c) => c.name === 'indexerFlags')?.isVisible ?? false;

    return (
      <TableRow
        className={additionalFile ? styles.additionalFile : undefined}
      >
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        <TableRowCell
          className={styles.path}
          title={path}
        >
          {pathCell}
        </TableRowCell>

        <TableRowCellButton
          isDisabled={!allowSeriesChange}
          title={allowSeriesChange ? translate('AllowSeriesChangeClickToChangeSeries') : undefined}
          onPress={this.onSelectSeriesPress}
        >
          {
            showSeriesPlaceholder ? <InteractiveImportRowCellPlaceholder /> : seriesName
          }
        </TableRowCellButton>

        <TableRowCellButton
          isDisabled={!series}
          title={series ? translate('SeriesClickToChangeIssue') : undefined}
          onPress={this.onSelectIssuePress}
        >
          {
            showIssueNumberPlaceholder ? <InteractiveImportRowCellPlaceholder /> : issueTitle
          }
        </TableRowCellButton>

        <TableRowCellButton
          title={translate('ClickToChangeReleaseGroup')}
          onPress={this.onSelectReleaseGroupPress}
        >
          {
            showReleaseGroupPlaceholder ?
              <InteractiveImportRowCellPlaceholder
                isOptional={true}
              /> :
              releaseGroup
          }
        </TableRowCellButton>

        <TableRowCellButton
          className={styles.quality}
          title={translate('ClickToChangeQuality')}
          onPress={this.onSelectQualityPress}
        >
          {
            showQualityPlaceholder &&
              <InteractiveImportRowCellPlaceholder />
          }

          {
            !showQualityPlaceholder && !!quality &&
              <IssueQuality
                className={styles.label}
                quality={quality}
              />
          }
        </TableRowCellButton>

        <TableRowCell>
          {formatBytes(size)}
        </TableRowCell>

        <TableRowCell>
          {
            customFormats?.length ?
              <Popover
                anchor={
                  <Icon name={icons.INTERACTIVE} />
                }
                title={translate('Formats')}
                body={
                  <div className={styles.customFormatTooltip}>
                    <IssueFormats formats={customFormats} />
                  </div>
                }
                position={tooltipPositions.LEFT}
              /> :
              null
          }
        </TableRowCell>

        {isIndexerFlagsColumnVisible ? (
          <TableRowCellButton
            title={translate('ClickToChangeIndexerFlags')}
            onPress={this.onSelectIndexerFlagsPress}
          >
            {showIndexerFlagsPlaceholder ? (
              <InteractiveImportRowCellPlaceholder isOptional={true} />
            ) : (
              <>
                {indexerFlags ? (
                  <Popover
                    anchor={<Icon name={icons.FLAG} kind={kinds.PRIMARY} />}
                    title={translate('IndexerFlags')}
                    body={<IndexerFlags indexerFlags={indexerFlags} />}
                    position={tooltipPositions.LEFT}
                  />
                ) : null}
              </>
            )}
          </TableRowCellButton>
        ) : null}

        <TableRowCell>
          {
            rejections.length ?
              <Popover
                anchor={
                  <Icon
                    name={icons.DANGER}
                    kind={kinds.DANGER}
                  />
                }
                title={translate('ReleaseRejected')}
                body={
                  <ul>
                    {
                      rejections.map((rejection, index) => {
                        return (
                          <li key={index}>
                            {rejection.reason}
                          </li>
                        );
                      })
                    }
                  </ul>
                }
                position={tooltipPositions.LEFT}
                canFlip={false}
              /> :
              null
          }
        </TableRowCell>

        <ConfirmModal
          isOpen={isDetailsModalOpen}
          title={translate('FileDetails')}
          message={fileDetails}
          size={sizes.LARGE}
          kind={kinds.DEFAULT}
          hideCancelButton={true}
          confirmLabel={translate('Close')}
          onConfirm={this.onDetailsModalClose}
          onCancel={this.onDetailsModalClose}
        />

        <SelectSeriesModal
          isOpen={isSelectSeriesModalOpen}
          ids={[id]}
          onModalClose={this.onSelectSeriesModalClose}
        />

        <SelectIssueModal
          isOpen={isSelectIssueModalOpen}
          ids={[id]}
          seriesId={series && series.id}
          onModalClose={this.onSelectIssueModalClose}
        />

        <SelectReleaseGroupModal
          isOpen={isSelectReleaseGroupModalOpen}
          ids={[id]}
          releaseGroup={releaseGroup ?? ''}
          onModalClose={this.onSelectReleaseGroupModalClose}
        />

        <SelectQualityModal
          isOpen={isSelectQualityModalOpen}
          ids={[id]}
          qualityId={quality ? quality.quality.id : 0}
          proper={quality ? quality.revision.version > 1 : false}
          real={quality ? quality.revision.real > 0 : false}
          onModalClose={this.onSelectQualityModalClose}
        />

        <SelectIndexerFlagsModal
          isOpen={isSelectIndexerFlagsModalOpen}
          ids={[id]}
          indexerFlags={indexerFlags ?? 0}
          onModalClose={this.onSelectIndexerFlagsModalClose}
        />
      </TableRow>
    );
  }

}

InteractiveImportRow.propTypes = {
  id: PropTypes.number.isRequired,
  allowSeriesChange: PropTypes.bool.isRequired,
  path: PropTypes.string.isRequired,
  series: PropTypes.object,
  issue: PropTypes.object,
  releaseGroup: PropTypes.string,
  quality: PropTypes.object,
  size: PropTypes.number.isRequired,
  customFormats: PropTypes.arrayOf(PropTypes.object),
  indexerFlags: PropTypes.number.isRequired,
  rejections: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  fileTags: PropTypes.object.isRequired,
  additionalFile: PropTypes.bool.isRequired,
  isReprocessing: PropTypes.bool,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onValidRowChange: PropTypes.func.isRequired
};

export default InteractiveImportRow;
