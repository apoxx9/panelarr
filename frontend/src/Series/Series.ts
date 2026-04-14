import ModelBase from 'App/ModelBase';

export type SeriesStatus = 'continuing' | 'ended';

interface Series extends ModelBase {
  status: SeriesStatus;
  ended: boolean;
  seriesName: string;
  seriesNameLastFirst: string;
  foreignSeriesId: string;
  titleSlug: string;
  overview: string;
  disambiguation?: string;
  links: object[];
  nextIssue?: object;
  lastIssue?: object;
  images: object[];
  remotePoster?: string;
  path: string;
  qualityProfileId: number;
  monitored: boolean;
  monitorNewItems: string;
  rootFolderPath: string;
  folder?: string;
  genres: string[];
  cleanName: string;
  sortName: string;
  tags: number[];
  added: string;
  addOptions?: object;
  ratings: object;
  year?: number;
  seriesType?: string;
  volumeNumber?: number;
  publisherName?: string;
  statistics: object;
  isSaving?: boolean;
}

export default Series;
