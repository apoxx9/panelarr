import ModelBase from 'App/ModelBase';

export type SeriesStatus = 'continuing' | 'ended';

interface Series extends ModelBase {
  added: string;
  genres: string[];
  monitored: boolean;
  overview: string;
  path: string;
  qualityProfileId: number;
  rootFolderPath: string;
  sortName: string;
  status: SeriesStatus;
  tags: number[];
  seriesName: string;
  isSaving?: boolean;
}

export default Series;
