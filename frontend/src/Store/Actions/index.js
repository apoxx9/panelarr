import * as app from './appActions';
import * as series from './seriesActions';
import * as seriesCollection from './seriesCollectionActions';
import * as seriesDetails from './seriesDetailsActions';
import * as seriesHistory from './seriesHistoryActions';
import * as seriesIndex from './seriesIndexActions';
import * as blocklist from './blocklistActions';
import * as issues from './issueActions';
import * as issueFiles from './issueFileActions';
import * as issueHistory from './issueHistoryActions';
import * as issueIndex from './issueIndexActions';
import * as issueStudio from './issueshelfActions';
import * as calendar from './calendarActions';
import * as captcha from './captchaActions';
import * as commands from './commandActions';
import * as customFilters from './customFilterActions';
import * as editions from './editionActions';
import * as history from './historyActions';
import * as interactiveImportActions from './interactiveImportActions';
import * as oAuth from './oAuthActions';
import * as organizePreview from './organizePreviewActions';
import * as paths from './pathActions';
import * as providerOptions from './providerOptionActions';
import * as queue from './queueActions';
import * as releases from './releaseActions';
import * as retagPreview from './retagPreviewActions';
import * as search from './searchActions';
import * as settings from './settingsActions';
import * as system from './systemActions';
import * as tags from './tagActions';
import * as wanted from './wantedActions';

export default [
  app,
  series,
  seriesCollection,
  seriesDetails,
  seriesHistory,
  seriesIndex,
  blocklist,
  issueFiles,
  issueHistory,
  issueIndex,
  issues,
  issueStudio,
  calendar,
  captcha,
  commands,
  customFilters,
  editions,
  history,
  interactiveImportActions,
  oAuth,
  organizePreview,
  paths,
  providerOptions,
  queue,
  releases,
  retagPreview,
  search,
  settings,
  system,
  tags,
  wanted
];
