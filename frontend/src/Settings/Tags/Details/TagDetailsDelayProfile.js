import PropTypes from 'prop-types';
import React from 'react';
import titleCase from 'Utilities/String/titleCase';

function TagDetailsDelayProfile(props) {
  const {
    preferredProtocol,
    enableTorrent,
    enableDirectDownload,
    torrentDelay,
    directDownloadDelay
  } = props;

  const preferred = preferredProtocol === 'directDownload' ? 'Direct Download' : titleCase(preferredProtocol);

  return (
    <div>
      <div>
        Protocol: {preferred}
      </div>

      <div>
        {
          enableTorrent ?
            `Torrent Delay: ${torrentDelay}` :
            'Torrents disabled'
        }
      </div>

      <div>
        {
          enableDirectDownload ?
            `Direct Download Delay: ${directDownloadDelay}` :
            'Direct downloads disabled'
        }
      </div>
    </div>
  );
}

TagDetailsDelayProfile.propTypes = {
  preferredProtocol: PropTypes.string.isRequired,
  enableTorrent: PropTypes.bool.isRequired,
  enableDirectDownload: PropTypes.bool.isRequired,
  torrentDelay: PropTypes.number.isRequired,
  directDownloadDelay: PropTypes.number.isRequired
};

export default TagDetailsDelayProfile;
