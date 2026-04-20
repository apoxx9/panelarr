import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, sortDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import getToggledRange from 'Utilities/Table/getToggledRange';
import IssueRowConnector from './IssueRowConnector';
import styles from './SeriesDetailsSeries.css';

class SeriesDetailsSeries extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isOrganizeModalOpen: false,
      isManageIssuesOpen: false,
      lastToggledIssue: null
    };
  }

  componentDidMount() {
    this._expandByDefault();
  }

  componentDidUpdate(prevProps) {
    const {
      seriesId
    } = this.props;

    if (prevProps.seriesId !== seriesId) {
      this._expandByDefault();
      return;
    }
  }

  //
  // Control

  _expandByDefault() {
    const {
      id,
      onExpandPress
    } = this.props;

    onExpandPress(id, true);
  }

  isSeriesMonitored(series) {
    return series.items.every((issue) => issue.monitored);
  }

  isSeriesSaving(series) {
    return series.items.some((issue) => issue.isSaving);
  }

  //
  // Listeners

  onExpandPress = () => {
    const {
      id,
      isExpanded
    } = this.props;

    this.props.onExpandPress(id, !isExpanded);
  };

  onMonitorIssuePress = (issueId, monitored, { shiftKey }) => {
    const lastToggled = this.state.lastToggledIssue;
    const issueIds = [issueId];

    if (shiftKey && lastToggled) {
      const { lower, upper } = getToggledRange(this.props.items, issueId, lastToggled);
      const items = this.props.items;

      for (let i = lower; i < upper; i++) {
        issueIds.push(items[i].id);
      }
    }

    this.setState({ lastToggledIssue: issueId });

    this.props.onMonitorIssuePress(_.uniq(issueIds), monitored);
  };

  onMonitorSeriesPress = (monitored, { shiftKey }) => {
    const issueIds = this.props.items.map((issue) => issue.id);

    this.props.onMonitorIssuePress(_.uniq(issueIds), monitored);
  };

  //
  // Render

  render() {
    const {
      label,
      items,
      positionMap,
      columns,
      isExpanded,
      sortKey,
      sortDirection,
      onSortPress,
      isSmallScreen,
      onTableOptionChange,
      seriesMonitored
    } = this.props;

    return (
      <div
        className={styles.issueType}
      >
        <div className={styles.seriesTitle}>
          <MonitorToggleButton
            size={24}
            monitored={this.isSeriesMonitored(this.props)}
            isDisabled={!seriesMonitored}
            isSaving={this.isSeriesSaving(this.props)}
            onPress={this.onMonitorSeriesPress}
          />

          <Link
            className={styles.expandButton}
            onPress={this.onExpandPress}
          >
            <div className={styles.header}>
              <div className={styles.left}>
                {
                  <div>
                    <span className={styles.issueTypeLabel}>
                      {label}
                    </span>

                    <span className={styles.issueCount}>
                      ({items.length} Issues)
                    </span>
                  </div>
                }

              </div>

              <Icon
                className={styles.expandButtonIcon}
                name={isExpanded ? icons.COLLAPSE : icons.EXPAND}
                title={isExpanded ? translate('IsExpandedHideIssues') : translate('IsExpandedShowIssues')}
                size={24}
              />

              {
                !isSmallScreen &&
                  <span>&nbsp;</span>
              }

            </div>
          </Link>
        </div>

        <div>
          {
            isExpanded &&
              <div className={styles.issues}>
                <Table
                  columns={columns}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  onSortPress={onSortPress}
                  onTableOptionChange={onTableOptionChange}
                >
                  <TableBody>
                    {
                      items.map((item) => {
                        return (
                          <IssueRowConnector
                            key={item.id}
                            columns={columns}
                            {...item}
                            position={positionMap[item.id]}
                            onMonitorIssuePress={this.onMonitorIssuePress}
                          />
                        );
                      })
                    }
                  </TableBody>
                </Table>

                <div className={styles.collapseButtonContainer}>
                  <IconButton
                    iconClassName={styles.collapseButtonIcon}
                    name={icons.COLLAPSE}
                    size={20}
                    title={translate('HideIssues')}
                    onPress={this.onExpandPress}
                  />
                </div>
              </div>
          }
        </div>
      </div>
    );
  }
}

SeriesDetailsSeries.propTypes = {
  id: PropTypes.number.isRequired,
  seriesId: PropTypes.number.isRequired,
  label: PropTypes.string.isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  positionMap: PropTypes.object.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isExpanded: PropTypes.bool,
  isSmallScreen: PropTypes.bool.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onMonitorIssuePress: PropTypes.func.isRequired,
  uiSettings: PropTypes.object.isRequired,
  seriesMonitored: PropTypes.bool.isRequired
};

export default SeriesDetailsSeries;
