import * as app from './appActions';
import * as blocklist from './blocklistActions';
import * as calendar from './calendarActions';
import * as captcha from './captchaActions';
import * as commands from './commandActions';
import * as customFilters from './customFilterActions';
import * as history from './historyActions';
import * as interactiveImportActions from './interactiveImportActions';
import * as issues from './issueActions';
import * as issueFiles from './issueFileActions';
import * as issueHistory from './issueHistoryActions';
import * as issueIndex from './issueIndexActions';
import * as oAuth from './oAuthActions';
import * as organizePreview from './organizePreviewActions';
import * as paths from './pathActions';
import * as providerOptions from './providerOptionActions';
import * as queue from './queueActions';
import * as releases from './releaseActions';
import * as retagPreview from './retagPreviewActions';
import * as search from './searchActions';
import * as series from './seriesActions';
import * as seriesDetails from './seriesDetailsActions';
import * as seriesHistory from './seriesHistoryActions';
import * as seriesIndex from './seriesIndexActions';
import * as settings from './settingsActions';
import * as system from './systemActions';
import * as tags from './tagActions';
import * as wanted from './wantedActions';

export default [
  app,
  series,
  seriesDetails,
  seriesHistory,
  seriesIndex,
  blocklist,
  issueFiles,
  issueHistory,
  issueIndex,
  issues,
  calendar,
  captcha,
  commands,
  customFilters,
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
