import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import styles from '../styles.css';

class Donations extends Component {

  //
  // Render

  render() {
    return (
      <FieldSet legend="Donations">
        <div className={styles.logoContainer} title="Panelarr">
          <Link to="https://opencollective.com/panelarr">
            <img
              className={styles.logo}
              src={`${window.Panelarr.urlBase}/Content/Images/Icons/logo-panelarr.png`}
            />
          </Link>
        </div>
      </FieldSet>
    );
  }
}

Donations.propTypes = {

};

export default Donations;
