import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { sortDirections } from 'Helpers/Props';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import getToggledRange from 'Utilities/Table/getToggledRange';
import IssueRowConnector from './IssueRowConnector';
import styles from './SeriesDetailsSeason.css';

class SeriesDetailsSeason extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      lastToggledIssue: null
    };
  }

  componentDidMount() {
    this.props.setSelectedState(this.props.items);
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection,
      setSelectedState
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      setSelectedState(items);
    }
  }

  //
  // Listeners

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

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    const {
      onSelectedChange,
      items
    } = this.props;

    return onSelectedChange(items, id, value, shiftKey);
  };

  //
  // Render

  render() {
    const {
      items,
      isEditorActive,
      columns,
      sortKey,
      sortDirection,
      onSortPress,
      onTableOptionChange,
      selectedState
    } = this.props;

    let titleColumns = columns;
    if (!isEditorActive) {
      titleColumns = columns.filter((x) => x.name !== 'select');
    }

    return (
      <div
        className={styles.issueType}
      >
        <div className={styles.issues}>
          <Table
            columns={titleColumns}
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
                      onMonitorIssuePress={this.onMonitorIssuePress}
                      isEditorActive={isEditorActive}
                      isSelected={selectedState[item.id]}
                      onSelectedChange={this.onSelectedChange}
                    />
                  );
                })
              }
            </TableBody>
          </Table>
        </div>
      </div>
    );
  }
}

SeriesDetailsSeason.propTypes = {
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isEditorActive: PropTypes.bool.isRequired,
  selectedState: PropTypes.object.isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  onExpandPress: PropTypes.func.isRequired,
  setSelectedState: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onMonitorIssuePress: PropTypes.func.isRequired,
  uiSettings: PropTypes.object.isRequired
};

export default SeriesDetailsSeason;
