import { ConnectedRouter } from 'connected-react-router';
import PropTypes from 'prop-types';
import React from 'react';
import DocumentTitle from 'react-document-title';
import { Provider } from 'react-redux';
import { Route, Switch } from 'react-router-dom';
import PageConnector from 'Components/Page/PageConnector';
import SetupWizardConnector from 'SetupWizard/SetupWizardConnector';
import ApplyTheme from './ApplyTheme';
import AppRoutes from './AppRoutes';

function App({ store, history }) {
  return (
    <DocumentTitle title={window.Panelarr.instanceName}>
      <Provider store={store}>
        <ConnectedRouter history={history}>
          <ApplyTheme>
            <Switch>
              <Route
                path="/setup"
                component={SetupWizardConnector}
              />
              <Route
                render={() => (
                  <PageConnector>
                    <AppRoutes app={App} />
                  </PageConnector>
                )}
              />
            </Switch>
          </ApplyTheme>
        </ConnectedRouter>
      </Provider>
    </DocumentTitle>
  );
}

App.propTypes = {
  store: PropTypes.object.isRequired,
  history: PropTypes.object.isRequired
};

export default App;
