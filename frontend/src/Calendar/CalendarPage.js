import PropTypes from 'prop-types';
import React, { Component } from 'react';
import NoSeries from 'Series/NoSeries';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Measure from 'Components/Measure';
import FilterMenu from 'Components/Menu/FilterMenu';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import { align, icons } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import CalendarConnector from './CalendarConnector';
import CalendarLinkModal from './iCal/CalendarLinkModal';
import LegendConnector from './Legend/LegendConnector';
import CalendarOptionsModal from './Options/CalendarOptionsModal';
import styles from './CalendarPage.css';

const MINIMUM_DAY_WIDTH = 120;

class CalendarPage extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isCalendarLinkModalOpen: false,
      isOptionsModalOpen: false,
      width: 0
    };
  }

  //
  // Listeners

  onMeasure = ({ width }) => {
    this.setState({ width });
    const days = Math.max(3, Math.min(7, Math.floor(width / MINIMUM_DAY_WIDTH)));

    this.props.onDaysCountChange(days);
  };

  onGetCalendarLinkPress = () => {
    this.setState({ isCalendarLinkModalOpen: true });
  };

  onGetCalendarLinkModalClose = () => {
    this.setState({ isCalendarLinkModalOpen: false });
  };

  onOptionsPress = () => {
    this.setState({ isOptionsModalOpen: true });
  };

  onOptionsModalClose = () => {
    this.setState({ isOptionsModalOpen: false });
  };

  onSearchMissingPress = () => {
    const {
      missingIssueIds,
      onSearchMissingPress
    } = this.props;

    onSearchMissingPress(missingIssueIds);
  };

  //
  // Render

  render() {
    const {
      selectedFilterKey,
      filters,
      hasSeries,
      seriesError,
      seriesIsFetching,
      seriesIsPopulated,
      missingIssueIds,
      isSearchingForMissing,
      useCurrentPage,
      onFilterSelect
    } = this.props;

    const {
      isCalendarLinkModalOpen,
      isOptionsModalOpen
    } = this.state;

    const isMeasured = this.state.width > 0;

    return (
      <PageContent title={translate('Calendar')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('ICalLink')}
              iconName={icons.CALENDAR}
              onPress={this.onGetCalendarLinkPress}
            />

            <PageToolbarButton
              label={translate('SearchForMissing')}
              iconName={icons.SEARCH}
              isDisabled={!missingIssueIds.length}
              isSpinning={isSearchingForMissing}
              onPress={this.onSearchMissingPress}
            />
          </PageToolbarSection>

          <PageToolbarSection alignContent={align.RIGHT}>
            <PageToolbarButton
              label={translate('Options')}
              iconName={icons.POSTER}
              onPress={this.onOptionsPress}
            />

            <FilterMenu
              alignMenu={align.RIGHT}
              isDisabled={!hasSeries}
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={[]}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <PageContentBody
          className={styles.calendarPageBody}
          innerClassName={styles.calendarInnerPageBody}
        >
          {
            seriesIsFetching && !seriesIsPopulated &&
              <LoadingIndicator />
          }

          {
            seriesError &&
              <div className={styles.errorMessage}>
                {getErrorMessage(seriesError, 'Failed to load series from API')}
              </div>
          }

          {
            !seriesError && seriesIsPopulated && hasSeries &&
              <Measure
                onMeasure={this.onMeasure}
              >
                {
                  isMeasured ?
                    <CalendarConnector
                      useCurrentPage={useCurrentPage}
                    /> :
                    <div />
                }
              </Measure>
          }

          {
            !seriesError && seriesIsPopulated && !hasSeries &&
              <NoSeries />
          }

          {
            hasSeries && !!seriesError &&
              <LegendConnector />
          }
        </PageContentBody>

        <CalendarLinkModal
          isOpen={isCalendarLinkModalOpen}
          onModalClose={this.onGetCalendarLinkModalClose}
        />

        <CalendarOptionsModal
          isOpen={isOptionsModalOpen}
          onModalClose={this.onOptionsModalClose}
        />

      </PageContent>
    );
  }
}

CalendarPage.propTypes = {
  selectedFilterKey: PropTypes.string.isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  hasSeries: PropTypes.bool.isRequired,
  seriesError: PropTypes.object,
  seriesIsFetching: PropTypes.bool.isRequired,
  seriesIsPopulated: PropTypes.bool.isRequired,
  missingIssueIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  isSearchingForMissing: PropTypes.bool.isRequired,
  useCurrentPage: PropTypes.bool.isRequired,
  onSearchMissingPress: PropTypes.func.isRequired,
  onDaysCountChange: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired
};

export default CalendarPage;
