import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import SelectEditionRowConnector from './SelectEditionRowConnector';
import styles from './SelectEditionModalContent.css';

const columns = [
  {
    name: 'issue',
    label: 'Issue',
    isVisible: true
  },
  {
    name: 'edition',
    label: 'Edition',
    isVisible: true
  }
];

class SelectEditionModalContent extends Component {

  //
  // Render

  render() {
    const {
      issues,
      isPopulated,
      isFetching,
      error,
      onEditionSelect,
      onModalClose,
      ...otherProps
    } = this.props;

    if (!isPopulated && !error) {
      return (<LoadingIndicator />);
    }

    if (!isFetching && error) {
      return (
        <div>
          {translate('LoadingEditionsFailed')}
        </div>
      );
    }

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('ManualImportSelectEdition')}
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.VERTICAL}
        >
          <Alert>
            Overriding an edition here will <b>disable automatic edition selection</b> for this issue. Use this to pick a specific printing or variant.
          </Alert>

          <Table
            columns={columns}
            {...otherProps}
          >
            <TableBody>
              {
                issues.map((item) => {
                  return (
                    <SelectEditionRowConnector
                      key={item.issue.id}
                      matchedEditionId={item.matchedEditionId}
                      columns={columns}
                      onEditionSelect={onEditionSelect}
                      {...item.issue}
                    />
                  );
                })
              }
            </TableBody>
          </Table>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>
            {translate('Cancel')}
          </Button>
        </ModalFooter>
      </ModalContent>
    );
  }
}

SelectEditionModalContent.propTypes = {
  issues: PropTypes.arrayOf(PropTypes.object).isRequired,
  isFetching: PropTypes.bool,
  isPopulated: PropTypes.bool,
  error: PropTypes.object,
  onEditionSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SelectEditionModalContent;
