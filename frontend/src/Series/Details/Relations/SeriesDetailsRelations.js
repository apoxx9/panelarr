import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';
import styles from './SeriesDetailsRelations.css';

const RELATION_TYPES = ['related', 'annual', 'spinOff'];

// Related-series links (feature-landscape #11): display-only grouping of
// annuals/spin-offs with their parent. Links are stored directionally with a
// type but rendered symmetrically — this strip shows every link touching the
// series, whichever side it was created from.
class SeriesDetailsRelations extends Component {

  constructor(props, context) {
    super(props, context);

    this.state = {
      relations: [],
      isAdding: false,
      selectedSeriesId: 0,
      selectedType: 'related',
      error: null
    };
  }

  componentDidMount() {
    this.fetchRelations();
  }

  componentDidUpdate(prevProps) {
    if (prevProps.seriesId !== this.props.seriesId) {
      this.fetchRelations();
    }
  }

  fetchRelations = () => {
    const { request } = createAjaxRequest({
      url: `/seriesrelation?seriesId=${this.props.seriesId}`,
      dataType: 'json'
    });

    request.done((relations) => this.setState({ relations, error: null }));
    request.fail(() => this.setState({ error: translate('RelatedSeriesLoadError') }));
  };

  onAddPress = () => {
    this.setState({ isAdding: true, selectedSeriesId: 0, selectedType: 'related', error: null });
  };

  onCancelPress = () => {
    this.setState({ isAdding: false, error: null });
  };

  onSeriesChange = (event) => {
    this.setState({ selectedSeriesId: parseInt(event.target.value) });
  };

  onTypeChange = (event) => {
    this.setState({ selectedType: event.target.value });
  };

  onSavePress = () => {
    const { seriesId } = this.props;
    const { selectedSeriesId, selectedType } = this.state;

    if (!selectedSeriesId) {
      return;
    }

    const { request } = createAjaxRequest({
      url: '/seriesrelation',
      method: 'POST',
      contentType: 'application/json',
      dataType: 'json',
      data: JSON.stringify({
        seriesId,
        relatedSeriesId: selectedSeriesId,
        relationType: selectedType
      })
    });

    request.done(() => {
      this.setState({ isAdding: false });
      this.fetchRelations();
    });

    request.fail((xhr) => {
      const message = xhr.responseJSON && (xhr.responseJSON.message || xhr.responseJSON.content);

      this.setState({ error: message || translate('RelatedSeriesSaveError') });
    });
  };

  onRemovePress = (id) => {
    const { request } = createAjaxRequest({
      url: `/seriesrelation/${id}`,
      method: 'DELETE'
    });

    request.done(() => this.fetchRelations());
  };

  render() {
    const { seriesId, allSeries } = this.props;
    const { relations, isAdding, selectedSeriesId, selectedType, error } = this.state;

    const seriesById = new Map(allSeries.map((s) => [s.id, s]));

    const linked = relations
      .map((r) => {
        const otherId = r.seriesId === seriesId ? r.relatedSeriesId : r.seriesId;
        const other = seriesById.get(otherId);

        return other ? { relation: r, series: other } : null;
      })
      .filter(Boolean);

    const linkedIds = new Set(linked.map((l) => l.series.id));

    const candidates = allSeries
      .filter((s) => s.id !== seriesId && !linkedIds.has(s.id))
      .sort((a, b) => a.sortName.localeCompare(b.sortName));

    if (!linked.length && !isAdding) {
      return (
        <div className={styles.relations}>
          <Link
            className={styles.addLink}
            component="div"
            onPress={this.onAddPress}
          >
            {translate('LinkRelatedSeries')}
          </Link>
        </div>
      );
    }

    return (
      <div className={styles.relations}>
        <span className={styles.label}>
          {translate('RelatedSeries')}:
        </span>

        {
          linked.map(({ relation, series }) => {
            return (
              <span key={relation.id} className={styles.chip}>
                <Link
                  className={styles.chipLink}
                  to={`/series/${series.titleSlug}`}
                >
                  {series.seriesName}
                </Link>

                <span className={styles.chipType}>
                  {translate(`SeriesRelationType_${relation.relationType}`)}
                </span>

                <IconButton
                  className={styles.chipRemove}
                  name={icons.REMOVE}
                  size={11}
                  title={translate('Delete')}
                  onPress={() => this.onRemovePress(relation.id)}
                />
              </span>
            );
          })
        }

        {
          isAdding ?
            <span className={styles.addForm}>
              <select
                className={styles.select}
                value={selectedSeriesId}
                onChange={this.onSeriesChange}
              >
                <option value={0}>{translate('SelectSeries')}</option>
                {
                  candidates.map((s) => {
                    return (
                      <option key={s.id} value={s.id}>{s.seriesName}{s.year ? ` (${s.year})` : ''}</option>
                    );
                  })
                }
              </select>

              <select
                className={styles.select}
                value={selectedType}
                onChange={this.onTypeChange}
              >
                {
                  RELATION_TYPES.map((t) => {
                    return (
                      <option key={t} value={t}>{translate(`SeriesRelationType_${t}`)}</option>
                    );
                  })
                }
              </select>

              <Link
                className={styles.addLink}
                component="div"
                onPress={this.onSavePress}
              >
                {translate('Add')}
              </Link>

              <Link
                className={styles.addLink}
                component="div"
                onPress={this.onCancelPress}
              >
                {translate('Cancel')}
              </Link>
            </span> :
            <Link
              className={styles.addLink}
              component="div"
              onPress={this.onAddPress}
            >
              {translate('LinkRelatedSeries')}
            </Link>
        }

        {
          error &&
            <span className={styles.error}>{error}</span>
        }
      </div>
    );
  }
}

SeriesDetailsRelations.propTypes = {
  seriesId: PropTypes.number.isRequired,
  allSeries: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default SeriesDetailsRelations;
