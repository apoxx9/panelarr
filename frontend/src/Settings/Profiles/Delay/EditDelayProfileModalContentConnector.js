import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveDelayProfile, setDelayProfileValue } from 'Store/Actions/settingsActions';
import selectSettings from 'Store/Selectors/selectSettings';
import EditDelayProfileModalContent from './EditDelayProfileModalContent';

const newDelayProfile = {
  enableUsenet: true,
  enableTorrent: true,
  enableDirectDownload: true,
  preferredProtocol: 'torrent',
  usenetDelay: 0,
  torrentDelay: 0,
  directDownloadDelay: 0,
  bypassIfHighestQuality: false,
  bypassIfAboveCustomFormatScore: false,
  minimumCustomFormatScore: 0,
  tags: []
};

// Comics are sourced from torrent trackers and direct-download sites; usenet
// stays enabled behind the scenes but is not a choice worth surfacing.
const protocolOptions = [
  { key: 'preferTorrent', value: 'Prefer Torrent' },
  { key: 'preferDirectDownload', value: 'Prefer Direct Download' },
  { key: 'onlyTorrent', value: 'Only Torrent' },
  { key: 'onlyDirectDownload', value: 'Only Direct Download' }
];

function createDelayProfileSelector() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.settings.delayProfiles,
    (id, delayProfiles) => {
      const {
        isFetching,
        error,
        isSaving,
        saveError,
        pendingChanges,
        items
      } = delayProfiles;

      const profile = id ? _.find(items, { id }) : newDelayProfile;
      const settings = selectSettings(profile, pendingChanges, saveError);

      return {
        id,
        isFetching,
        error,
        isSaving,
        saveError,
        item: settings.settings,
        ...settings
      };
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createDelayProfileSelector(),
    (delayProfile) => {
      const enableTorrent = delayProfile.item.enableTorrent.value;
      const enableDirectDownload = delayProfile.item.enableDirectDownload.value;
      const preferredProtocol = delayProfile.item.preferredProtocol.value;
      let protocol = 'preferTorrent';

      if (preferredProtocol === 'directDownload') {
        protocol = 'preferDirectDownload';
      }

      if (!enableDirectDownload) {
        protocol = 'onlyTorrent';
      }

      if (!enableTorrent) {
        protocol = 'onlyDirectDownload';
      }

      return {
        protocol,
        protocolOptions,
        ...delayProfile
      };
    }
  );
}

const mapDispatchToProps = {
  setDelayProfileValue,
  saveDelayProfile
};

class EditDelayProfileModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    if (!this.props.id) {
      Object.keys(newDelayProfile).forEach((name) => {
        this.props.setDelayProfileValue({
          name,
          value: newDelayProfile[name]
        });
      });
    }
  }

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setDelayProfileValue({ name, value });
  };

  onProtocolChange = ({ value }) => {
    switch (value) {
      case 'preferTorrent':
        this.props.setDelayProfileValue({ name: 'enableTorrent', value: true });
        this.props.setDelayProfileValue({ name: 'enableDirectDownload', value: true });
        this.props.setDelayProfileValue({ name: 'preferredProtocol', value: 'torrent' });
        break;
      case 'preferDirectDownload':
        this.props.setDelayProfileValue({ name: 'enableTorrent', value: true });
        this.props.setDelayProfileValue({ name: 'enableDirectDownload', value: true });
        this.props.setDelayProfileValue({ name: 'preferredProtocol', value: 'directDownload' });
        break;
      case 'onlyTorrent':
        this.props.setDelayProfileValue({ name: 'enableTorrent', value: true });
        this.props.setDelayProfileValue({ name: 'enableDirectDownload', value: false });
        this.props.setDelayProfileValue({ name: 'preferredProtocol', value: 'torrent' });
        break;
      case 'onlyDirectDownload':
        this.props.setDelayProfileValue({ name: 'enableTorrent', value: false });
        this.props.setDelayProfileValue({ name: 'enableDirectDownload', value: true });
        this.props.setDelayProfileValue({ name: 'preferredProtocol', value: 'directDownload' });
        break;
      default:
        throw Error(`Unknown protocol option: ${value}`);
    }
  };

  onSavePress = () => {
    this.props.saveDelayProfile({ id: this.props.id });
  };

  //
  // Render

  render() {
    return (
      <EditDelayProfileModalContent
        {...this.props}
        onSavePress={this.onSavePress}
        onInputChange={this.onInputChange}
        onProtocolChange={this.onProtocolChange}
      />
    );
  }
}

EditDelayProfileModalContentConnector.propTypes = {
  id: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  setDelayProfileValue: PropTypes.func.isRequired,
  saveDelayProfile: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditDelayProfileModalContentConnector);
