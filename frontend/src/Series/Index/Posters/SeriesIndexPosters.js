import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Grid, WindowScroller } from 'react-virtualized';
import Measure from 'Components/Measure';
import PublisherGroupHeader from 'Series/Index/PublisherGroupHeader';
import SeriesIndexItemConnector from 'Series/Index/SeriesIndexItemConnector';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import groupSeriesByPublisher from 'Utilities/Series/groupSeriesByPublisher';
import SeriesIndexPoster from './SeriesIndexPoster';
import styles from './SeriesIndexPosters.css';

// Poster container dimensions
const columnPadding = parseInt(dimensions.seriesIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.seriesIndexColumnPaddingSmallScreen);
const progressBarHeight = parseInt(dimensions.progressBarSmallHeight);
const detailedProgressBarHeight = parseInt(dimensions.progressBarMediumHeight);

const additionalColumnCount = {
  small: 3,
  medium: 2,
  large: 1
};

const groupHeaderHeight = 40;

function calculateColumnWidth(width, posterSize, isSmallScreen) {
  const maxiumColumnWidth = isSmallScreen ? 172 : 182;
  const columns = Math.floor(width / maxiumColumnWidth);
  const remainder = width % maxiumColumnWidth;

  if (remainder === 0 && posterSize === 'large') {
    return maxiumColumnWidth;
  }

  return Math.floor(width / (columns + additionalColumnCount[posterSize]));
}

function calculateRowHeight(posterHeight, sortKey, isSmallScreen, posterOptions) {
  const {
    detailedProgressBar,
    showTitle,
    showMonitored,
    showQualityProfile
  } = posterOptions;

  const heights = [
    posterHeight,
    detailedProgressBar ? detailedProgressBarHeight : progressBarHeight,
    isSmallScreen ? columnPaddingSmallScreen : columnPadding
  ];

  if (showTitle !== 'no') {
    heights.push(19);
  }

  if (showMonitored) {
    heights.push(19);
  }

  if (showQualityProfile) {
    heights.push(19);
  }

  switch (sortKey) {
    case 'added':
    case 'path':
    case 'sizeOnDisk':
      heights.push(19);
      break;
    case 'qualityProfileId':
      if (!showQualityProfile) {
        heights.push(19);
      }
      break;
    default:
      // No need to add a height of 0
  }

  return heights.reduce((acc, height) => acc + height, 0);
}

function calculatePosterHeight(posterWidth) {
  return Math.ceil(posterWidth);
}

