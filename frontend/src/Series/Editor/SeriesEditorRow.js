import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesNameLink from 'Series/SeriesNameLink';
import SeriesStatusCell from 'Series/Index/Table/SeriesStatusCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import formatBytes from 'Utilities/Number/formatBytes';

class SeriesEditorRow extends Component {

  //
  // Listeners

  onIssueFolderChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  };

  //
  // Render

  render() {
    const {
      id,
      status,
      titleSlug,
      seriesName,
      monitored,
      qualityProfile,
      path,
      statistics,
      tags,
      columns,
      isSelected,
      onSelectedChange
    } = this.props;

    return (
      <TableRow>
        <TableSelectCell
          id={id}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
        />

        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'status') {
              return (
                <SeriesStatusCell
                  key={name}
                  monitored={monitored}
                  status={status}
                />
              );
            }

            if (name === 'sortName') {
              return (
                <TableRowCell
                  key={name}
                >
                  <SeriesNameLink
                    titleSlug={titleSlug}
                    seriesName={seriesName}
                  />
                </TableRowCell>
              );
            }

            if (name === 'qualityProfileId') {
              return (
                <TableRowCell key={name}>
                  {qualityProfile.name}
                </TableRowCell>
              );
            }

            if (name === 'path') {
              return (
                <TableRowCell key={name}>
                  {path}
                </TableRowCell>
              );
            }

            if (name === 'sizeOnDisk') {
              return (
                <TableRowCell key={name}>
                  {formatBytes(statistics.sizeOnDisk)}
                </TableRowCell>
              );
            }

            if (name === 'tags') {
              return (
                <TableRowCell key={name}>
                  <TagListConnector
                    tags={tags}
                  />
                </TableRowCell>
              );
            }

            return null;
          })
        }
      </TableRow>
    );
  }
}

SeriesEditorRow.propTypes = {
  id: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  qualityProfile: PropTypes.object.isRequired,
  path: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

SeriesEditorRow.defaultProps = {
  tags: []
};

export default SeriesEditorRow;
