import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Card from 'Components/Card';
import Label from 'Components/Label';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TagList from 'Components/TagList';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import EditNotificationModalConnector from './EditNotificationModalConnector';
import styles from './Notification.css';

class Notification extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditNotificationModalOpen: false,
      isDeleteNotificationModalOpen: false
    };
  }

  //
  // Listeners

  onEditNotificationPress = () => {
    this.setState({ isEditNotificationModalOpen: true });
  };

  onEditNotificationModalClose = () => {
    this.setState({ isEditNotificationModalOpen: false });
  };

  onDeleteNotificationPress = () => {
    this.setState({
      isEditNotificationModalOpen: false,
      isDeleteNotificationModalOpen: true
    });
  };

  onDeleteNotificationModalClose= () => {
    this.setState({ isDeleteNotificationModalOpen: false });
  };

  onConfirmDeleteNotification = () => {
    this.props.onConfirmDeleteNotification(this.props.id);
  };

  //
  // Render

  render() {
    const {
      id,
      name,
      onGrab,
      onReleaseImport,
      onUpgrade,
      onRename,
      onSeriesAdded,
      onSeriesDelete,
      onIssueDelete,
      onComicFileDelete,
      onComicFileDeleteForUpgrade,
      onHealthIssue,
      onDownloadFailure,
      onImportFailure,
      onIssueRetag,
      onApplicationUpdate,
      supportsOnGrab,
      supportsOnReleaseImport,
      supportsOnUpgrade,
      supportsOnRename,
      supportsOnSeriesAdded,
      supportsOnSeriesDelete,
      supportsOnIssueDelete,
      supportsOnComicFileDelete,
      supportsOnComicFileDeleteForUpgrade,
      supportsOnHealthIssue,
      supportsOnDownloadFailure,
      supportsOnImportFailure,
      supportsOnIssueRetag,
      supportsOnApplicationUpdate,
      tags,
      tagList
    } = this.props;

    return (
      <Card
        className={styles.notification}
        overlayContent={true}
        onPress={this.onEditNotificationPress}
      >
        <div className={styles.name}>
          {name}
        </div>

        {
          supportsOnGrab && onGrab ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnGrab')}
            </Label> :
            null
        }

        {
          supportsOnReleaseImport && onReleaseImport ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnReleaseImport')}
            </Label> :
            null
        }

        {
          supportsOnUpgrade && onReleaseImport && onUpgrade ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnUpgrade')}
            </Label> :
            null
        }

        {
          supportsOnRename && onRename ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnRename')}
            </Label> :
            null
        }

        {
          supportsOnIssueRetag && onIssueRetag ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnIssueTagUpdate')}
            </Label> :
            null
        }

        {
          supportsOnSeriesAdded && onSeriesAdded ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnSeriesAdded')}
            </Label> :
            null
        }

        {
          supportsOnSeriesDelete && onSeriesDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnSeriesDelete')}
            </Label> :
            null
        }

        {
          supportsOnIssueDelete && onIssueDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnIssueDelete')}
            </Label> :
            null
        }

        {
          supportsOnComicFileDelete && onComicFileDelete ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnIssueFileDelete')}
            </Label> :
            null
        }

        {
          supportsOnComicFileDeleteForUpgrade && onComicFileDelete && onComicFileDeleteForUpgrade ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnIssueFileDeleteForUpgrade')}
            </Label> :
            null
        }

        {
          supportsOnHealthIssue && onHealthIssue ?
            <Label kind={kinds.SUCCESS}>
              {translate('OnHealthIssue')}
            </Label> :
            null
        }

        {
          supportsOnDownloadFailure && onDownloadFailure ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnDownloadFailure')}
            </Label> :
            null
        }

        {
          supportsOnImportFailure && onImportFailure ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnImportFailure')}
            </Label> :
            null
        }

        {
          supportsOnApplicationUpdate && onApplicationUpdate ?
            <Label kind={kinds.SUCCESS} >
              {translate('OnApplicationUpdate')}
            </Label> :
            null
        }

        {
          !onGrab && !onReleaseImport && !onRename && !onIssueRetag && !onHealthIssue && !onDownloadFailure && !onImportFailure ?
            <Label
              kind={kinds.DISABLED}
              outline={true}
            >
              {translate('Disabled')}
            </Label> :
            null
        }

        <TagList
          tags={tags}
          tagList={tagList}
        />

        <EditNotificationModalConnector
          id={id}
          isOpen={this.state.isEditNotificationModalOpen}
          onModalClose={this.onEditNotificationModalClose}
          onDeleteNotificationPress={this.onDeleteNotificationPress}
        />

        <ConfirmModal
          isOpen={this.state.isDeleteNotificationModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteNotification')}
          message={translate('DeleteNotificationMessageText', { name })}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDeleteNotification}
          onCancel={this.onDeleteNotificationModalClose}
        />
      </Card>
    );
  }
}

Notification.propTypes = {
  id: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  onGrab: PropTypes.bool.isRequired,
  onReleaseImport: PropTypes.bool.isRequired,
  onUpgrade: PropTypes.bool.isRequired,
  onRename: PropTypes.bool.isRequired,
  onSeriesAdded: PropTypes.bool.isRequired,
  onSeriesDelete: PropTypes.bool.isRequired,
  onIssueDelete: PropTypes.bool.isRequired,
  onComicFileDelete: PropTypes.bool.isRequired,
  onComicFileDeleteForUpgrade: PropTypes.bool.isRequired,
  onHealthIssue: PropTypes.bool.isRequired,
  onDownloadFailure: PropTypes.bool.isRequired,
  onImportFailure: PropTypes.bool.isRequired,
  onIssueRetag: PropTypes.bool.isRequired,
  onApplicationUpdate: PropTypes.bool.isRequired,
  supportsOnGrab: PropTypes.bool.isRequired,
  supportsOnReleaseImport: PropTypes.bool.isRequired,
  supportsOnUpgrade: PropTypes.bool.isRequired,
  supportsOnRename: PropTypes.bool.isRequired,
  supportsOnSeriesAdded: PropTypes.bool.isRequired,
  supportsOnSeriesDelete: PropTypes.bool.isRequired,
  supportsOnIssueDelete: PropTypes.bool.isRequired,
  supportsOnComicFileDelete: PropTypes.bool.isRequired,
  supportsOnComicFileDeleteForUpgrade: PropTypes.bool.isRequired,
  supportsOnHealthIssue: PropTypes.bool.isRequired,
  supportsOnDownloadFailure: PropTypes.bool.isRequired,
  supportsOnImportFailure: PropTypes.bool.isRequired,
  supportsOnIssueRetag: PropTypes.bool.isRequired,
  supportsOnApplicationUpdate: PropTypes.bool.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  tagList: PropTypes.arrayOf(PropTypes.object).isRequired,
  onConfirmDeleteNotification: PropTypes.func.isRequired
};

export default Notification;
