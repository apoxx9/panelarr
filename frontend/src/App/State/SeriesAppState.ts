import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import Series from 'Series/Series';

interface SeriesAppState
  extends AppSectionState<Series>,
    AppSectionDeleteState,
    AppSectionSaveState {
  itemMap: Record<number, number>;

  deleteOptions: {
    addImportListExclusion: boolean;
  };
}

export default SeriesAppState;