class SeriesIndexPosters extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      width: 0,
      columnWidth: 182,
      columnCount: 1,
      posterWidth: 238,
      posterHeight: 238,
      rowHeight: calculateRowHeight(238, null, props.isSmallScreen, {}),
      scrollRestored: false
    };

    this._isInitialized = false;
    this._grid = null;
    this._padding = props.isSmallScreen ? columnPaddingSmallScreen : columnPadding;

    // Memoized grouped row model: header rows + chunked poster rows
    this._groupedRowsSource = null;
    this._groupedRowsColumnCount = null;
    this._groupedRows = null;
  }

  componentDidUpdate(prevProps, prevState) {
    const {
      items,
      sortKey,
      posterOptions,
      jumpToCharacter,
      isSmallScreen,
      isEditorActive,
      scrollTop,
      selectedState
    } = this.props;

    const {
      width,
      columnWidth,
      columnCount,
      rowHeight,
      scrollRestored
    } = this.state;

    if (prevProps.sortKey !== sortKey ||
        prevProps.posterOptions !== posterOptions) {
      this.calculateGrid(width, isSmallScreen);
    }

    if (this._grid &&
        (prevState.width !== width ||
            prevState.columnWidth !== columnWidth ||
            prevState.columnCount !== columnCount ||
            prevState.rowHeight !== rowHeight ||
            hasDifferentItemsOrOrder(prevProps.items, items)) ||
            prevProps.isEditorActive !== isEditorActive ||
            prevProps.selectedState !== selectedState ||
            prevProps.groupByPublisher !== this.props.groupByPublisher ||
            prevProps.posterOptions.showTitle !== posterOptions.showTitle) {
      // recomputeGridSize also forces Grid to discard its cache of rendered cells
      this._grid.recomputeGridSize();
    }

    if (this._grid && scrollTop !== 0 && !scrollRestored) {
      this.setState({ scrollRestored: true });
      this._grid.scrollToPosition({ scrollTop });
    }

    if (jumpToCharacter != null && jumpToCharacter !== prevProps.jumpToCharacter) {
      const index = getIndexOfFirstCharacter(items, sortKey, jumpToCharacter);

      if (this._grid && index != null) {
        const row = Math.floor(index / columnCount);

        this._grid.scrollToCell({
          rowIndex: row,
          columnIndex: 0
        });
      }
    }
  }

  //
  // Control

  setGridRef = (ref) => {
    this._grid = ref;
  };

  getGroupedRows() {
    const {
      items
    } = this.props;

    const {
      columnCount
    } = this.state;

    if (this._groupedRowsSource !== items || this._groupedRowsColumnCount !== columnCount) {
      this._groupedRows = groupSeriesByPublisher(items).reduce((acc, group) => {
        acc.push({ isGroupHeader: true, title: group.title, count: group.items.length });

        for (let i = 0; i < group.items.length; i += columnCount) {
          acc.push({ items: group.items.slice(i, i + columnCount) });
        }

        return acc;
      }, []);

      this._groupedRowsSource = items;
      this._groupedRowsColumnCount = columnCount;
    }

    return this._groupedRows;
  }

  getRowHeight = ({ index }) => {
    const rows = this.getGroupedRows();

    return rows[index] && rows[index].isGroupHeader ? groupHeaderHeight : this.state.rowHeight;
  };

  calculateGrid = (width = this.state.width, isSmallScreen) => {
    const {
      sortKey,
      posterOptions
    } = this.props;

    const columnWidth = calculateColumnWidth(width, posterOptions.size, isSmallScreen);
    const columnCount = Math.max(Math.floor(width / columnWidth), 1);
    const posterWidth = columnWidth - this._padding * 2;
    const posterHeight = calculatePosterHeight(posterWidth);
    const rowHeight = calculateRowHeight(posterHeight, sortKey, isSmallScreen, posterOptions);

    this.setState({
      width,
      columnWidth,
      columnCount,
      posterWidth,
      posterHeight,
      rowHeight
    });
  };

  cellRenderer = ({ key, rowIndex, columnIndex, style }) => {
    const {
      items,
      groupByPublisher,
      sortKey,
      posterOptions,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      selectedState,
      isEditorActive,
      onSelectedChange
    } = this.props;

    const {
      width,
      posterWidth,
      posterHeight,
      columnCount
    } = this.state;

    const {
      detailedProgressBar,
      showTitle,
      showMonitored,
      showQualityProfile
    } = posterOptions;

    let series = null;

    if (groupByPublisher) {
      const row = this.getGroupedRows()[rowIndex];

      if (!row) {
        return null;
      }

      if (row.isGroupHeader) {
        // Grid renders one cell per column; the header renders once, spanning
        // the full row via an overridden width
        if (columnIndex !== 0) {
          return null;
        }

        return (
          <div
            key={key}
            style={{
              ...style,
              width
            }}
          >
            <PublisherGroupHeader
              title={row.title}
              count={row.count}
            />
          </div>
        );
      }

      series = row.items[columnIndex];
    } else {
      series = items[rowIndex * columnCount + columnIndex];
    }

    if (!series) {
      return null;
    }

    return (
      <div
        key={key}
        style={{
          ...style,
          padding: this._padding
        }}
      >
        <SeriesIndexItemConnector
          key={series.id}
          component={SeriesIndexPoster}
          sortKey={sortKey}
          posterWidth={posterWidth}
          posterHeight={posterHeight}
          detailedProgressBar={detailedProgressBar}
          showTitle={showTitle}
          showMonitored={showMonitored}
          showQualityProfile={showQualityProfile}
          showRelativeDates={showRelativeDates}
          shortDateFormat={shortDateFormat}
          timeFormat={timeFormat}
          style={style}
          seriesId={series.id}
          qualityProfileId={series.qualityProfileId}
          isSelected={selectedState[series.id]}
          onSelectedChange={onSelectedChange}
          isEditorActive={isEditorActive}
        />
      </div>
    );
  };

  //
  // Listeners

  onMeasure = ({ width }) => {
    this.calculateGrid(width, this.props.isSmallScreen);
  };

  //
  // Render

  render() {
    const {
      scroller,
      items,
      groupByPublisher,
      isSmallScreen
    } = this.props;

    const {
      width,
      columnWidth,
      columnCount,
      rowHeight
    } = this.state;

    const rowCount = groupByPublisher ?
      this.getGroupedRows().length :
      Math.ceil(items.length / columnCount);

    return (
      <Measure
        onMeasure={this.onMeasure}
      >
        <WindowScroller
          scrollElement={isSmallScreen ? undefined : scroller}
        >
          {({ height, registerChild, onChildScroll, scrollTop }) => {
            if (!height) {
              return <div />;
            }

            return (
              <div ref={registerChild}>
                <Grid
                  ref={this.setGridRef}
                  className={styles.grid}
                  autoHeight={true}
                  height={height}
                  columnCount={columnCount}
                  columnWidth={columnWidth}
                  rowCount={rowCount}
                  rowHeight={groupByPublisher ? this.getRowHeight : rowHeight}
                  width={width}
                  onScroll={onChildScroll}
                  scrollTop={scrollTop}
                  overscanRowCount={2}
                  cellRenderer={this.cellRenderer}
                  scrollToAlignment={'start'}
                  isScrollingOptOut={true}
                />
              </div>
            );
          }
          }
        </WindowScroller>
      </Measure>
    );
  }
}

SeriesIndexPosters.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  groupByPublisher: PropTypes.bool.isRequired,
  sortKey: PropTypes.string,
  posterOptions: PropTypes.object.isRequired,
  jumpToCharacter: PropTypes.string,
  scrollTop: PropTypes.number.isRequired,
  scroller: PropTypes.instanceOf(Element).isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  timeFormat: PropTypes.string.isRequired,
  selectedState: PropTypes.object.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired
};

export default SeriesIndexPosters;
