
function getNewSeries(series, payload) {
  const {
    rootFolderPath,
    monitor,
    monitorNewItems,
    qualityProfileId,
    metadataProfileId,
    tags,
    searchForMissingIssues = false
  } = payload;

  const addOptions = {
    monitor,
    searchForMissingIssues
  };

  series.addOptions = addOptions;
  series.monitored = true;
  series.monitorNewItems = monitorNewItems;
  series.qualityProfileId = qualityProfileId;
  series.metadataProfileId = metadataProfileId;
  series.rootFolderPath = rootFolderPath;
  series.tags = tags;

  return series;
}

export default getNewSeries;
