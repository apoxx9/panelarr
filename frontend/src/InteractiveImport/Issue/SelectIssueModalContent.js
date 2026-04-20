import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SelectIssueRow from './SelectIssueRow';
import styles from './SelectIssueModalContent.css';

const columns = [
  {
    name: 'title',
    label: 'Issue Title',
    isSortable: true,
    isVisible: true
  },
  {
    name: 'releaseDate',
    label: 'Release Date',
    isSortable: true,
    isVisible: true
  },
  {
    name: 'status',
    label: 'Issue Status',
    isVisible: true
  }
];

class SelectIssueModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      filter: ''
    };
  }

  //
  // Listeners

  onFilterChange = ({ value }) => {
    this.setState({ filter: value });
  };

  //
  // Render

  render() {
    const {
      items,
      onIssueSelect,
      onModalClose,
      isFetching,
      ...otherProps
    } = this.props;

    const filter = this.state.filter;
    const filterLower = filter.toLowerCase();

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Manual Import - Select Issue
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          {
            isFetching &&
              <LoadingIndicator />
          }
          <TextInput
            className={styles.filterInput}
            placeholder={translate('FilterPlaceHolder')}
            name="filter"
            value={filter}
            autoFocus={true}
            onChange={this.onFilterChange}
          />

          <Scroller className={styles.scroller}>
            {
              <Table
                columns={columns}
                {...otherProps}
              >
                <TableBody>
                  {
                    items.map((item) => {
                      return item.title.toLowerCase().includes(filterLower) ?
                        (
                          <SelectIssueRow
                            key={item.id}
                            columns={columns}
                            onIssueSelect={onIssueSelect}
                            {...item}
                          />
                        ) :
                        null;
                    })
                  }
                </TableBody>
              </Table>
            }
          </Scroller>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            Cancel
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

SelectIssueModalContent.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isFetching: PropTypes.bool.isRequired,
  onIssueSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectIssueModalContent;
