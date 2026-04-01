import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import moment from 'moment';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import styles from './IssueMetadataTable.css';

function buildDatabaseMetadata(issue, series) {
  const fields = {};

  if (series && series.seriesName) {
    fields.Series = series.seriesName;
  }

  if (series && series.disambiguation) {
    fields.Imprint = series.disambiguation;
  }

  if (issue.issueNumber) {
    fields.Number = String(issue.issueNumber);
  }

  if (issue.title) {
    fields.Title = issue.title;
  }

  if (issue.seriesTitle) {
    fields['Series Title'] = issue.seriesTitle;
  }

  if (series && series.publisherName) {
    fields.Publisher = series.publisherName;
  }

  if (issue.releaseDate) {
    fields['Release Date'] = moment(issue.releaseDate).format('YYYY-MM-DD');
  }

  if (issue.pageCount) {
    fields['Page Count'] = String(issue.pageCount);
  }

  if (issue.ratings && issue.ratings.value > 0) {
    fields.Rating = String(issue.ratings.value);
  }

  if (issue.genres && issue.genres.length) {
    fields.Genres = issue.genres.join(', ');
  }

  if (issue.overview) {
    fields.Summary = issue.overview;
  } else if (series && series.overview) {
    fields.Summary = series.overview;
  }

  if (issue.links && issue.links.length) {
    issue.links.forEach((link) => {
      if (link.url) {
        fields[link.name || 'Link'] = link.url;
      }
    });
  }

  if (issue.foreignIssueId) {
    fields['Foreign ID'] = issue.foreignIssueId;
  }

  if (issue.coverArtUrl) {
    fields['Cover Art URL'] = issue.coverArtUrl;
  }

  return fields;
}

function renderSection(source, fields) {
  if (!fields || !Object.keys(fields).length) {
    return null;
  }

  return (
    <div key={source} className={styles.section}>
      <div className={styles.sectionHeader}>
        {source}
      </div>

      <table className={styles.table}>
        <tbody>
          {
            Object.entries(fields).map(([key, value]) => (
              <tr key={key} className={styles.row}>
                <td className={styles.fieldName}>
                  {key}
                </td>
                <td className={styles.fieldValue}>
                  {value}
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>
    </div>
  );
}

class IssueMetadataTable extends Component {

  //
  // Lifecycle

  constructor(props) {
    super(props);

    this.state = {
      isFetching: false,
      fileMetadata: []
    };
  }

  componentDidMount() {
    this.fetchFileMetadata();
  }

  componentDidUpdate(prevProps) {
    const issueChanged = prevProps.issueId !== this.props.issueId;
    const filesArrived = prevProps.issueFileIds.length === 0 &&
      this.props.issueFileIds.length > 0;

    if (issueChanged || filesArrived) {
      this.fetchFileMetadata();
    }
  }

  //
  // Methods

  fetchFileMetadata() {
    const { issueFileIds } = this.props;

    if (!issueFileIds || !issueFileIds.length) {
      this.setState({ isFetching: false, fileMetadata: [] });
      return;
    }

    this.setState({ isFetching: true });

    const fileId = issueFileIds[0];

    const { request } = createAjaxRequest({
      url: `/comicfile/${fileId}/metadata`,
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ isFetching: false, fileMetadata: data });
    });

    request.fail(() => {
      this.setState({ isFetching: false, fileMetadata: [] });
    });
  }

  //
  // Render

  render() {
    const {
      issue,
      series
    } = this.props;

    const {
      isFetching,
      fileMetadata
    } = this.state;

    const dbFields = buildDatabaseMetadata(issue, series);

    return (
      <div className={styles.container}>
        {renderSection('Database', dbFields)}

        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          fileMetadata.map((section) =>
            renderSection(section.source, section.fields)
          )
        }
      </div>
    );
  }
}

IssueMetadataTable.propTypes = {
  issueId: PropTypes.number.isRequired,
  issue: PropTypes.object.isRequired,
  series: PropTypes.object.isRequired,
  issueFileIds: PropTypes.arrayOf(PropTypes.number).isRequired
};

function createMapStateToProps() {
  return (state, ownProps) => {
    const issue = state.issues.items.find((i) => i.id === ownProps.issueId) || {};
    const series = state.seriess.items.find((s) => s.id === issue.seriesId) || {};
    const issueFileIds = state.issueFiles.items
      .filter((f) => f.issueId === ownProps.issueId)
      .map((f) => f.id);

    return {
      issue,
      series,
      issueFileIds
    };
  };
}

export default connect(createMapStateToProps)(IssueMetadataTable);
